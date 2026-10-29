using System.Text.Json;
using OpenClient.Core;

// Find apps.json next to the program (or use a path given on the command line).
var path = args.Length > 0 ? args[0] : Path.Combine(AppContext.BaseDirectory, "apps.json");

// Read the file and turn the JSON text into C# objects.
var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, WriteIndented = true };
var list = JsonSerializer.Deserialize<AppList>(File.ReadAllText(path), options) ?? new AppList();

var github = new GitHubClient();

foreach (var app in list.Apps)
{
    try
    {
        var latest = await github.GetLatestReleaseAsync(app.Repo);
        var status = UpdateChecker.Compare(app.InstalledVersion, latest);
        Console.WriteLine($"{app.Repo}\n  installed: {app.InstalledVersion ?? "-"}  latest: {latest?.Tag ?? "-"}  => {status}");
    }
    catch (Exception e)   // one broken repo link shouldn't stop the others
    {
        Console.WriteLine($"{app.Repo}\n  error: {e.Message}");
    }
}
