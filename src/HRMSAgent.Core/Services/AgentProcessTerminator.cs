using System.Diagnostics;
using System.Runtime.Versioning;

namespace HRMSAgent.Core.Services;

[SupportedOSPlatform("windows")]
public static class AgentProcessTerminator
{
    public static void KillAll(bool excludeCurrentProcess = false)
    {
        var currentPid = excludeCurrentProcess ? Process.GetCurrentProcess().Id : -1;

        foreach (var process in Process.GetProcessesByName("HRMSAgent"))
        {
            try
            {
                if (process.Id == currentPid)
                    continue;

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch
            {
                // Process may already have exited or require elevation.
            }
            finally
            {
                process.Dispose();
            }
        }
    }
}
