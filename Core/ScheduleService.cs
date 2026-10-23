using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace DailyWallpaperAI.Core;

public static class ScheduleService
{
    private const string TaskName = "DailyWallpaperAI";

    public static async Task ApplyAsync(bool enabled, string time)
    {
        // Interactive user token: wallpaper changes must happen in the signed-in session.
        if (enabled)
        {
            if (!DateTime.TryParseExact(time, "HH:mm", CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out _))
                throw new InvalidDataException("Daily time must be HH:mm (24-hour clock).");
            var executable = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName
                ?? throw new InvalidOperationException("Cannot locate the application executable.");
            if (!Path.GetFileName(executable).Equals("DailyWallpaperAI.exe", StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Publish the app before enabling the schedule.");
            await RunAsync("/Create", "/F", "/SC", "DAILY", "/ST", time,
                "/TN", TaskName, "/TR", $"\"{executable}\" --scheduled", "/RL", "LIMITED", "/IT");
        }
        else
        {
            // Missing task is fine when auto generation has never been enabled.
            try { await RunAsync("/Delete", "/F", "/TN", TaskName); }
            catch (CodexFailure) { }
        }
    }

    private static async Task RunAsync(params string[] args)
    {
        var executable = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "schtasks.exe");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardError = true, RedirectStandardOutput = true
        };
#if NETFRAMEWORK
        start.Arguments = ClassicCompat.BuildCommandLine(args);
#else
        foreach (var arg in args) start.ArgumentList.Add(arg);
#endif
        using var process = new Process { StartInfo = start };
        try { process.Start(); }
        catch (Win32Exception) { throw new CodexFailure("Windows Task Scheduler is unavailable."); }
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        var outTask = process.StandardOutput.ReadToEndAsync();
        var errTask = process.StandardError.ReadToEndAsync();
        try
        {
#if NETFRAMEWORK
            await ClassicCompat.WaitForExitAsync(process, cts.Token);
#else
            await process.WaitForExitAsync(cts.Token);
#endif
        }
        catch (OperationCanceledException)
        {
            try
            {
#if NETFRAMEWORK
                process.Kill();
#else
                process.Kill(entireProcessTree: true);
#endif
            }
            catch (InvalidOperationException) { }
            throw new CodexFailure("Task Scheduler did not respond in time.");
        }
        await Task.WhenAll(outTask, errTask);
        if (process.ExitCode != 0)
            throw new CodexFailure("Unable to configure the scheduled task. Check Windows Task Scheduler permissions.");
    }
}
