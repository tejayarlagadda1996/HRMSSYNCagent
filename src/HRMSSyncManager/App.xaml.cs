using System.Windows;
using HRMSSyncManager.ViewModels;
using HRMSSyncManager.Views;
using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace HRMSSyncManager;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton<IAgentConfigurationStore, AgentConfigurationStore>();
        services.AddSingleton<ICheckpointManager, CheckpointManager>();
        services.AddSingleton<IPendingRecordsStore, PendingRecordsStore>();
        services.AddSingleton<ISqlInstanceDetector, SqlInstanceDetector>();
        services.AddSingleton<ISqlConnectionTester, SqlConnectionTester>();
        services.AddSingleton<IWindowsServiceManager, WindowsServiceManager>();

        services.AddSingleton<Func<HRMSAgent.Core.Models.AgentConfiguration>>(sp =>
        {
            var store = sp.GetRequiredService<IAgentConfigurationStore>();
            return () => store.Exists()
                ? store.Load()
                : new HRMSAgent.Core.Models.AgentConfiguration();
        });

        services.AddSingleton<IHrmsApiClient>(sp =>
            new HrmsApiClient(
                sp.GetRequiredService<Func<HRMSAgent.Core.Models.AgentConfiguration>>(),
                Microsoft.Extensions.Logging.Abstractions.NullLogger<HrmsApiClient>.Instance));

        services.AddSingleton<MainViewModel>();
        services.AddSingleton<SetupWizardViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<MainWindow>();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }
}
