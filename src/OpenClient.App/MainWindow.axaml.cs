using System.Text.Json;
using Avalonia.Controls;
using OpenClient.Core;

namespace OpenClient.App;

// One tile in the grid.
public record AppRow(string Name, string Versions, string Status);

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Opened += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        StatusText.Text = "Checking for updates...";

        var path = Path.Combine(AppContext.BaseDirectory, "apps.json");
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var list = JsonSerializer.Deserialize<AppList>(File.ReadAllText(path), options) ?? new AppList();

        var github = new GitHubClient();
        var rows = new List<AppRow>();

        foreach (var app in list.Apps)
        {
            var name = app.Repo.TrimEnd('/').Split('/').Last();
            try
            {
                var latest = await github.GetLatestReleaseAsync(app.Repo);
                var status = UpdateChecker.Compare(app.InstalledVersion, latest);
                rows.Add(new AppRow(name,
                    $"installed: {app.InstalledVersion ?? "-"}  latest: {latest?.Tag ?? "-"}",
                    status.ToString()));
            }
            catch (Exception e)
            {
                rows.Add(new AppRow(name, "", $"error: {e.Message}"));
            }
        }

        AppGrid.ItemsSource = rows;
        StatusText.Text = $"{rows.Count} apps";
    }
}