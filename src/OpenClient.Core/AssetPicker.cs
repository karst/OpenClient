using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace OpenClient.Core;

public static class AssetPicker
{
    public static ReleaseAsset? Pick(IEnumerable<ReleaseAsset> assets) =>
        assets.Select(a => (Asset: a, Score: Score(a.Name.ToLowerInvariant())))
              .Where(x => x.Score > 0)
              .OrderByDescending(x => x.Score)
              .Select(x => x.Asset)
              .FirstOrDefault();

    private static int Score(string name)
    {
        bool zip = name.EndsWith(".zip");
        bool tar = name.EndsWith(".tar.gz") || name.EndsWith(".tgz");
        bool appImage = name.EndsWith(".appimage");
        if (!(zip || tar || appImage)) return 0;                          // installers etc. not supported yet

        string[] junk = { "symbols", "debug", "source", "setup", "installer" };
        if (junk.Any(name.Contains)) return 0;

        // Split into words so "darwin" isn't mistaken for "win".
        var tokens = Regex.Split(name, "[^a-z0-9]+");
        bool Has(params string[] prefixes) => tokens.Any(t => prefixes.Any(t.StartsWith));

        bool isWin = Has("win"), isLinux = Has("linux") || appImage, isMac = Has("mac", "osx", "darwin");
        bool osMatch = OperatingSystem.IsWindows() ? isWin
                     : OperatingSystem.IsMacOS() ? isMac
                     : isLinux;
        bool otherOs = (isWin || isLinux || isMac) && !osMatch;
        if (otherOs) return 0;

        int score = 1 + (osMatch ? 10 : 0);

        bool x64 = name.Contains("x64") || name.Contains("amd64") || name.Contains("x86_64");
        bool arm = name.Contains("arm64") || name.Contains("aarch64");
        bool wantArm = RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        if (wantArm ? arm : x64) score += 5;
        else if (wantArm ? x64 : arm)
        {
            if (!OperatingSystem.IsMacOS()) return 0;                     // wrong CPU (macOS can use Rosetta)
            score -= 3;
        }
        if (name.Contains("universal")) score += 3;
        return score;
    }
}