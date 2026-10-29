using System.Formats.Tar;
using System.IO.Compression;

namespace OpenClient.Core;

public class Installer
{
    private readonly GitHubClient _github;
    public Installer(GitHubClient github) => _github = github;

    // Installs or updates. Fills in InstalledVersion, InstallPath and Executable on the entry.
    public async Task InstallAsync(AppEntry app, ReleaseInfo release, string installRoot,
        IProgress<double>? progress = null, CancellationToken ct = default)
    {
        var asset = AssetPicker.Pick(release.Assets)
            ?? throw new InvalidOperationException(
                $"No download for this OS/CPU in {release.Tag}. See {release.Url}");

        var (_, repoName) = GitHubClient.ParseRepoUrl(app.Repo);
        var target = app.InstallPath ?? Path.Combine(installRoot, repoName); // updates stay where they are
        Directory.CreateDirectory(Path.GetDirectoryName(target)!);

        var temp = Path.Combine(Path.GetTempPath(), "OpenClient-" + Guid.NewGuid().ToString("N"));
        var staging = target + ".new-" + Guid.NewGuid().ToString("N")[..8];
        Directory.CreateDirectory(temp);

        try
        {
            var file = Path.Combine(temp, asset.Name);
            await _github.DownloadAsync(asset.DownloadUrl, file, progress, ct);

            Directory.CreateDirectory(staging);
            Extract(file, staging);

            var source = ContentRoot(staging);
            if (!Directory.Exists(target)) Directory.Move(source, target);   // fresh install
            else CopyOver(source, target);                                    // update: keep user files
        }
        finally
        {
            TryDelete(temp);
            TryDelete(staging);
        }

        app.InstallPath = target;
        app.InstalledVersion = release.Tag;
        if (app.Executable is null || !File.Exists(Path.Combine(target, app.Executable))
                                   && !Directory.Exists(Path.Combine(target, app.Executable)))
            app.Executable = Launcher.FindExecutable(target, repoName);
    }

    private static void Extract(string file, string dest)
    {
        var lower = file.ToLowerInvariant();
        if (lower.EndsWith(".zip"))
            ZipFile.ExtractToDirectory(file, dest, overwriteFiles: true);
        else if (lower.EndsWith(".tar.gz") || lower.EndsWith(".tgz"))
        {
            using var gz = new GZipStream(File.OpenRead(file), CompressionMode.Decompress);
            TarFile.ExtractToDirectory(gz, dest, overwriteFiles: true);
        }
        else if (lower.EndsWith(".appimage"))
            File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), overwrite: true);
        else
            throw new NotSupportedException($"Unsupported file type: {Path.GetFileName(file)}");
    }

    // Many releases wrap everything in one top-level folder; step into it.
    private static string ContentRoot(string dir)
    {
        var entries = Directory.GetFileSystemEntries(dir);
        return entries.Length == 1 && Directory.Exists(entries[0])
               && !entries[0].EndsWith(".app", StringComparison.OrdinalIgnoreCase)
            ? entries[0] : dir;
    }

    private static void CopyOver(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var f in Directory.GetFiles(src))
            File.Copy(f, Path.Combine(dst, Path.GetFileName(f)), overwrite: true);
        foreach (var d in Directory.GetDirectories(src))
            CopyOver(d, Path.Combine(dst, Path.GetFileName(d)));
    }

    private static void TryDelete(string dir)
    {
        try { if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true); } catch { /* best effort */ }
    }
}