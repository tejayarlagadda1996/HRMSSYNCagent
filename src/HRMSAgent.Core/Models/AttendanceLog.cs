namespace HRMSAgent.Core.Models;

public class AttendanceLog
{
    public long DeviceLogId { get; set; }
    public int DeviceId { get; set; }
    public string EmployeeCode { get; set; } = string.Empty;
    public DateTime LogDate { get; set; }
    public string Direction { get; set; } = string.Empty;
}
