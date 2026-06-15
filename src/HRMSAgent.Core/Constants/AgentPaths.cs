namespace HRMSAgent.Core.Constants;

public static class AgentPaths
{
    public const string ServiceName = "HRMSSyncService";
    public const string DataRoot = @"C:\ProgramData\HRMSAgent";
    public const string ConfigFile = @"C:\ProgramData\HRMSAgent\config.json";
    public const string SyncStateFile = @"C:\ProgramData\HRMSAgent\syncstate.json";
    public const string PendingFile = @"C:\ProgramData\HRMSAgent\pending.json";
    public const string LogsDirectory = @"C:\ProgramData\HRMSAgent\Logs";

    public static void EnsureDirectoriesExist()
    {
        Directory.CreateDirectory(DataRoot);
        Directory.CreateDirectory(LogsDirectory);
    }
}
