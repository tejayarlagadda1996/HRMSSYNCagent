using Microsoft.Extensions.Logging;
using HRMSAgent.Core.Configuration;
using HRMSAgent.Core.Models;

namespace HRMSAgent.Core.Services;

public interface IAttendanceSyncEngine
{
    Task RunSyncCycleAsync(CancellationToken cancellationToken = default);
}

public class AttendanceSyncEngine : IAttendanceSyncEngine
{
    private readonly IAgentConfigurationStore _configStore;
    private readonly IAttendanceRepository _repository;
    private readonly IHrmsApiClient _apiClient;
    private readonly ICheckpointManager _checkpointManager;
    private readonly IPendingRecordsStore _pendingStore;
    private readonly ILogger<AttendanceSyncEngine> _logger;

    public AttendanceSyncEngine(
        IAgentConfigurationStore configStore,
        IAttendanceRepository repository,
        IHrmsApiClient apiClient,
        ICheckpointManager checkpointManager,
        IPendingRecordsStore pendingStore,
        ILogger<AttendanceSyncEngine> logger)
    {
        _configStore = configStore;
        _repository = repository;
        _apiClient = apiClient;
        _checkpointManager = checkpointManager;
        _pendingStore = pendingStore;
        _logger = logger;
    }

    public async Task RunSyncCycleAsync(CancellationToken cancellationToken = default)
    {
        if (!_configStore.Exists())
        {
            _logger.LogWarning("Configuration not found. Skipping sync cycle.");
            return;
        }

        var config = _configStore.Load();
        var state = _checkpointManager.Load();
        var recordsToSync = new List<AttendanceLog>();

        try
        {
            await _repository.TestConnectionAsync(cancellationToken);
            state.SqlHealthy = true;
        }
        catch (Exception ex)
        {
            state.SqlHealthy = false;
            state.LastError = $"SQL: {ex.Message}";
            state.LastSyncTime = DateTime.UtcNow;
            _checkpointManager.Save(state);
            _logger.LogError(ex, "SQL connection failed");
            throw;
        }

        var pendingRecords = _pendingStore.LoadAll();
        recordsToSync.AddRange(pendingRecords);

        var newLogs = new List<AttendanceLog>();
        if (recordsToSync.Count < config.BatchSize)
        {
            newLogs = (await _repository.GetLogsAfterCheckpointAsync(
                state.LastDeviceLogId,
                config.BatchSize - recordsToSync.Count,
                cancellationToken)).ToList();

            var existingIds = recordsToSync.Select(r => r.DeviceLogId).ToHashSet();
            foreach (var log in newLogs)
            {
                if (existingIds.Add(log.DeviceLogId))
                    recordsToSync.Add(log);
            }
        }

        recordsToSync.Sort((a, b) => a.DeviceLogId.CompareTo(b.DeviceLogId));

        if (recordsToSync.Count == 0)
        {
            state.LastSyncTime = DateTime.UtcNow;
            state.PendingRecordsCount = _pendingStore.Count;
            _checkpointManager.Save(state);
            _logger.LogDebug("No new attendance records to sync.");
            return;
        }

        _logger.LogInformation("Syncing {Count} attendance records", recordsToSync.Count);

        AttendanceSyncResponse response;
        try
        {
            response = await _apiClient.SyncAttendanceAsync(recordsToSync, cancellationToken);
            state.ApiHealthy = response.Success;
        }
        catch (Exception ex)
        {
            state.ApiHealthy = false;
            state.LastError = $"API: {ex.Message}";
            state.LastSyncTime = DateTime.UtcNow;
            state.PendingRecordsCount = _pendingStore.Count + recordsToSync.Count;
            _pendingStore.Save(recordsToSync);
            _checkpointManager.Save(state);
            _logger.LogError(ex, "API sync failed; records saved to pending store");
            throw;
        }

        if (!response.Success)
        {
            state.ApiHealthy = false;
            state.LastError = response.Message ?? "API sync failed";
            state.LastSyncTime = DateTime.UtcNow;
            state.PendingRecordsCount = _pendingStore.Count + recordsToSync.Count;
            _pendingStore.Save(recordsToSync);
            _checkpointManager.Save(state);
            _logger.LogError("API sync failed: {Message}", response.Message);
            return;
        }

        _pendingStore.Clear();

        if (newLogs.Count > 0)
            state.LastDeviceLogId = newLogs.Max(r => r.DeviceLogId);

        state.LastSyncTime = DateTime.UtcNow;
        state.LastSuccessfulSyncTime = DateTime.UtcNow;
        state.PendingRecordsCount = 0;
        state.ApiHealthy = true;
        state.LastError = null;

        _checkpointManager.Save(state);
        _logger.LogInformation(
            "Sync successful. Checkpoint updated to DeviceLogId {Checkpoint}",
            state.LastDeviceLogId);
    }
}
