using System.Text.Json;
using HRMSAgent.Core.Constants;
using HRMSAgent.Core.Models;

namespace HRMSAgent.Core.Services;

public interface ICheckpointManager
{
    SyncState Load();
    void Save(SyncState state);
    void UpdateCheckpoint(long lastDeviceLogId, string? tableName = null);
}

public class CheckpointManager : ICheckpointManager
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _lock = new();

    public SyncState Load()
    {
        lock (_lock)
        {
            if (!File.Exists(AgentPaths.SyncStateFile))
                return new SyncState();

            try
            {
                var json = File.ReadAllText(AgentPaths.SyncStateFile);
                return JsonSerializer.Deserialize<SyncState>(json, JsonOptions) ?? new SyncState();
            }
            catch
            {
                return new SyncState();
            }
        }
    }

    public void Save(SyncState state)
    {
        lock (_lock)
        {
            AgentPaths.EnsureDirectoriesExist();
            var json = JsonSerializer.Serialize(state, JsonOptions);
            File.WriteAllText(AgentPaths.SyncStateFile, json);
        }
    }

    public void UpdateCheckpoint(long lastDeviceLogId, string? tableName = null)
    {
        var state = Load();
        state.LastDeviceLogId = lastDeviceLogId;
        state.LastSyncTime = DateTime.UtcNow;
        if (tableName is not null)
            state.CurrentDeviceLogTable = tableName;
        Save(state);
    }
}
