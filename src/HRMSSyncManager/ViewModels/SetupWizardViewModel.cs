using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Constants;
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

        DetectSqlInstances();
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
    private void DetectSqlInstances()
    {
        SqlInstances.Clear();
        foreach (var instance in _sqlDetector.DetectInstances())
            SqlInstances.Add(instance);

        if (SqlInstances.Count > 0 && string.IsNullOrWhiteSpace(SqlServer))
            SqlServer = SqlInstances[0];
    }

    [RelayCommand]
    private async Task TestApiConnectionAsync()
    {
        if (string.IsNullOrWhiteSpace(ApiUrl) || string.IsNullOrWhiteSpace(ApiKey))
        {
            StatusMessage = "Enter API URL and API Key first.";
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
        if (!ValidateCurrentStep())
            return;

        IsBusy = true;
        StatusMessage = "Saving configuration and installing service...";

        try
        {
            _configStore.Save(BuildConfiguration());

            var serviceExe = Path.Combine(
                AppContext.BaseDirectory,
                "HRMSSyncService.exe");

            if (!File.Exists(serviceExe))
            {
                serviceExe = Path.Combine(
                    AppContext.BaseDirectory,
                    "service",
                    "HRMSSyncService.exe");
            }

            if (!File.Exists(serviceExe))
                throw new FileNotFoundException(
                    "HRMSSyncService.exe not found next to the manager application.",
                    serviceExe);

            if (!_serviceManager.IsInstalled())
                await _serviceManager.InstallAsync(serviceExe);

            await _serviceManager.StartAsync();

            StatusMessage = "Setup complete. Service installed and started.";
            _mainViewModel.RefreshView();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Setup failed: {ex.Message}";
            MessageBox.Show(
                StatusMessage,
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
                    StatusMessage = "API URL and API Key are required.";
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
}
