using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Constants;
using HRMSAgent.Core.Models;
using HRMSAgent.Core.Services;

namespace HRMSSyncManager.ViewModels;

public partial class DashboardViewModel : ObservableObject, IDisposable
{
    private readonly IAgentConfigurationStore _configStore;
    private readonly ISqlConnectionTester _sqlTester;
    private readonly IHrmsApiClient _apiClient;
    private readonly IWindowsServiceManager _serviceManager;
    private readonly DispatcherTimer _refreshTimer;

    [ObservableProperty] private string _serviceStatus = "Unknown";
    [ObservableProperty] private HealthLevel _serviceHealth = HealthLevel.Unknown;
    [ObservableProperty] private string _lastSyncTime = "—";
    [ObservableProperty] private string _lastSuccessfulSync = "—";
    [ObservableProperty] private int _pendingRecordsCount;
    [ObservableProperty] private HealthLevel _apiHealth = HealthLevel.Unknown;
    [ObservableProperty] private HealthLevel _sqlHealth = HealthLevel.Unknown;
    [ObservableProperty] private int _syncIntervalSeconds = 30;
    [ObservableProperty] private string _database = string.Empty;
    [ObservableProperty] private string _sqlServer = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyCode = string.Empty;
    [ObservableProperty] private string _apiUrl = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _sqlUsername = string.Empty;
    [ObservableProperty] private string _sqlPassword = string.Empty;
    [ObservableProperty] private int _batchSize = 100;
    [ObservableProperty] private bool _isServiceNotInstalled;
    [ObservableProperty] private bool _isServiceRunning;
    [ObservableProperty] private bool _showStartService;
    [ObservableProperty] private bool _showPauseService;
    [ObservableProperty] private bool _showRestartService;

    public DashboardViewModel(
        IAgentConfigurationStore configStore,
        ISqlConnectionTester sqlTester,
        IHrmsApiClient apiClient,
        IWindowsServiceManager serviceManager)
    {
        _configStore = configStore;
        _sqlTester = sqlTester;
        _apiClient = apiClient;
        _serviceManager = serviceManager;

        _refreshTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        _refreshTimer.Tick += (_, _) => RefreshStatus();
        _refreshTimer.Start();

        LoadConfiguration();
        RefreshStatus();
    }

    public void ReloadFromStore() => LoadConfiguration();

    private void LoadConfiguration()
    {
        if (!_configStore.Exists())
            return;

        var config = _configStore.Load();
        CompanyName = config.CompanyName;
        CompanyCode = config.CompanyCode;
        ApiUrl = config.ApiUrl;
        ApiKey = config.ApiKey;
        SqlServer = config.SqlServer;
        Database = config.Database;
        SqlUsername = config.SqlUsername;
        SqlPassword = config.SqlPassword;
        SyncIntervalSeconds = config.SyncIntervalSeconds;
        BatchSize = config.BatchSize;
    }

    [RelayCommand]
    private void RefreshStatus()
    {
        AgentConfiguration? config = null;
        if (_configStore.Exists())
            config = _configStore.Load();

        var health = _serviceManager.GetHealthOverview(config);

        ServiceStatus = health.ServiceStatus;
        ServiceHealth = health.ServiceHealth;
        LastSyncTime = FormatTime(health.LastSyncTime);
        LastSuccessfulSync = FormatTime(health.LastSuccessfulSyncTime);
        PendingRecordsCount = health.PendingRecordsCount;
        ApiHealth = health.ApiHealth;
        SqlHealth = health.SqlHealth;
        SyncIntervalSeconds = health.SyncIntervalSeconds;
        Database = health.Database;
        SqlServer = health.SqlServer;
        IsServiceNotInstalled = health.ServiceStatus == "Not Installed";
        IsServiceRunning = health.ServiceStatus.Equals("Running", StringComparison.OrdinalIgnoreCase);
        ShowStartService = !IsServiceNotInstalled && !IsServiceRunning;
        ShowPauseService = IsServiceRunning;
        ShowRestartService = IsServiceRunning;

        if (IsServiceNotInstalled)
            StatusMessage = "Background sync service is not installed yet. Click \"Install Service\" above.";
        else if (!string.IsNullOrWhiteSpace(health.LastError))
            StatusMessage = health.LastError;
        else if (IsServiceRunning)
            StatusMessage = "Background sync is running.";
        else
            StatusMessage = "Service is paused. Click \"Start Service\" to resume sync.";
    }

    [RelayCommand]
    private async Task InstallServiceAsync()
    {
        var serviceExe = Environment.ProcessPath
            ?? Path.Combine(AppContext.BaseDirectory, "HRMSAgent.exe");

        await RunServiceAction(async () =>
        {
            if (!_serviceManager.IsInstalled())
                await _serviceManager.InstallAsync(serviceExe);

            await _serviceManager.StartAsync();
        }, "Service is installed and running.");
    }

    [RelayCommand]
    private async Task StartServiceAsync() => await RunServiceAction(
        () => _serviceManager.StartAsync(),
        "Service started. Sync resumed.");

    [RelayCommand]
    private async Task PauseServiceAsync() => await RunServiceAction(
        () => _serviceManager.StopAsync(),
        "Service paused. Sync stopped.");

    [RelayCommand]
    private async Task RestartServiceAsync() => await RunServiceAction(
        () => _serviceManager.RestartAsync(),
        "Service restarted.");

    [RelayCommand]
    private void OpenLogsFolder()
    {
        AgentPaths.EnsureDirectoriesExist();
        Process.Start(new ProcessStartInfo
        {
            FileName = AgentPaths.LogsDirectory,
            UseShellExecute = true
        });
    }

    [RelayCommand]
    private async Task TestSqlConnectionAsync()
    {
        IsBusy = true;
        StatusMessage = "Testing SQL connection...";

        try
        {
            await _sqlTester.TestConnectionAsync(SqlServer, Database, SqlUsername, SqlPassword);
            SqlHealth = HealthLevel.Healthy;
            StatusMessage = "SQL connection successful.";
        }
        catch (Exception ex)
        {
            SqlHealth = HealthLevel.Error;
            StatusMessage = $"SQL connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestApiConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "Enter API sync URL and API Key first.";
            return;
        }

        if (!ApiUrlHelper.TryValidateSyncEndpoint(ApiUrl, out var apiError))
        {
            StatusMessage = apiError!;
            return;
        }

        IsBusy = true;
        StatusMessage = "Testing API connection...";

        try
        {
            SaveConfigurationInternal();
            var success = await _apiClient.TestConnectionAsync();
            ApiHealth = success ? HealthLevel.Healthy : HealthLevel.Error;
            StatusMessage = success
                ? "API connection successful."
                : "API connection failed.";
        }
        catch (Exception ex)
        {
            ApiHealth = HealthLevel.Error;
            StatusMessage = $"API test error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SaveConfiguration()
    {
        try
        {
            if (!TryValidateConfiguration(out var error))
            {
                StatusMessage = error!;
                return;
            }

            SaveConfigurationInternal();
            StatusMessage = "Configuration saved.";
            ReloadFromStore();
            RefreshStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Failed to save configuration: {ex.Message}";
            MessageBox.Show(StatusMessage, "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void SaveConfigurationInternal()
    {
        var config = _configStore.Exists()
            ? _configStore.Load()
            : new AgentConfiguration();

        if (!string.IsNullOrWhiteSpace(CompanyName))
            config.CompanyName = CompanyName.Trim();

        if (!string.IsNullOrWhiteSpace(CompanyCode))
            config.CompanyCode = CompanyCode.Trim().ToUpperInvariant();

        if (!string.IsNullOrWhiteSpace(ApiUrl))
            config.ApiUrl = ApiUrlHelper.ResolveSyncEndpoint(ApiUrl);

        if (!string.IsNullOrWhiteSpace(ApiKey))
            config.ApiKey = ApiKey.Trim();

        config.SqlServer = SqlServer.Trim();
        config.Database = Database.Trim();
        config.SqlUsername = SqlUsername.Trim();

        if (!string.IsNullOrEmpty(SqlPassword))
            config.SqlPassword = SqlPassword;

        config.SyncIntervalSeconds = SyncIntervalSeconds;
        config.BatchSize = BatchSize;

        _configStore.Save(config);
    }

    private bool TryValidateConfiguration(out string? error)
    {
        if (string.IsNullOrWhiteSpace(CompanyName) || string.IsNullOrWhiteSpace(CompanyCode))
        {
            error = "Company Name and Company Code are required.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(ApiKey))
        {
            error = "API sync URL and API Key are required.";
            return false;
        }

        if (!ApiUrlHelper.TryValidateSyncEndpoint(ApiUrl, out error))
            return false;

        if (string.IsNullOrWhiteSpace(SqlServer) || string.IsNullOrWhiteSpace(Database))
        {
            error = "SQL Server and Database are required.";
            return false;
        }

        if (SyncIntervalSeconds < 5 || BatchSize < 1)
        {
            error = "Sync interval must be at least 5 seconds and batch size at least 1.";
            return false;
        }

        error = null;
        return true;
    }

    private async Task RunServiceAction(Func<Task> action, string successMessage)
    {
        IsBusy = true;
        try
        {
            await action();
            StatusMessage = successMessage;
            RefreshStatus();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(ex.Message, "Service Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static string FormatTime(DateTime? value) =>
        value?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") ?? "—";

    public void Dispose() => _refreshTimer.Stop();
}
