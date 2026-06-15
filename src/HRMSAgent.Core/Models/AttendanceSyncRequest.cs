using System.Text.Json.Serialization;

namespace HRMSAgent.Core.Models;

public class AttendanceSyncRequest
{
    [JsonPropertyName("companyCode")]
    public string CompanyCode { get; set; } = string.Empty;

    [JsonPropertyName("logs")]
    public List<AttendanceSyncLogDto> Logs { get; set; } = [];
}

public class AttendanceSyncLogDto
{
    [JsonPropertyName("deviceLogId")]
    public long DeviceLogId { get; set; }

    [JsonPropertyName("employeeCode")]
    public string EmployeeCode { get; set; } = string.Empty;

    [JsonPropertyName("deviceId")]
    public int DeviceId { get; set; }

    [JsonPropertyName("logDate")]
    public DateTime LogDate { get; set; }

    [JsonPropertyName("direction")]
    public string Direction { get; set; } = string.Empty;
}

public class AttendanceSyncResponse
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("acceptedCount")]
    public int AcceptedCount { get; set; }
}
