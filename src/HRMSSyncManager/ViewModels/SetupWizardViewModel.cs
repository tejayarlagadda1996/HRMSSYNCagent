using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Models;
using HRMSAgent.Core.Services;

namespace HRMSSyncManager.ViewModels;

public partial class SetupWizardViewModel : ObservableObject
{
    private readonly IAgentConfigurationStore _configStore;
    private readonly ISqlInstanceDetector _sqlDetector;
    private readonly ISqlConnectionTester _sqlTester;
    private readonly IHrmsApiClient _apiClient;
    private readonly IWindowsServiceManager _serviceManager;
    private readonly MainViewModel _mainViewModel;

    [ObservableProperty] private int _currentStep = 1;
    [ObservableProperty] private string _companyName = string.Empty;
    [ObservableProperty] private string _companyCode = string.Empty;
    [ObservableProperty] private string _apiUrl = string.Empty;
    [ObservableProperty] private string _apiKey = string.Empty;
    [ObservableProperty] private string _sqlServer = string.Empty;
    [ObservableProperty] private string _database = "etimetracklite1";
    [ObservableProperty] private string _sqlUsername = string.Empty;
    [ObservableProperty] private string _sqlPassword = string.Empty;
    [ObservableProperty] private int _syncIntervalSeconds = 30;
    [ObservableProperty] private int _batchSize = 100;
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public ObservableCollection<string> SqlInstances { get; } = [];

    public SetupWizardViewModel(
        IAgentConfigurationStore configStore,
        ISqlInstanceDetector sqlDetector,
        ISqlConnectionTester sqlTester,
        IHrmsApiClient apiClient,
        IWindowsServiceManager serviceManager,
        MainViewModel mainViewModel)
    {
        _configStore = configStore;
        _sqlDetector = sqlDetector;
        _sqlTester = sqlTester;
        _apiClient = apiClient;
        _serviceManager = serviceManager;
        _mainViewModel = mainViewModel;

        _ = DetectSqlInstancesAsync();
    }

    [RelayCommand]
    private void Next()
    {
        if (!ValidateCurrentStep())
            return;

        if (CurrentStep < 5)
            CurrentStep++;
    }

    [RelayCommand]
    private void Back()
    {
        if (CurrentStep > 1)
            CurrentStep--;
    }

    [RelayCommand]
    private async Task DetectSqlInstancesAsync()
    {
        IsBusy = true;
        StatusMessage = "Detecting SQL instances...";

        try
        {
            SqlInstances.Clear();
            var candidates = _sqlDetector.DetectInstances();
            var database = Database.Trim();
            var matched = new List<string>();

            if (!string.IsNullOrWhiteSpace(database))
            {
                foreach (var instance in candidates)
                {
                    try
                    {
                        if (await _sqlTester.TestConnectionAsync(instance, database, SqlUsername, SqlPassword))
                            matched.Add(instance);
                    }
                    catch
                    {
                        // Instance exists but does not host this database or is unreachable.
                    }
                }
            }

            var toShow = matched.Count > 0 ? matched : candidates;
            foreach (var instance in toShow)
                SqlInstances.Add(instance);

            if (SqlInstances.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(SqlServer)
                    || !SqlInstances.Any(i => i.Equals(SqlServer, StringComparison.OrdinalIgnoreCase)))
                    SqlServer = SqlInstances[0];
            }

            StatusMessage = matched.Count > 0
                ? $"Found {matched.Count} instance(s) with database '{database}'."
                : candidates.Count > 0
                    ? $"Found {candidates.Count} SQL instance(s). None have database '{database}'."
                    : "No SQL instances found.";
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
            _configStore.Save(BuildConfiguration());
            var success = await _apiClient.TestConnectionAsync();
            StatusMessage = success
                ? "API connection successful."
                : "API connection failed. Check URL and API key.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"API test error: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task TestSqlConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(SqlServer) || string.IsNullOrWhiteSpace(Database))
        {
            StatusMessage = "Enter SQL Server and Database first.";
            return;
        }

        IsBusy = true;
        StatusMessage = "Testing SQL connection...";

        try
        {
            await _sqlTester.TestConnectionAsync(SqlServer, Database, SqlUsername, SqlPassword);
            StatusMessage = "SQL connection successful.";
        }
        catch (Exception ex)
        {
            StatusMessage = $"SQL connection failed: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task FinishAsync()
    {
        if (!ValidateAllSteps())
            return;

        IsBusy = true;
        StatusMessage = "Saving configuration and installing service...";

        try
        {
            _configStore.Save(BuildConfiguration());

            var serviceExe = Environment.ProcessPath
                ?? Path.Combine(AppContext.BaseDirectory, "HRMSAgent.exe");

            if (!File.Exists(serviceExe))
                throw new FileNotFoundException(
                    "HRMSAgent.exe not found.",
                    serviceExe);

            if (!_serviceManager.IsInstalled())
                await _serviceManager.InstallAsync(serviceExe);

            await _serviceManager.StartAsync();

            StatusMessage = "Setup complete. Service installed and started.";
            _mainViewModel.RefreshView();
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
            MessageBox.Show(
                ex.Message,
                "Setup Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private AgentConfiguration BuildConfiguration() => new()
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
    };

    private bool ValidateCurrentStep()
    {
        StatusMessage = string.Empty;

        switch (CurrentStep)
        {
            case 1:
                if (string.IsNullOrWhiteSpace(CompanyName) || string.IsNullOrWhiteSpace(CompanyCode))
                {
                    StatusMessage = "Company Name and Company Code are required.";
                    return false;
                }
                break;
            case 2:
                if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(ApiKey))
                {
                    StatusMessage = "API sync URL and API Key are required.";
                    return false;
                }

                if (!ApiUrlHelper.TryValidateSyncEndpoint(ApiUrl, out var apiError))
                {
                    StatusMessage = apiError!;
                    return false;
                }
                break;
            case 3:
                if (string.IsNullOrWhiteSpace(SqlServer) || string.IsNullOrWhiteSpace(Database))
                {
                    StatusMessage = "SQL Server and Database are required.";
                    return false;
                }
                break;
            case 4:
                if (SyncIntervalSeconds < 5 || BatchSize < 1)
                {
                    StatusMessage = "Sync interval must be at least 5 seconds and batch size at least 1.";
                    return false;
                }
                break;
        }

        return true;
    }

    private bool ValidateAllSteps()
    {
        for (var step = 1; step <= 4; step++)
        {
            var previousStep = CurrentStep;
            CurrentStep = step;
            if (!ValidateCurrentStep())
                return false;
            CurrentStep = previousStep;
        }

        return true;
    }
}
