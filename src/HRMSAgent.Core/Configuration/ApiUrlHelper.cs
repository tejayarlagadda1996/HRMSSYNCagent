namespace HRMSAgent.Core.Configuration;

public static class ApiUrlHelper
{
    /// <summary>
    /// Returns the configured sync endpoint URL as entered by the user (trimmed).
    /// </summary>
    public static string ResolveSyncEndpoint(string apiUrl)
    {
        var trimmed = apiUrl.Trim().TrimEnd('/');
        if (string.IsNullOrWhiteSpace(trimmed))
            return string.Empty;

        return trimmed;
    }

    public static bool TryValidateSyncEndpoint(string apiUrl, out string? error)
    {
        error = null;
        var endpoint = ResolveSyncEndpoint(apiUrl);

        if (string.IsNullOrWhiteSpace(endpoint))
        {
            error = "API sync URL is required.";
            return false;
        }

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            error = "Enter a full URL including the endpoint path (e.g. https://api.example.com/api/attendance/sync).";
            return false;
        }

        var path = uri.AbsolutePath.Trim('/');
        if (string.IsNullOrEmpty(path))
        {
            error = "URL must include the sync endpoint path, not just the server host.";
            return false;
        }

        return true;
    }
}
