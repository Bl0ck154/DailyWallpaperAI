# Daily Wallpaper AI — Alpha 3 (UI stability)

## Main changes
- **Navigation fixed:** replace WPF TabPanel with a plain vertical items stack; tab selection no longer causes selected-row reordering.
- Sidebar width and row heights stay constant across tab changes. Selected/hover states keep the same padding and border thickness.
- **Button text fixed:** button templates explicitly bind their label foreground to the Button's Foreground property, overriding the global TextBlock styling that caused white-on-lime primary labels.
- Contrast pass for primary (lime with dark text), secondary (blue-gray with white text) and destructive (dark burgundy with pale pink text) controls.
- Gallery tiles keep a constant border width to prevent selection-related size changes.
- CI runs static UI layout contract checks and builds both Windows variants.

## Packages
- **Lite:** smallest download; requires [Microsoft .NET 10 Desktop Runtime x64](https://dotnet.microsoft.com/en-us/download/dotnet/10.0).
- **Portable:** includes the .NET 10 Desktop Runtime; a much larger download.

Extract every file, then launch `DailyWallpaperAI.exe`. To generate images you must separately install the unofficial `codex-imagegen-cli` helper and have file-based local Codex ChatGPT authorization. The helper uses undocumented endpoints that may change; neither package includes tokens.

## Alpha limitations
Windows CI verifies XAML/C# compilation and offline smoke tests; it does **not** constitute live visual testing of window behavior, tray/scheduling, or Codex image generation. Please test on a real Windows desktop and report issues with a screenshot.
