using CommunityToolkit.Mvvm.ComponentModel;
using HRMSAgent.Core.Configuration;

namespace HRMSSyncManager.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IAgentConfigurationStore _configStore;

    [ObservableProperty]
    private bool _showSetupWizard;

    [ObservableProperty]
    private bool _showDashboard;

    public SetupWizardViewModel? SetupWizard { get; set; }
    public DashboardViewModel? Dashboard { get; set; }

    public MainViewModel(IAgentConfigurationStore configStore)
    {
        _configStore = configStore;
        RefreshView();
    }

    public void RefreshView()
    {
        var exists = _configStore.Exists();
        if (exists)
            Dashboard?.ReloadFromStore();

        ShowSetupWizard = !exists;
        ShowDashboard = exists;
    }
}
