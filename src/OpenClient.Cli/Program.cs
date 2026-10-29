using OpenClient.Core;

// apps.json: a path from the command line, otherwise the one in the data folder.
var path = args.Length > 0 ? args[0] : AppPaths.AppsFile;
var list = AppListStore.Load(path);

// First run: pick an install folder, or press Enter for the default.
var settings = SettingsStore.Load();
if (!settings.FirstRunDone)
{
    Console.Write($"Install folder [{AppPaths.DefaultInstallRoot}] (Enter = default): ");
    var input = Console.ReadLine()?.Trim().Trim('"');
    settings.InstallRoot = string.IsNullOrEmpty(input) ? null : Path.GetFullPath(input);
    settings.FirstRunDone = true;
    SettingsStore.Save(settings);
}
var installRoot = SettingsStore.InstallRoot(settings);
Console.WriteLine($"Installing to: {installRoot}\n");

var github = new GitHubClient();
var installer = new Installer(github);

foreach (var app in list.Apps)
{
    try
    {
        var latest = await github.GetLatestReleaseAsync(app.Repo);
        var status = UpdateChecker.Compare(app.InstalledVersion, latest);
        Console.WriteLine($"{app.Repo}\n  installed: {app.InstalledVersion ?? "-"}  latest: {latest?.Tag ?? "-"}  => {status}");

        // Build the list of actions available for this app.
        var actions = new List<string>();
        if (status == UpdateStatus.NotInstalled) actions.Add("[i]nstall");
        if (status == UpdateStatus.UpdateAvailable) actions.Add("[u]pdate");
        if (Launcher.CanLaunch(app)) actions.Add("[p]lay");
        if (actions.Count == 0) { Console.WriteLine(); continue; }

        Console.Write($"  {string.Join("  ", actions)}  [Enter = skip]: ");
        var choice = Console.ReadLine()?.Trim().ToLowerInvariant();

        if ((choice == "i" || choice == "u") && latest is not null && actions.Any(a => a.StartsWith($"[{choice}]")))
        {
            var progress = new Progress<double>(p => Console.Write($"\r  downloading {p:P0}   "));
            await installer.InstallAsync(app, latest, installRoot, progress);
            AppListStore.Save(path, list);
            Console.WriteLine($"\n  installed {app.InstalledVersion} -> {app.InstallPath}");
            if (app.Executable is null)
                Console.WriteLine("  (no executable found - set \"executable\" for this app in apps.json)");
        }
        else if (choice == "p" && Launcher.CanLaunch(app))
        {
            if (!app.LaunchConfirmed)
            {
                Console.Write($"  First launch of {app.Executable}. Run it? [y/N]: ");
                if (Console.ReadLine()?.Trim().ToLowerInvariant() != "y") { Console.WriteLine(); continue; }
                app.LaunchConfirmed = true;
                AppListStore.Save(path, list);
            }
            Launcher.Launch(app);
        }
        Console.WriteLine();
    }
    catch (Exception e)   // one broken repo shouldn't stop the others
    {
        Console.WriteLine($"  error: {e.Message}\n");
    }
}