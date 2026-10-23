# Daily Wallpaper AI — compact Windows app (initial implementation)

A small, local Windows WPF app that creates AI wallpapers using a **separately
installed, locally signed-in** `codex-imagegen-cli` helper. **No API key is
stored in this app**, and it does not read Codex auth files or tokens.

This source project is independent of WallTrek/WinUI. It targets .NET 10/WPF
and uses no external NuGet packages. It is **not a validated Windows release**:
Windows compilation and live ChatGPT image generation cannot be verified in
this environment. GitHub Actions configuration is included for build checks.

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
- Portable self-contained publish target. Close-to-tray is optional.
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
