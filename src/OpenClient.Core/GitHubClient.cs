using System.Net;
using System.Text.Json;

namespace OpenClient.Core;

public class GitHubClient
{
    // HttpClient is C#'s tool for web requests. One instance is reused for every call.
    private readonly HttpClient _http = new();

    public GitHubClient()
    {
        // GitHub rejects requests that don't say who is calling.
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("OpenClient/0.1");
        _http.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    }

    // "https://github.com/OpenRCT2/OpenRCT2" -> ("OpenRCT2", "OpenRCT2")
    public static (string Owner, string Name) ParseRepoUrl(string url)
    {
        var uri = new Uri(url);                                  // throws if not a valid URL
        var parts = uri.AbsolutePath.Trim('/').Split('/');       // "/a/b/" -> ["a","b"]
        if (uri.Host != "github.com" || parts.Length < 2)
            throw new ArgumentException($"Not a GitHub repo link: {url}");
        return (parts[0], parts[1]);
    }

    // Asks GitHub for the newest release. Returns null if the repo has no releases.
    // "async/await" means: start the web request, and wait for the answer without freezing the program.
    public async Task<ReleaseInfo?> GetLatestReleaseAsync(string repoUrl)
    {
        var (owner, name) = ParseRepoUrl(repoUrl);
        var apiUrl = $"https://api.github.com/repos/{owner}/{name}/releases/latest";

        using var response = await _http.GetAsync(apiUrl);
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        response.EnsureSuccessStatusCode();                      // throw on any other error

        // Read the JSON reply and pull out the two fields we care about.
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
        var page = json.RootElement.GetProperty("html_url").GetString() ?? "";
        return new ReleaseInfo(tag, page);
    }
}
