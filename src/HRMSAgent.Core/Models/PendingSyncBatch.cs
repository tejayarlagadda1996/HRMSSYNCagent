namespace HRMSAgent.Core.Models;

public class PendingSyncBatch
{
    public List<AttendanceLog> Records { get; set; } = [];
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int RetryCount { get; set; }
}
