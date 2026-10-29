namespace OpenClient.Core;

public static class AppPaths
{
    public static string DataDir
    {
        get
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            var dir = OperatingSystem.IsWindows()
                ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OpenClient")
                : OperatingSystem.IsMacOS()
                    ? Path.Combine(home, "Library", "Application Support", "OpenClient")
                    : Path.Combine(Environment.GetEnvironmentVariable("XDG_DATA_HOME")
                                   ?? Path.Combine(home, ".local", "share"), "OpenClient");
            Directory.CreateDirectory(dir);
            return dir;
        }
    }

    public static string AppsFile => Path.Combine(DataDir, "apps.json");
    public static string SettingsFile => Path.Combine(DataDir, "settings.json");
    public static string DefaultInstallRoot => Path.Combine(DataDir, "apps");
}