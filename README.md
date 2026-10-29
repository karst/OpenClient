# OpenClient

A cross-platform launcher for open-source projects hosted on GitHub. It keeps them up to date
and lets each project customise extra UI through a `metadata.json`, in a Steam Big Picture-style interface.

## Goals
- Add apps by pasting a GitHub repo link (stored in `apps.json`).
- Show update status per app, with Update / Launch buttons.
- Let projects ship a `metadata.json` that unlocks custom screens (e.g. a mod launcher)
  built from fixed UI blocks (list, button, text, image, toggle). Data only, no scripts.
- Windows, Linux and macOS. C# with Avalonia.

## Layout
- `src/OpenClient.Core` - logic only (GitHub API, version comparison). No UI code.
- `src/OpenClient.Cli`  - console test tool for the Core logic.
- `samples/apps.json`   - example app list.

## Run the CLI
    dotnet run --project src/OpenClient.Cli -- samples/apps.json

## Runtime data (not in the repo)
- Windows: %APPDATA%/OpenClient/
- Linux:   ~/.local/share/OpenClient/
- macOS:   ~/Library/Application Support/OpenClient/
Inside: `apps.json` and `apps/<name>/` (unpacked releases + their metadata.json).

## Roadmap
1. [x] Update checking (GitHub latest release vs installed version)
2. [ ] Install: pick the right release asset per OS, download, unpack
3. [ ] Launch: run the executable named in metadata.json (confirm before first run)
4. [ ] Avalonia UI: tile grid, controller/keyboard focus
5. [ ] metadata.json schema + data-driven screens (docs/metadata-schema.md)
6. [ ] Tests for version comparison

## Open design question
Is a "mod launcher" a separate game that OpenClient manages mods/profiles for,
or a mod-aware app downloaded from GitHub? This decides the metadata.json UI blocks.
