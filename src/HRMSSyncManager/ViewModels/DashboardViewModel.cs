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

        if (!string.IsNullOrWhiteSpace(health.LastError))
            StatusMessage = health.LastError;
    }

    [RelayCommand]
    private async Task StartServiceAsync() => await RunServiceAction(
        () => _serviceManager.StartAsync(),
        "Service started.");

    [RelayCommand]
    private async Task StopServiceAsync() => await RunServiceAction(
        () => _serviceManager.StopAsync(),
        "Service stopped.");

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
            SaveConfigurationInternal();
            StatusMessage = "Configuration saved.";
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
        _configStore.Save(new AgentConfiguration
        {
            CompanyName = CompanyName.Trim(),
            CompanyCode = CompanyCode.Trim().ToUpperInvariant(),
            ApiUrl = ApiUrl.Trim(),
            ApiKey = ApiKey.Trim(),
            SqlServer = SqlServer.Trim(),
            Database = Database.Trim(),
            SqlUsername = SqlUsername.Trim(),
            SqlPassword = SqlPassword,
            SyncIntervalSeconds = SyncIntervalSeconds,
            BatchSize = BatchSize
        });
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
