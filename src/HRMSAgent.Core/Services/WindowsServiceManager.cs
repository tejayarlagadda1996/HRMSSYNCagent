using System.Diagnostics;
using System.Runtime.Versioning;
using System.ServiceProcess;
using HRMSAgent.Core.Constants;
using HRMSAgent.Core.Models;

namespace HRMSAgent.Core.Services;

public interface IWindowsServiceManager
{
    string ServiceName { get; }
    bool IsInstalled();
    string GetStatus();
    Task StartAsync();
    Task StopAsync();
    Task RestartAsync();
    Task InstallAsync(string serviceExecutablePath);
    Task UninstallAsync();
    ServiceHealthStatus GetHealthOverview(AgentConfiguration? config);
}

[SupportedOSPlatform("windows")]
public class WindowsServiceManager : IWindowsServiceManager
{
    public string ServiceName => AgentPaths.ServiceName;

    public bool IsInstalled()
    {
        try
        {
            return ServiceController.GetServices()
                .Any(s => s.ServiceName.Equals(ServiceName, StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return false;
        }
    }

    public string GetStatus()
    {
        if (!IsInstalled())
            return "Not Installed";

        try
        {
            using var sc = new ServiceController(ServiceName);
            return sc.Status.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task StartAsync()
    {
        await RunScAsync($"start {ServiceName}");
    }

    public async Task StopAsync()
    {
        await RunScAsync($"stop {ServiceName}");
    }

    public async Task RestartAsync()
    {
        if (GetStatus() == "Running")
            await StopAsync();

        await StartAsync();
    }

    public async Task InstallAsync(string serviceExecutablePath)
    {
        var binPath = $"\"{serviceExecutablePath}\"";
        await RunScAsync($"create {ServiceName} binPath= {binPath} start= auto DisplayName= \"HRMS Attendance Sync Service\"");

        try
        {
            await RunScAsync($"failure {ServiceName} reset= 86400 actions= restart/60000/restart/60000/restart/60000");
            await RunScAsync($"description {ServiceName} \"Synchronizes eSSL attendance data to HRMS cloud API\"");
        }
        catch
        {
            // Non-fatal if failure actions cannot be set.
        }
    }

    public async Task UninstallAsync()
    {
        if (GetStatus() == "Running")
            await StopAsync();

        await RunScAsync($"delete {ServiceName}");
    }

    public ServiceHealthStatus GetHealthOverview(AgentConfiguration? config)
    {
        var checkpoint = new CheckpointManager();
        var pending = new PendingRecordsStore();
        var state = checkpoint.Load();

        var status = new ServiceHealthStatus
        {
            ServiceStatus = GetStatus(),
            LastSyncTime = state.LastSyncTime,
            LastSuccessfulSyncTime = state.LastSuccessfulSyncTime,
            PendingRecordsCount = Math.Max(state.PendingRecordsCount, pending.Count),
            SyncIntervalSeconds = config?.SyncIntervalSeconds ?? 30,
            Database = config?.Database ?? string.Empty,
            SqlServer = config?.SqlServer ?? string.Empty,
            LastError = state.LastError
        };

        status.ServiceHealth = status.ServiceStatus switch
        {
            "Running" => HealthLevel.Healthy,
            "Stopped" => HealthLevel.Warning,
            "Not Installed" => HealthLevel.Error,
            _ when status.ServiceStatus.StartsWith("Error", StringComparison.OrdinalIgnoreCase) => HealthLevel.Error,
            _ => HealthLevel.Warning
        };

        status.ApiHealth = state.ApiHealthy ? HealthLevel.Healthy : HealthLevel.Error;
        status.SqlHealth = state.SqlHealthy ? HealthLevel.Healthy : HealthLevel.Error;

        if (status.PendingRecordsCount > 0)
            status.ApiHealth = HealthLevel.Warning;

        return status;
    }

    private static async Task RunScAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "sc.exe",
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start sc.exe");

        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            var output = await process.StandardOutput.ReadToEndAsync();
            throw new InvalidOperationException(
                $"sc.exe {arguments} failed ({process.ExitCode}): {error} {output}".Trim());
        }
    }
}
