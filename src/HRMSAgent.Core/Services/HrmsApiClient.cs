using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using HRMSAgent.Core.Models;

namespace HRMSAgent.Core.Services;

public interface IHrmsApiClient
{
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
    Task<AttendanceSyncResponse> SyncAttendanceAsync(
        IEnumerable<AttendanceLog> logs,
        CancellationToken cancellationToken = default);
}

public class HrmsApiClient : IHrmsApiClient, IDisposable
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly Func<AgentConfiguration> _configFactory;
    private readonly ILogger<HrmsApiClient> _logger;
    private readonly HttpClient _httpClient;

    public HrmsApiClient(
        Func<AgentConfiguration> configFactory,
        ILogger<HrmsApiClient> logger,
        HttpClient? httpClient = null)
    {
        _configFactory = configFactory;
        _logger = logger;
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var config = _configFactory();
        ConfigureClient(config);

        var request = new HttpRequestMessage(HttpMethod.Post, BuildSyncUrl(config))
        {
            Content = JsonContent.Create(new AttendanceSyncRequest
            {
                CompanyCode = config.CompanyCode,
                Logs = []
            }, options: JsonOptions)
        };

        try
        {
            var response = await _httpClient.SendAsync(request, cancellationToken);
            _logger.LogInformation("API test response: {StatusCode}", response.StatusCode);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "API connection test failed");
            return false;
        }
    }

    public async Task<AttendanceSyncResponse> SyncAttendanceAsync(
        IEnumerable<AttendanceLog> logs,
        CancellationToken cancellationToken = default)
    {
        var config = _configFactory();
        ConfigureClient(config);

        var logList = logs.ToList();
        var payload = new AttendanceSyncRequest
        {
            CompanyCode = config.CompanyCode,
            Logs = logList.Select(l => new AttendanceSyncLogDto
            {
                DeviceLogId = l.DeviceLogId,
                EmployeeCode = l.EmployeeCode,
                DeviceId = l.DeviceId,
                LogDate = l.LogDate,
                Direction = l.Direction
            }).ToList()
        };

        _logger.LogInformation("Sending {Count} attendance records to API", logList.Count);

        var response = await _httpClient.PostAsJsonAsync(
            BuildSyncUrl(config),
            payload,
            JsonOptions,
            cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "API sync failed with status {StatusCode}: {Body}",
                response.StatusCode,
                body);

            return new AttendanceSyncResponse
            {
                Success = false,
                Message = $"HTTP {(int)response.StatusCode}: {body}"
            };
        }

        var result = await response.Content.ReadFromJsonAsync<AttendanceSyncResponse>(
            JsonOptions,
            cancellationToken);

        _logger.LogInformation(
            "API sync succeeded. Accepted: {Count}",
            result?.AcceptedCount ?? logList.Count);

        return result ?? new AttendanceSyncResponse
        {
            Success = true,
            AcceptedCount = logList.Count
        };
    }

    private void ConfigureClient(AgentConfiguration config)
    {
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", config.ApiKey);
    }

    private static string BuildSyncUrl(AgentConfiguration config) =>
        $"{config.ApiUrl.TrimEnd('/')}/api/attendance/sync";

    public void Dispose() => _httpClient.Dispose();
}
