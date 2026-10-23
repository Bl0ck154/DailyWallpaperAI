using System.Windows;
using DailyWallpaperAI.Core;

namespace DailyWallpaperAI;

public partial class App : System.Windows.Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        try
        {
            var store = new SettingsStore();
            var settings = store.Load();
            if (e.Args.Contains("--scheduled", StringComparer.OrdinalIgnoreCase))
            {
                // The scheduler runs independently of the UI. No window or tray is created.
                try
                {
                    if (!settings.ScheduleEnabled) return;
                    var result = await new GenerationService(store).RunAsync(settings, CancellationToken.None);
                    AppLog.Write(result.Applied ? "Scheduled run applied wallpaper." : "Scheduled run saved wallpaper.");
                }
                catch (Exception ex)
                {
                    AppLog.Write("Scheduled generation failed: " + SafeErrors.Describe(ex));
                }
                finally { Shutdown(); }
                return;
            }
            var window = new MainWindow(store, settings);
            MainWindow = window;
            ShutdownMode = ShutdownMode.OnMainWindowClose;
            window.Show();
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show("Startup failed: " + SafeErrors.Describe(ex),
                "Daily Wallpaper AI", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
        }
    }
}
