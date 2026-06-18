using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Principal;

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
        EnsureWritableDirectory(DataRoot);
        EnsureWritableDirectory(LogsDirectory);
    }

    [SupportedOSPlatform("windows")]
    private static void EnsureWritableDirectory(string path)
    {
        Directory.CreateDirectory(path);

        try
        {
            var directory = new DirectoryInfo(path);
            var security = directory.GetAccessControl();
            var users = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            var rule = new FileSystemAccessRule(
                users,
                FileSystemRights.Modify | FileSystemRights.ReadAndExecute | FileSystemRights.ListDirectory,
                InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                PropagationFlags.None,
                AccessControlType.Allow);

            security.ModifyAccessRule(AccessControlModification.Add, rule, out _);
            directory.SetAccessControl(security);
        }
        catch
        {
            // Best-effort; installer may already have set permissions.
        }
    }
}
