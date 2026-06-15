using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Constants;
using HRMSAgent.Core.Models;
using HRMSAgent.Core.Services;
using HRMSSyncService;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.File(
        Path.Combine(AgentPaths.LogsDirectory, ".log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    AgentPaths.EnsureDirectoriesExist();
    Log.Information("HRMSSyncService starting up");

    var builder = Host.CreateApplicationBuilder(args);
    builder.Services.AddWindowsService(options =>
    {
        options.ServiceName = AgentPaths.ServiceName;
    });

    builder.Services.AddSerilog();

    builder.Services.AddSingleton<IAgentConfigurationStore, AgentConfigurationStore>();
    builder.Services.AddSingleton<ICheckpointManager, CheckpointManager>();
    builder.Services.AddSingleton<IPendingRecordsStore, PendingRecordsStore>();
    builder.Services.AddSingleton<IDeviceLogTableResolver, DeviceLogTableResolver>();

    builder.Services.AddSingleton<Func<AgentConfiguration>>(sp =>
    {
        var store = sp.GetRequiredService<IAgentConfigurationStore>();
        return () => store.Exists() ? store.Load() : new AgentConfiguration();
    });

    builder.Services.AddSingleton<Func<string>>(sp =>
    {
        var store = sp.GetRequiredService<IAgentConfigurationStore>();
        return () => store.Load().BuildConnectionString();
    });

    builder.Services.AddSingleton<IAttendanceRepository, AttendanceRepository>();
    builder.Services.AddSingleton<IHrmsApiClient, HrmsApiClient>();
    builder.Services.AddSingleton<IAttendanceSyncEngine, AttendanceSyncEngine>();
    builder.Services.AddHostedService<SyncWorker>();

    var host = builder.Build();
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HRMSSyncService terminated unexpectedly");
    throw;
}
finally
{
    Log.Information("HRMSSyncService shutting down");
    Log.CloseAndFlush();
}
