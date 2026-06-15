namespace HRMSAgent.Core.Models;

public class SyncState
{
    public long LastDeviceLogId { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public DateTime? LastSuccessfulSyncTime { get; set; }
    public int PendingRecordsCount { get; set; }
    public string? CurrentDeviceLogTable { get; set; }
    public string? LastError { get; set; }
    public bool SqlHealthy { get; set; }
    public bool ApiHealthy { get; set; }
}
