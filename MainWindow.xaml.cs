using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DailyWallpaperAI.Core;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using ComboBox = System.Windows.Controls.ComboBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DailyWallpaperAI;

public partial class MainWindow : Window
{
    private readonly SettingsStore _store;
    private readonly WallpaperSettings _settings;
    private readonly Forms.NotifyIcon _tray;
    private WallpaperScenario? _editing;
    private CancellationTokenSource? _generation;
    private bool _loading;
    private bool _exiting;

    public MainWindow(SettingsStore store, WallpaperSettings settings)
    {
        _store = store;
        _settings = settings;
        InitializeComponent();
        _loading = true;
        ScenarioPicker.ItemsSource = _settings.Scenarios;
        ScenarioPicker.SelectedItem = _settings.Scenarios.FirstOrDefault(x => x.Id == _settings.ActiveScenarioId);
        _editing = ScenarioPicker.SelectedItem as WallpaperScenario;
        LoadScene(_editing);
        HelperPathBox.Text = _settings.HelperPath;
        SelectCombo(SizePicker, _settings.Size);
        SelectCombo(QualityPicker, _settings.Quality);
        ApplyCheck.IsChecked = _settings.ApplyAutomatically;
        ConsentCheck.IsChecked = _settings.AllowPhotoUploads;
        ScheduleCheck.IsChecked = _settings.ScheduleEnabled;
        ScheduleTimeBox.Text = _settings.ScheduleTime;
        TrayCheck.IsChecked = _settings.CloseToTray;
        PhotosList.ItemsSource = _settings.References;
        _loading = false;
        ActiveScenarioLabel.Text = "Scenario: " + (_editing?.Name ?? "none");
        ShowPreview(_settings.LastWallpaper);
        RefreshGallery();
        _tray = new Forms.NotifyIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            Text = "Daily Wallpaper AI",
            Visible = true
        };
        var context = new Forms.ContextMenuStrip();
        context.Items.Add("Open", null, (_, _) => Dispatcher.Invoke(ShowFromTray));
        context.Items.Add("Generate now", null, (_, _) => Dispatcher.Invoke(() => _ = GenerateAsync()));
        context.Items.Add("Quit", null, (_, _) => Dispatcher.Invoke(ExitFromTray));
        _tray.ContextMenuStrip = context;
        _tray.DoubleClick += (_, _) => Dispatcher.Invoke(ShowFromTray);
    }

    private static void SelectCombo(ComboBox combo, string value)
    {
        combo.SelectedItem = combo.Items.Cast<ComboBoxItem>().FirstOrDefault(x => (string?)x.Content == value)
            ?? combo.Items[0];
    }

    private static string ComboValue(ComboBox combo) => (string?)((ComboBoxItem?)combo.SelectedItem)?.Content ?? "auto";

    private void LoadScene(WallpaperScenario? scene)
    {
        ScenarioName.Text = scene?.Name ?? "";
        ScenarioPrompt.Text = scene?.Template ?? "";
        PlacesBox.Text = scene?.Places ?? "";
        MoodsBox.Text = scene?.Moods ?? "";
        ScenarioStyle.Text = scene?.Style ?? "";
        ScenarioAvoid.Text = scene?.Avoid ?? "";
    }

    private void CaptureScene()
    {
        if (_editing is null) return;
        _editing.Name = ScenarioName.Text.Trim();
        _editing.Template = ScenarioPrompt.Text;
        _editing.Places = PlacesBox.Text;
        _editing.Moods = MoodsBox.Text;
        _editing.Style = ScenarioStyle.Text;
        _editing.Avoid = ScenarioAvoid.Text;
    }

    private void CapturePreferences(bool includeSchedule = false)
    {
        _settings.HelperPath = HelperPathBox.Text.Trim();
        _settings.Size = ComboValue(SizePicker);
        _settings.Quality = ComboValue(QualityPicker);
        _settings.ApplyAutomatically = ApplyCheck.IsChecked == true;
        _settings.AllowPhotoUploads = ConsentCheck.IsChecked == true;
        _settings.CloseToTray = TrayCheck.IsChecked == true;
        if (includeSchedule)
        {
            _settings.ScheduleEnabled = ScheduleCheck.IsChecked == true;
            _settings.ScheduleTime = ScheduleTimeBox.Text.Trim();
        }
    }

    private void ScenarioPicker_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || ScenarioPicker.SelectedItem is not WallpaperScenario scene) return;
        CaptureScene();
        _editing = scene;
        _settings.ActiveScenarioId = scene.Id;
        LoadScene(scene);
        ActiveScenarioLabel.Text = "Scenario: " + scene.Name;
        _store.Save(_settings);
    }

    private void AddScenario_Click(object sender, RoutedEventArgs e)
    {
        CaptureScene();
        var scenario = new WallpaperScenario { Name = "New scenario", Template = "A beautiful {{place}} during {{mood}}.", Places = "Alps\nForest", Moods = "sunrise\nmoonlight" };
        _settings.Scenarios.Add(scenario);
        ScenarioPicker.Items.Refresh();
        ScenarioPicker.SelectedItem = scenario;
    }

    private void DeleteScenario_Click(object sender, RoutedEventArgs e)
    {
        if (_settings.Scenarios.Count <= 1) { Status("Keep at least one scenario."); return; }
        if (_editing is null) return;
        _settings.Scenarios.Remove(_editing);
        _editing = null;
        ScenarioPicker.Items.Refresh();
        ScenarioPicker.SelectedItem = _settings.Scenarios[0];
        _store.Save(_settings);
    }

    private void SaveScenario_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            CaptureScene();
            if (_editing is null) return;
            _ = ScenarioComposer.Compose(_editing, false); // Input validation; no image request.
            _store.Save(_settings);
            ScenarioPicker.Items.Refresh();
            ActiveScenarioLabel.Text = "Scenario: " + _editing.Name;
            Status("Scenario saved.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private async void Generate_Click(object sender, RoutedEventArgs e) => await GenerateAsync();

    private async Task GenerateAsync()
    {
        if (_generation is not null) { Status("Generation is already running."); return; }
        _generation = new CancellationTokenSource();
        GenerateButton.IsEnabled = false;
        CancelButton.IsEnabled = true;
        try
        {
            CaptureScene();
            CapturePreferences();
            _store.Save(_settings);
            Status("Generating wallpaper via Codex…");
            var oneOffPrompt = OneOffPromptBox.Text;
            var result = await new GenerationService(_store).RunAsync(_settings, _generation.Token, oneOffPrompt);
            ShowPreview(result.SavedPath);
            RefreshGallery();
            Status(result.Applied ? "Generated and applied to desktop." : "Generated and saved to gallery.");
        }
        catch (Exception ex) { Error(ex); }
        finally
        {
            _generation.Dispose(); _generation = null;
            GenerateButton.IsEnabled = true;
            CancelButton.IsEnabled = false;
        }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e) => _generation?.Cancel();

    private void ShowPreview(string? path)
    {
        LastWallpaperLabel.Text = path is not null ? Path.GetFileName(path) : "";
        if (path is null || !File.Exists(path))
        {
            PreviewImage.Source = null;
            EmptyPreview.Visibility = Visibility.Visible;
            return;
        }
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(path, UriKind.Absolute);
            bitmap.EndInit(); bitmap.Freeze();
            PreviewImage.Source = bitmap;
            EmptyPreview.Visibility = Visibility.Collapsed;
        }
        catch (Exception) { PreviewImage.Source = null; EmptyPreview.Visibility = Visibility.Visible; }
    }

    private sealed record GalleryEntry(string Name, string Path);

    private void RefreshGallery()
    {
        Directory.CreateDirectory(Paths.Wallpapers);
        var previous = (GalleryList.SelectedItem as GalleryEntry)?.Path;
        var items = Directory.EnumerateFiles(Paths.Wallpapers, "*.png")
            .OrderByDescending(Path.GetFileName).Take(150)
            .Select(x => new GalleryEntry(Path.GetFileName(x), x)).ToList();
        GalleryList.ItemsSource = items;
        GalleryCountText.Text = items.Count == 0
            ? "No wallpapers yet. Generate one from the Overview tab."
            : items.Count == 1 ? "1 wallpaper saved locally." : $"{items.Count} wallpapers saved locally.";
        GalleryList.SelectedItem = items.FirstOrDefault(x =>
                string.Equals(x.Path, previous, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault(x =>
                string.Equals(x.Path, _settings.LastWallpaper, StringComparison.OrdinalIgnoreCase))
            ?? items.FirstOrDefault();
        if (items.Count == 0) GalleryPreviewImage.Source = null;
    }

    private void GalleryList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GalleryPreviewImage is null) return;
        if (GalleryList.SelectedItem is not GalleryEntry entry)
        {
            GalleryPreviewImage.Source = null;
            return;
        }
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(entry.Path, UriKind.Absolute);
            bitmap.EndInit();
            bitmap.Freeze();
            GalleryPreviewImage.Source = bitmap;
        }
        catch (Exception)
        {
            GalleryPreviewImage.Source = null;
            Status("Could not preview this image.");
        }
    }
    private void RefreshGallery_Click(object sender, RoutedEventArgs e) => RefreshGallery();

    private void ApplySelected_Click(object sender, RoutedEventArgs e)
    {
        if (GalleryList.SelectedItem is not GalleryEntry entry) { Status("Choose a wallpaper first."); return; }
        try
        {
            ImageValidator.ValidatePng(entry.Path);
            WallpaperService.Apply(entry.Path);
            _settings.LastWallpaper = entry.Path;
            _store.Save(_settings);
            Status("Wallpaper applied.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void DeleteSelected_Click(object sender, RoutedEventArgs e)
    {
        if (GalleryList.SelectedItem is not GalleryEntry entry) { Status("Choose a wallpaper to delete."); return; }
        if (string.Equals(Path.GetFullPath(entry.Path), _settings.LastWallpaper, StringComparison.OrdinalIgnoreCase))
        {
            Status("Cannot delete the last generated wallpaper. Apply a different wallpaper first.");
            return;
        }
        var confirmation = System.Windows.MessageBox.Show(this,
            $"Permanently delete {entry.Name}?", "Delete wallpaper",
            MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirmation != MessageBoxResult.Yes) return;
        try
        {
            var full = Path.GetFullPath(entry.Path);
            var root = Path.GetFullPath(Paths.Wallpapers).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("Refusing to delete a file outside the wallpaper gallery.");
            File.Delete(full);
            RefreshGallery();
            ShowPreview(_settings.LastWallpaper);
            Status("Wallpaper deleted.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(Paths.Wallpapers);
        Process.Start(new ProcessStartInfo("explorer.exe") { UseShellExecute = true, ArgumentList = { Paths.Wallpapers } });
    }

    private void BrowseHelper_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Filter = "Windows executables (*.exe)|*.exe", Title = "Select codex-imagegen.exe" };
        if (picker.ShowDialog(this) == true) HelperPathBox.Text = picker.FileName;
    }

    private async void TestHelper_Click(object sender, RoutedEventArgs e)
    {
        HelperStatus.Text = "Checking…";
        try { HelperStatus.Text = await new CodexHelper().TestAsync(HelperPathBox.Text.Trim(), CancellationToken.None); }
        catch (Exception ex) { HelperStatus.Text = SafeErrors.Describe(ex); }
    }

    private void AddPhotos_Click(object sender, RoutedEventArgs e)
    {
        var picker = new OpenFileDialog { Multiselect = true, Filter = "Images|*.png;*.jpg;*.jpeg;*.webp", Title = "Choose reference photos" };
        if (picker.ShowDialog(this) != true) return;
        try
        {
            if (_settings.References.Count + picker.FileNames.Length > 5)
                throw new InvalidDataException("You can keep at most 5 reference photos.");
            foreach (var path in picker.FileNames) _settings.References.Add(PhotoStore.Import(path));
            // A prior approval must never automatically cover newly imported photos.
            _settings.AllowPhotoUploads = false;
            ConsentCheck.IsChecked = false;
            PhotosList.Items.Refresh();
            _store.Save(_settings);
            Status("Photos imported. Approve uploading them before generation.");
        }
        catch (Exception ex) { Error(ex); }
    }

    private void RemovePhoto_Click(object sender, RoutedEventArgs e)
    {
        if (PhotosList.SelectedItem is not PhotoReference reference) return;
        _settings.References.Remove(reference);
        PhotosList.Items.Refresh();
        _store.Save(_settings);
        try { File.Delete(PhotoStore.GetPath(reference)); } catch (IOException) { }
        Status("Reference removed.");
    }

    private async void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var previousEnabled = _settings.ScheduleEnabled;
        var previousTime = _settings.ScheduleTime;
        try
        {
            CaptureScene();
            CapturePreferences(includeSchedule: true);
            // Task registration must succeed before we commit the new schedule settings.
            await ScheduleService.ApplyAsync(_settings.ScheduleEnabled, _settings.ScheduleTime);
            _store.Save(_settings);
            Status("Settings saved. Daily task " + (_settings.ScheduleEnabled ? "enabled." : "disabled."));
        }
        catch (Exception ex)
        {
            _settings.ScheduleEnabled = previousEnabled;
            _settings.ScheduleTime = previousTime;
            Error(ex);
        }
    }

    private void Tabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source == Tabs && Tabs.SelectedIndex == 2 && GalleryList is not null)
            RefreshGallery();
    }

    private void Status(string message) => StatusText.Text = message;
    private void Error(Exception ex) => Status("Error: " + SafeErrors.Describe(ex));

    private void ShowFromTray()
    {
        Show(); WindowState = WindowState.Normal; Activate();
    }

    private void ExitFromTray()
    {
        _exiting = true;
        _generation?.Cancel();
        Close();
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_exiting && _settings.CloseToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        _generation?.Cancel();
        _tray.Visible = false;
        _tray.Dispose();
        base.OnClosing(e);
    }
}
