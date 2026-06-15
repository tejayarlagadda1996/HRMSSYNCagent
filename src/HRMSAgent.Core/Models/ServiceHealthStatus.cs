namespace HRMSAgent.Core.Models;

public enum HealthLevel
{
    Unknown,
    Healthy,
    Warning,
    Error
}

public class ServiceHealthStatus
{
    public string ServiceStatus { get; set; } = "Unknown";
    public HealthLevel ServiceHealth { get; set; } = HealthLevel.Unknown;
    public DateTime? LastSyncTime { get; set; }
    public DateTime? LastSuccessfulSyncTime { get; set; }
    public int PendingRecordsCount { get; set; }
    public HealthLevel ApiHealth { get; set; } = HealthLevel.Unknown;
    public HealthLevel SqlHealth { get; set; } = HealthLevel.Unknown;
    public int SyncIntervalSeconds { get; set; }
    public string Database { get; set; } = string.Empty;
    public string SqlServer { get; set; } = string.Empty;
    public string? LastError { get; set; }
}
