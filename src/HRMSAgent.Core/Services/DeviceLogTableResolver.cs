using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace HRMSAgent.Core.Services;

public interface IDeviceLogTableResolver
{
    Task<IReadOnlyList<string>> ResolveActiveTablesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Resolves eSSL eTimeTrackLite DeviceLogs tables (e.g. DeviceLogs_6_2026).
/// Never hardcodes month names — derives candidates from the current date.
/// </summary>
public partial class DeviceLogTableResolver(ILogger<DeviceLogTableResolver> logger) : IDeviceLogTableResolver
{
    private static readonly Regex TablePattern = DeviceLogsTableRegex();

    public async Task<IReadOnlyList<string>> ResolveActiveTablesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken = default)
    {
        var allTables = await GetDeviceLogTablesAsync(connection, cancellationToken);
        if (allTables.Count == 0)
        {
            logger.LogWarning("No DeviceLogs_* tables found in database.");
            return [];
        }

        var now = DateTime.Now;
        var currentKey = $"{now.Month}_{now.Year}";

        var parsed = allTables
            .Select(ParseTable)
            .Where(p => p is not null)
            .Select(p => p!)
            .OrderBy(p => p.Year)
            .ThenBy(p => p.Period)
            .ToList();

        var current = parsed
            .Where(p => $"{p.Period}_{p.Year}" == currentKey)
            .Select(p => p.TableName)
            .ToList();

        if (current.Count > 0)
            return current;

        // Fallback: most recent table for current year, then overall latest.
        var currentYearTables = parsed
            .Where(p => p.Year == now.Year)
            .Select(p => p.TableName)
            .ToList();

        if (currentYearTables.Count > 0)
            return [currentYearTables[^1]];

        return [parsed[^1].TableName];
    }

    private static async Task<List<string>> GetDeviceLogTablesAsync(
        SqlConnection connection,
        CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT TABLE_NAME
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE'
              AND TABLE_NAME LIKE 'DeviceLogs[_]%'
            ORDER BY TABLE_NAME
            """;

        var tables = new List<string>();
        await using var command = new SqlCommand(sql, connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
            tables.Add(reader.GetString(0));

        return tables;
    }

    private static DeviceLogTableInfo? ParseTable(string tableName)
    {
        var match = TablePattern.Match(tableName);
        if (!match.Success)
            return null;

        if (!int.TryParse(match.Groups["period"].Value, out var period))
            return null;

        if (!int.TryParse(match.Groups["year"].Value, out var year))
            return null;

        return new DeviceLogTableInfo(tableName, period, year);
    }

    [GeneratedRegex(@"^DeviceLogs_(?<period>\d+)_(?<year>\d{4})$", RegexOptions.IgnoreCase)]
    private static partial Regex DeviceLogsTableRegex();

    private sealed record DeviceLogTableInfo(string TableName, int Period, int Year);
}
