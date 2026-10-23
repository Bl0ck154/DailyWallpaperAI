# Daily Wallpaper AI — Alpha

First test release of a standalone Windows 10/11 x64 wallpaper generator built with .NET 10 / WPF.

## Which package?
- **DailyWallpaperAI-Lite-win-x64.zip** — small application bundle; requires [Microsoft .NET 10 Desktop Runtime (Windows x64)](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- **DailyWallpaperAI-Portable-win-x64.zip** — larger self-contained bundle; no separate .NET Desktop Runtime required.

Extract **all files** from your chosen ZIP into a writable folder (for example `%USERPROFILE%\Apps\DailyWallpaperAI`), then launch `DailyWallpaperAI.exe`. These are portable folders, not installers. Do not run directly from the ZIP.

## Features
- Simple WPF window with Home, Scenarios, Gallery and Settings
- One-off prompts and reusable scenarios with rotating location/mood combinations
- Optional 1–5 reference photos (explicit upload consent is required)
- Manual generation, cancellation, local image archive, restore/delete from Gallery
- Optional static Windows wallpaper update, tray and a daily task via Windows Task Scheduler

## Required for generation — BOTH packages
The third-party [codex-imagegen-cli](https://github.com/jdmnk/codex-imagegen-cli) helper must be installed separately and accessible in the same Windows user's PATH (or chosen in Settings). It requires a supported local **file-based Codex/ChatGPT login**; it uses undocumented endpoints, has usage limits and is not an official OpenAI image API. No Codex session files, auth tokens, photos or other credentials are included in this release.

## Important alpha limitations
CI validates compilation and offline scenario tests **only**. Live image generation, GUI, tray behaviour and Windows Task Scheduler have **not** yet been exercised on the owner's PC. Daily scheduling does not wake a sleeping/off computer; missed-run catch-up and cross-process consistency need more testing. Image generation capability is not guaranteed by a successful Codex login.

Please test with a disposable prompt and reference image first. If generation fails, the app should keep the previous desktop wallpaper. See [release validation issue #1](https://github.com/Bl0ck154/DailyWallpaperAI/issues/1), [scheduler issue #2](https://github.com/Bl0ck154/DailyWallpaperAI/issues/2), [privacy issue #3](https://github.com/Bl0ck154/DailyWallpaperAI/issues/3). This is an **alpha prerelease, not a stable release**.
