using System.Text.Json.Serialization;

namespace HRMSAgent.Core.Models;

public class AgentConfiguration
{
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyCode { get; set; } = string.Empty;
    /// <summary>Full POST URL for attendance sync, including endpoint path.</summary>
    public string ApiUrl { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string SqlServer { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string SqlUsername { get; set; } = string.Empty;

    [JsonIgnore]
    public string SqlPassword { get; set; } = string.Empty;

    /// <summary>DPAPI-protected password stored in config.json.</summary>
    public string? EncryptedSqlPassword { get; set; }

    public int SyncIntervalSeconds { get; set; } = 30;
    public int BatchSize { get; set; } = 100;

    public string BuildConnectionString()
    {
        var builder = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = SqlServer,
            InitialCatalog = Database,
            TrustServerCertificate = true,
            Encrypt = true
        };

        if (!string.IsNullOrWhiteSpace(SqlUsername))
        {
            builder.UserID = SqlUsername;
            builder.Password = SqlPassword;
            builder.IntegratedSecurity = false;
        }
        else
        {
            builder.IntegratedSecurity = true;
        }

        return builder.ConnectionString;
    }
}
