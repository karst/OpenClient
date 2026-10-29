namespace OpenClient.Core;

// One entry in apps.json: the repo link you typed in, plus which version
// we have installed (null = not installed yet). Like a plain C++ struct.
public class AppEntry
{
    public string Repo { get; set; } = "";
    public string? InstalledVersion { get; set; }
}

// The whole apps.json file: just a list of entries.
public class AppList
{
    public List<AppEntry> Apps { get; set; } = new();
}

// What we need to know about a GitHub release: its tag ("v1.2.3") and web page.
public record ReleaseInfo(string Tag, string Url);
