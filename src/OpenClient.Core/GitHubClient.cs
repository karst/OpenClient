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
        var root = json.RootElement;
        var tag = root.GetProperty("tag_name").GetString() ?? "";
        var page = root.GetProperty("html_url").GetString() ?? "";

        var assets = new List<ReleaseAsset>();
        if (root.TryGetProperty("assets", out var arr))
            foreach (var a in arr.EnumerateArray())
                assets.Add(new ReleaseAsset(
                    a.GetProperty("name").GetString() ?? "",
                    a.GetProperty("browser_download_url").GetString() ?? "",
                    a.GetProperty("size").GetInt64()));

        return new ReleaseInfo(tag, page, assets);
    }

    // Downloads a file to disk, reporting progress from 0.0 to 1.0.
    public async Task DownloadAsync(string url, string destPath,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Accept.Clear();                                   // asset downloads want a binary Accept
        request.Headers.Accept.ParseAdd("application/octet-stream");

        using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;

        await using var src = await response.Content.ReadAsStreamAsync(ct);
        await using var dst = File.Create(destPath);
        var buffer = new byte[81920];
        long read = 0;
        int n;
        while ((n = await src.ReadAsync(buffer, ct)) > 0)
        {
            await dst.WriteAsync(buffer.AsMemory(0, n), ct);
            read += n;
            if (total > 0) progress?.Report((double)read / total.Value);
        }
    }
}
