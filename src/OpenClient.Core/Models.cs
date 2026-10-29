namespace OpenClient.Core;

public class AppEntry
{
    public string Repo { get; set; } = "";
    public string? InstalledVersion { get; set; }
    public string? InstallPath { get; set; }      // folder the app was unpacked into
    public string? Executable { get; set; }       // relative to InstallPath (auto-detected, editable)
    public bool LaunchConfirmed { get; set; }     // user agreed to run this app
}

public class AppList
{
    public List<AppEntry> Apps { get; set; } = new();
}

public class Settings
{
    public string? InstallRoot { get; set; }      // null = use default
    public bool FirstRunDone { get; set; }
}

public record ReleaseAsset(string Name, string DownloadUrl, long Size);
public record ReleaseInfo(string Tag, string Url, IReadOnlyList<ReleaseAsset> Assets);