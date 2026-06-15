using HRMSSyncManager.ViewModels;

namespace HRMSSyncManager.Views;

public partial class MainWindow
{
    public MainWindow(MainViewModel mainViewModel, SetupWizardViewModel setupWizard, DashboardViewModel dashboard)
    {
        mainViewModel.SetupWizard = setupWizard;
        mainViewModel.Dashboard = dashboard;
        DataContext = mainViewModel;
        InitializeComponent();
    }
}
