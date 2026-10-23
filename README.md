# Daily Wallpaper AI — compact Windows app (initial implementation)

A small, local Windows WPF app that creates AI wallpapers using a **separately
installed, locally signed-in** `codex-imagegen-cli` helper. **No API key is
stored in this app**, and it does not read Codex auth files or tokens.

This source project is independent of WallTrek/WinUI. It targets .NET 10/WPF
and uses no external NuGet packages. The Windows CI builds both Lite and Portable packages and runs offline scenario tests.
**Live GUI/Codex generation has not been validated yet**; published versions are
marked Alpha until real Windows acceptance tests pass.

## Features implemented

- A four-tab WPF UI: Home, Scenarios, Gallery, Settings.
- Scenario prompt templates with `{{place}}` and `{{mood}}` variation pools;
  rotation uses every unique combination before repeating.
- Optional 1–5 local identity-reference photos for Codex image editing;
  explicit consent checkbox is required. Adding new photos revokes prior consent.
- Manual generation, cancellation, helper path selector and helper install probe.
- Validate PNG output and archive it under `Pictures/DailyWallpaperAI`.
- Optional Windows background update through `SystemParametersInfo`.
- Optional daily automation via Windows Task Scheduler (`--scheduled`).
- Separate Lite (framework-dependent) and Portable (self-contained) publish targets.
- Close-to-tray is optional.
- No SQLite, server, analytics, DeviantArt, video, or cloud secrets vault.

## Requirements

- Windows 10/11 x64.
- For a framework-dependent developer build: .NET 10 Desktop Runtime.
  The self-contained publish includes .NET runtime libraries.
- Codex CLI signed into your ChatGPT account, plus **separately installed**
  [`codex-imagegen-cli`](https://github.com/jdmnk/codex-imagegen-cli).
  It is an unofficial alpha package that requires Python/uv and **file-based
  Codex authentication**. Its underlying ChatGPT endpoints are undocumented,
  availability is account-dependent, and usage limits apply.

## Download an alpha Windows build

Use the [GitHub Releases page](https://github.com/Bl0ck154/DailyWallpaperAI/releases).
The two builds have the **same features**:

- **Lite** (`DailyWallpaperAI-Lite-win-x64.zip`): smaller download; install the
  [Microsoft .NET 10 Desktop Runtime for Windows x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) first.
- **Portable** (`DailyWallpaperAI-Portable-win-x64.zip`): larger download, includes
  the .NET runtime; no separate .NET installation required.

Extract the entire ZIP to a writable directory such as
`%USERPROFILE%\\Apps\\DailyWallpaperAI` and run `DailyWallpaperAI.exe`.
Neither edition includes Codex or the separately installed image helper; follow
**Image helper setup** below. Do not launch directly inside the ZIP.

## Build and run on Windows

```powershell
# Run from the DailyWallpaperAI directory with .NET SDK 10 installed
 dotnet run --project .\DailyWallpaperAI.csproj

# Test only scenario logic without auth/network
 dotnet run --project .\Tests\ScenarioSmokeTests.csproj

# Portable Windows folder with bundled runtime
 dotnet publish .\DailyWallpaperAI.csproj -c Release -r win-x64 --self-contained true -p:PublishTrimmed=false -o .\dist\DailyWallpaperAI
```

Run `dist\DailyWallpaperAI\DailyWallpaperAI.exe`. No administrator access
should be required for the normal user-level workflow. Do not move the folder
without saving the schedule again, as the scheduled task references the EXE path.

### Image helper setup

Follow the helper project's official README to review and install from its
repository, for example by cloning and using `uv tool install -e .` in the
reviewed checkout. Then test `codex-imagegen --version` from PowerShell.
On first launch open **Settings** → **Check installed helper**, then try one
manual non-personal generation. An installed helper check **does not** verify
ChatGPT account image generation eligibility.

For personal photos, choose 1–5 reference images and explicitly check the
upload-consent box. Imported local files are stored separately from originals.
`codex-imagegen-cli` converts edit inputs to WebP before upload according to
its current documentation; validate that behavior if privacy is sensitive.

## Automatic generation

In Settings, enter a 24-hour local time such as `09:00`, enable the daily task,
then select **Save settings and schedule**. The app calls `schtasks.exe` to
register a task named `DailyWallpaperAI`, which runs the same executable with
`--scheduled` while you are signed in.

Important limitations:
- Windows sign-in is required. The computer is **not woken** from sleep/off.
- Exact catch-up after a missed scheduled time is **not yet implemented/tested**.
- Task Scheduler registration and Windows GUI integration need real-PC smoke tests.
- Scheduled work is single-flight with manual work in the same session.
- Account credentials are handled only by the external helper, not this app.

## Storage and security

- Settings: `%LOCALAPPDATA%\DailyWallpaperAI\settings.json` (+ `.bak`).
- Reference images: `%LOCALAPPDATA%\DailyWallpaperAI\references\`.
- Generated images: `%USERPROFILE%\Pictures\DailyWallpaperAI\`.
- Diagnostics: `%LOCALAPPDATA%\DailyWallpaperAI\status.log` (no raw
  prompt, token, helper output, or personally identifying file names).
- No fallback to another provider and no hidden network uploads.
- If generation fails, the old wallpaper stays unchanged.
- As of this first implementation: no per-photo disable switch, multiple
  display selection, 16:9 post-cropping, delayed catch-up, or real Windows
  integration evidence. Those are follow-up items, not implied working features.

## Origin and license

Design and subprocess flow informed by WallTrek (MIT); see
`LICENSE-WALLTREK-NOTICE.txt`. This project does **not** ship third-party helper
source or authentication data.

## Manual one-off prompts and gallery operations

The Home page accepts an optional one-off prompt. A nonempty one-off prompt bypasses
scenario rotation for that one run and does not consume a scenario variation. When
reference photos are enabled, the same identity-preservation instruction and upload
consent apply. A successful image is saved in the Gallery; an older image may be
selected and deleted after confirmation. The most recently generated wallpaper is
protected from accidental deletion.

**Project status:** Windows CI verifies compilation, offline scenario behavior and both
packaging flavours. The GitHub Release is a prerelease, not a production-ready
version. Neither compilation nor mocked tests establish live Codex generation;
that requires the separately installed helper and a signed-in Windows user.

## Making a GitHub alpha release

The [Windows workflow](.github/workflows/windows-build.yml) runs on pushes to
`main` and uploads both packages as temporary Actions artifacts. To publish
versioned, durable downloads, run it manually via **Actions → Windows WPF build
and alpha release → Run workflow**, choose `main`, enter a new
`release_tag` such as `v0.1.0-alpha.1`, and submit. A release is created **only
if** scenario tests, WPF build and both Windows packages succeed. Tags must be
unique. Notes come from [the alpha release notes](docs/ALPHA_RELEASE_NOTES.md).

