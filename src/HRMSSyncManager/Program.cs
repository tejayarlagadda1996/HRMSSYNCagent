using System.Windows;
using HRMSSyncManager.Services;

namespace HRMSSyncManager;

public static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        if (args.Contains("--service", StringComparer.OrdinalIgnoreCase))
        {
            AgentServiceHost.Run(args);
            return;
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
