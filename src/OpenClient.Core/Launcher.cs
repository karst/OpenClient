using System.Diagnostics;

namespace OpenClient.Core;

public static class Launcher
{
    // Full path of the executable, or null if the app can't be launched (this decides if Play shows).
    public static string? ResolveExe(AppEntry app)
    {
        if (app.InstallPath is null || app.Executable is null) return null;
        var path = Path.Combine(app.InstallPath, app.Executable);
        return File.Exists(path) || Directory.Exists(path) ? path : null;
    }

    public static bool CanLaunch(AppEntry app) => ResolveExe(app) is not null;

    public static void Launch(AppEntry app)
    {
        var exe = ResolveExe(app) ?? throw new InvalidOperationException("App is not installed.");

        if (OperatingSystem.IsMacOS() && exe.EndsWith(".app", StringComparison.OrdinalIgnoreCase))
        {
            var open = new ProcessStartInfo("open") { UseShellExecute = false };
            open.ArgumentList.Add(exe);
            Process.Start(open);
            return;
        }

        if (!OperatingSystem.IsWindows())   // archives don't always keep the executable bit
            File.SetUnixFileMode(exe, File.GetUnixFileMode(exe) |
                UnixFileMode.UserExecute | UnixFileMode.GroupExecute | UnixFileMode.OtherExecute);

        Process.Start(new ProcessStartInfo(exe)
        {
            WorkingDirectory = Path.GetDirectoryName(exe)!,
            UseShellExecute = false
        });
    }

    // Guesses the main executable. Best guess wins: name matches repo, closer to the top folder.
    public static string? FindExecutable(string dir, string repoName)
    {
        if (OperatingSystem.IsMacOS())
        {
            var bundle = Directory.EnumerateDirectories(dir, "*.app", SearchOption.AllDirectories).FirstOrDefault();
            if (bundle is not null) return Path.GetRelativePath(dir, bundle);
        }

        return Directory.EnumerateFiles(dir, "*", SearchOption.AllDirectories)
            .Select(f => (File: f, Score: Score(f, dir, repoName)))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => Path.GetRelativePath(dir, x.File))
            .FirstOrDefault();
    }

    private static int Score(string file, string root, string repoName)
    {
        var name = Path.GetFileName(file).ToLowerInvariant();
        string[] skip = { "unins", "crash", "setup", "redist", "updater" };
        if (skip.Any(name.Contains)) return 0;

        bool isExe = OperatingSystem.IsWindows()
            ? name.EndsWith(".exe")
            : name.EndsWith(".appimage") ||
              (!name.EndsWith(".so") && !name.Contains(".so.") &&
               (File.GetUnixFileMode(file) & UnixFileMode.UserExecute) != 0);
        if (!isExe) return 0;

        int depth = Path.GetRelativePath(root, file).Count(c => c == Path.DirectorySeparatorChar);
        return 20 - depth + (name.Contains(repoName.ToLowerInvariant()) ? 10 : 0);
    }
}