using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Services;

namespace HRMSSyncService;

public class SyncWorker : BackgroundService
{
    private readonly ILogger<SyncWorker> _logger;
    private readonly IAgentConfigurationStore _configStore;
    private readonly IAttendanceSyncEngine _syncEngine;

    public SyncWorker(
        ILogger<SyncWorker> logger,
        IAgentConfigurationStore configStore,
        IAttendanceSyncEngine syncEngine)
    {
        _logger = logger;
        _configStore = configStore;
        _syncEngine = syncEngine;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("HRMS Attendance Sync Service is running");

        while (!stoppingToken.IsCancellationRequested)
        {
            var intervalSeconds = 30;

            try
            {
                if (_configStore.Exists())
                {
                    var config = _configStore.Load();
                    intervalSeconds = Math.Max(5, config.SyncIntervalSeconds);

                    _logger.LogInformation(
                        "Starting sync cycle for {CompanyCode} (interval: {Interval}s)",
                        config.CompanyCode,
                        intervalSeconds);

                    await _syncEngine.RunSyncCycleAsync(stoppingToken);
                }
                else
                {
                    _logger.LogWarning(
                        "Configuration not found at {Path}. Waiting for setup.",
                        HRMSAgent.Core.Constants.AgentPaths.ConfigFile);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error during sync cycle");
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("HRMS Attendance Sync Service is stopping");
    }
}
