namespace OpenClient.Core;

public enum UpdateStatus { NotInstalled, UpToDate, UpdateAvailable, NoReleases }

public static class UpdateChecker
{
    // Compares what's installed with what GitHub says is newest.
    public static UpdateStatus Compare(string? installed, ReleaseInfo? latest)
    {
        if (latest is null) return UpdateStatus.NoReleases;
        if (string.IsNullOrEmpty(installed)) return UpdateStatus.NotInstalled;

        // Tags look like "v1.2.3". Strip the "v" and try to read them as numbers
        // so that 1.10.0 is correctly newer than 1.9.0 (plain text comparison gets this wrong).
        if (Version.TryParse(installed.TrimStart('v'), out var a) &&
            Version.TryParse(latest.Tag.TrimStart('v'), out var b))
            return a >= b ? UpdateStatus.UpToDate : UpdateStatus.UpdateAvailable;

        // Fallback for odd tag names (dates, words): any difference counts as an update.
        return installed == latest.Tag ? UpdateStatus.UpToDate : UpdateStatus.UpdateAvailable;
    }
}
