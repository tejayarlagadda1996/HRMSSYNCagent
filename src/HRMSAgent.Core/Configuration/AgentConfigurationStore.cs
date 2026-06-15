using System.Text.Json;
using HRMSAgent.Core.Constants;
using HRMSAgent.Core.Models;
using HRMSAgent.Core.Security;

namespace HRMSAgent.Core.Configuration;

public interface IAgentConfigurationStore
{
    bool Exists();
    AgentConfiguration Load();
    void Save(AgentConfiguration configuration);
}

public class AgentConfigurationStore : IAgentConfigurationStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public bool Exists() => File.Exists(AgentPaths.ConfigFile);

    public AgentConfiguration Load()
    {
        if (!Exists())
            throw new FileNotFoundException("Agent configuration not found.", AgentPaths.ConfigFile);

        var json = File.ReadAllText(AgentPaths.ConfigFile);
        var config = JsonSerializer.Deserialize<AgentConfiguration>(json, JsonOptions)
            ?? new AgentConfiguration();

        if (!string.IsNullOrEmpty(config.EncryptedSqlPassword))
            config.SqlPassword = CredentialProtector.Unprotect(config.EncryptedSqlPassword);

        return config;
    }

    public void Save(AgentConfiguration configuration)
    {
        AgentPaths.EnsureDirectoriesExist();

        var toSave = new AgentConfiguration
        {
            CompanyName = configuration.CompanyName,
            CompanyCode = configuration.CompanyCode,
            ApiUrl = configuration.ApiUrl.TrimEnd('/'),
            ApiKey = configuration.ApiKey,
            SqlServer = configuration.SqlServer,
            Database = configuration.Database,
            SqlUsername = configuration.SqlUsername,
            EncryptedSqlPassword = string.IsNullOrEmpty(configuration.SqlPassword)
                ? configuration.EncryptedSqlPassword
                : CredentialProtector.Protect(configuration.SqlPassword),
            SyncIntervalSeconds = configuration.SyncIntervalSeconds,
            BatchSize = configuration.BatchSize
        };

        var json = JsonSerializer.Serialize(toSave, JsonOptions);
        File.WriteAllText(AgentPaths.ConfigFile, json);
    }
}
