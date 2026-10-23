using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace DailyWallpaperAI.Core;

public static class WallpaperService
{
    private const uint SpiSetDeskWallpaper = 0x0014;
    private const uint SpifUpdateIniFile = 0x01;
    private const uint SpifSendWinIniChange = 0x02;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SystemParametersInfo(uint action, uint param, string path, uint flags);

    public static void Apply(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Wallpaper missing.", path);
        using var key = Registry.CurrentUser.OpenSubKey(@"Control Panel\Desktop", writable: true)
            ?? throw new UnauthorizedAccessException("Desktop settings unavailable.");
        key.SetValue("WallpaperStyle", "10"); // Fill, preserve original image file.
        key.SetValue("TileWallpaper", "0");
        if (!SystemParametersInfo(SpiSetDeskWallpaper, 0, Path.GetFullPath(path),
                SpifUpdateIniFile | SpifSendWinIniChange))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Windows refused to set wallpaper.");
    }
}
