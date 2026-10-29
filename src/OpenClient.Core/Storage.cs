using System.Text.Json;

namespace OpenClient.Core;

public static class AppListStore
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static AppList Load(string path) =>
        File.Exists(path)
            ? JsonSerializer.Deserialize<AppList>(File.ReadAllText(path), Opt) ?? new AppList()
            : new AppList();

    public static void Save(string path, AppList list)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(list, Opt));
    }
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static Settings Load() =>
        File.Exists(AppPaths.SettingsFile)
            ? JsonSerializer.Deserialize<Settings>(File.ReadAllText(AppPaths.SettingsFile), Opt) ?? new Settings()
            : new Settings();

    public static void Save(Settings s) =>
        File.WriteAllText(AppPaths.SettingsFile, JsonSerializer.Serialize(s, Opt));

    public static string InstallRoot(Settings s) => s.InstallRoot ?? AppPaths.DefaultInstallRoot;
}