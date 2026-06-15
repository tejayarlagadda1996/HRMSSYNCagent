using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using HRMSAgent.Core.Models;

namespace HRMSAgent.Core.Services;

public interface IAttendanceRepository
{
    Task<IReadOnlyList<AttendanceLog>> GetLogsAfterCheckpointAsync(
        long lastDeviceLogId,
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);
}

public class AttendanceRepository : IAttendanceRepository
{
    private readonly Func<string> _connectionStringFactory;
    private readonly IDeviceLogTableResolver _tableResolver;
    private readonly ILogger<AttendanceRepository> _logger;

    public AttendanceRepository(
        Func<string> connectionStringFactory,
        IDeviceLogTableResolver tableResolver,
        ILogger<AttendanceRepository> logger)
    {
        _connectionStringFactory = connectionStringFactory;
        _tableResolver = tableResolver;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionStringFactory());
        await connection.OpenAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<AttendanceLog>> GetLogsAfterCheckpointAsync(
        long lastDeviceLogId,
        int batchSize,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(_connectionStringFactory());
        await connection.OpenAsync(cancellationToken);

        var tables = await _tableResolver.ResolveActiveTablesAsync(connection, cancellationToken);
        if (tables.Count == 0)
            return [];

        var logs = new List<AttendanceLog>();

        foreach (var table in tables)
        {
            if (logs.Count >= batchSize)
                break;

            var remaining = batchSize - logs.Count;
            var tableLogs = await ReadFromTableAsync(
                connection,
                table,
                lastDeviceLogId,
                remaining,
                cancellationToken);

            logs.AddRange(tableLogs);
        }

        logs.Sort((a, b) => a.DeviceLogId.CompareTo(b.DeviceLogId));
        return logs.Take(batchSize).ToList();
    }

    private async Task<List<AttendanceLog>> ReadFromTableAsync(
        SqlConnection connection,
        string tableName,
        long lastDeviceLogId,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (!IsValidTableName(tableName))
            throw new InvalidOperationException($"Invalid table name: {tableName}");

        _logger.LogDebug("Reading from {Table} where DeviceLogId > {Checkpoint}", tableName, lastDeviceLogId);

        var sql = $"""
            SELECT TOP (@BatchSize)
                dl.DeviceLogId,
                dl.DeviceId,
                dl.UserId,
                dl.LogDate,
                dl.Direction
            FROM dbo.[{tableName}] dl
            WHERE dl.DeviceLogId > @LastDeviceLogId
            ORDER BY dl.DeviceLogId ASC
            """;

        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@LastDeviceLogId", lastDeviceLogId);

        var logs = new List<AttendanceLog>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            logs.Add(new AttendanceLog
            {
                DeviceLogId = reader.GetInt64(reader.GetOrdinal("DeviceLogId")),
                DeviceId = reader.GetInt32(reader.GetOrdinal("DeviceId")),
                EmployeeCode = reader.GetValue(reader.GetOrdinal("UserId"))?.ToString() ?? string.Empty,
                LogDate = reader.GetDateTime(reader.GetOrdinal("LogDate")),
                Direction = NormalizeDirection(reader.GetValue(reader.GetOrdinal("Direction"))?.ToString())
            });
        }

        return logs;
    }

    private static bool IsValidTableName(string tableName) =>
        System.Text.RegularExpressions.Regex.IsMatch(
            tableName,
            @"^DeviceLogs_\d+_\d{4}$",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

    private static string NormalizeDirection(string? direction)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return "in";

        var normalized = direction.Trim().ToLowerInvariant();
        return normalized switch
        {
            "0" or "in" or "i" or "checkin" or "check-in" => "in",
            "1" or "out" or "o" or "checkout" or "check-out" => "out",
            _ => normalized
        };
    }
}
