using System.Text.Json;
using HRMSAgent.Core.Constants;
using HRMSAgent.Core.Models;

namespace HRMSAgent.Core.Services;

public interface IPendingRecordsStore
{
    IReadOnlyList<AttendanceLog> LoadAll();
    void Save(IEnumerable<AttendanceLog> records);
    void Clear();
    int Count { get; }
}

public class PendingRecordsStore : IPendingRecordsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly object _lock = new();
    private List<AttendanceLog> _cache = [];

    public PendingRecordsStore()
    {
        _cache = LoadFromDisk();
    }

    public int Count
    {
        get
        {
            lock (_lock)
                return _cache.Count;
        }
    }

    public IReadOnlyList<AttendanceLog> LoadAll()
    {
        lock (_lock)
            return _cache.ToList();
    }

    public void Save(IEnumerable<AttendanceLog> records)
    {
        lock (_lock)
        {
            var existingIds = _cache.Select(r => r.DeviceLogId).ToHashSet();
            foreach (var record in records)
            {
                if (existingIds.Add(record.DeviceLogId))
                    _cache.Add(record);
            }

            _cache.Sort((a, b) => a.DeviceLogId.CompareTo(b.DeviceLogId));
            Persist();
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _cache.Clear();
            Persist();
        }
    }

    private List<AttendanceLog> LoadFromDisk()
    {
        if (!File.Exists(AgentPaths.PendingFile))
            return [];

        try
        {
            var json = File.ReadAllText(AgentPaths.PendingFile);
            var batch = JsonSerializer.Deserialize<PendingSyncBatch>(json, JsonOptions);
            return batch?.Records ?? [];
        }
        catch
        {
            return [];
        }
    }

    private void Persist()
    {
        AgentPaths.EnsureDirectoriesExist();
        var batch = new PendingSyncBatch { Records = _cache };
        var json = JsonSerializer.Serialize(batch, JsonOptions);
        File.WriteAllText(AgentPaths.PendingFile, json);
    }
}
