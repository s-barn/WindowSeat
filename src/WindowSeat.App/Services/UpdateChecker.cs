using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace WindowSeat.App.Services;

/// <summary>
/// Checks the GitHub Releases API once per session for a newer version of WindowSeat.
/// No auto-download — only notifies the user if a newer tag is available.
/// </summary>
public sealed class UpdateChecker
{
    private const string ReleasesApiUrl =
        "https://api.github.com/repos/stmba/WindowSeat/releases/latest";

    private static readonly Version CurrentVersion =
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(1, 0, 0);

    public record UpdateInfo(string TagName, string HtmlUrl);

    /// <summary>
    /// Returns update info if a newer release exists, or null if up to date / unreachable.
    /// </summary>
    public static async Task<UpdateInfo?> CheckAsync(CancellationToken ct = default)
    {
        try
        {
            using var client = new HttpClient();
            client.DefaultRequestHeaders.UserAgent.ParseAdd("WindowSeat-UpdateCheck");
            client.Timeout = TimeSpan.FromSeconds(10);

            var response = await client.GetAsync(ReleasesApiUrl, ct);
            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("tag_name", out var tagEl)) return null;
            if (!root.TryGetProperty("html_url", out var urlEl)) return null;

            var tag     = tagEl.GetString()?.TrimStart('v') ?? string.Empty;
            var htmlUrl = urlEl.GetString() ?? string.Empty;

            if (!Version.TryParse(tag, out var releaseVersion)) return null;
            if (releaseVersion <= CurrentVersion)               return null;

            return new UpdateInfo(tagEl.GetString()!, htmlUrl);
        }
        catch
        {
            // Network unavailable, rate-limited, etc — fail silently
            return null;
        }
    }
}
