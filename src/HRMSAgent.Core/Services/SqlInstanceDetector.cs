using Microsoft.Win32;

namespace HRMSAgent.Core.Services;

public interface ISqlInstanceDetector
{
    IReadOnlyList<string> DetectInstances();
}

public class SqlInstanceDetector : ISqlInstanceDetector
{
    public IReadOnlyList<string> DetectInstances()
    {
        var instances = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (OperatingSystem.IsWindows())
            CollectFromRegistry(instances);

        if (instances.Count == 0)
        {
            instances.Add("localhost");
            instances.Add(@".\SQLEXPRESS");
            instances.Add(@"localhost\SQLEXPRESS");
        }

        return instances.OrderBy(i => i, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void CollectFromRegistry(HashSet<string> instances)
    {
        try
        {
            using var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Microsoft SQL Server\Instance Names\SQL");

            if (key is null)
                return;

            foreach (var instanceName in key.GetValueNames())
            {
                if (string.IsNullOrWhiteSpace(instanceName))
                    continue;

                var server = instanceName.Equals("MSSQLSERVER", StringComparison.OrdinalIgnoreCase)
                    ? Environment.MachineName
                    : $"{Environment.MachineName}\\{instanceName}";

                instances.Add(server);
                instances.Add($"localhost\\{instanceName}");
                instances.Add($@".\{instanceName}");
            }
        }
        catch
        {
            // Registry access may fail without permissions.
        }
    }
}
