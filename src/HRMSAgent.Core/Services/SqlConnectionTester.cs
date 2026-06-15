using Microsoft.Data.SqlClient;

namespace HRMSAgent.Core.Services;

public interface ISqlConnectionTester
{
    Task<bool> TestConnectionAsync(
        string server,
        string database,
        string username,
        string password,
        CancellationToken cancellationToken = default);
}

public class SqlConnectionTester : ISqlConnectionTester
{
    public async Task<bool> TestConnectionAsync(
        string server,
        string database,
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = database,
            TrustServerCertificate = true,
            Encrypt = true
        };

        if (!string.IsNullOrWhiteSpace(username))
        {
            builder.UserID = username;
            builder.Password = password;
            builder.IntegratedSecurity = false;
        }
        else
        {
            builder.IntegratedSecurity = true;
        }

        await using var connection = new SqlConnection(builder.ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return true;
    }
}
