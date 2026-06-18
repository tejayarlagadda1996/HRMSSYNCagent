using System.Runtime.Versioning;
using System.Security.Principal;

namespace HRMSAgent.Core.Services;

public static class WindowsAdminHelper
{
    [SupportedOSPlatform("windows")]
    public static bool IsRunningAsAdministrator()
    {
        using var identity = WindowsIdentity.GetCurrent();
        var principal = new WindowsPrincipal(identity);
        return principal.IsInRole(WindowsBuiltInRole.Administrator);
    }

    public const string AdminRequiredMessage =
        "Administrator access is required for this action. " +
        "Close the app, right-click HRMSAgent.exe, choose \"Run as administrator\", then try again.";
}
