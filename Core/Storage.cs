using System.Text.Json;

namespace DailyWallpaperAI.Core;

public static class Paths
{
    public static string Data => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DailyWallpaperAI");
    public static string Settings => Path.Combine(Data, "settings.json");
    public static string References => Path.Combine(Data, "references");
    public static string Wallpapers => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "DailyWallpaperAI");
}

public sealed class SettingsStore
{
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true, PropertyNameCaseInsensitive = true };
    private readonly object _sync = new();

    public WallpaperSettings Load()
    {
        if (!File.Exists(Paths.Settings)) return WallpaperSettings.CreateDefault();
        try
        {
            var settings = JsonSerializer.Deserialize<WallpaperSettings>(File.ReadAllText(Paths.Settings), Options)
                ?? throw new InvalidDataException("Empty settings document");
            if (settings.SchemaVersion != 1 || settings.Scenarios is null || settings.References is null)
                throw new InvalidDataException("Unsupported settings format");
            return settings;
        }
        catch (JsonException)
        {
            throw new InvalidDataException($"Settings are corrupted. Backup or repair: {Paths.Settings}");
        }
    }

    public void Save(WallpaperSettings settings)
    {
        lock (_sync)
        {
            Directory.CreateDirectory(Paths.Data);
            var temp = Paths.Settings + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                File.WriteAllText(temp, JsonSerializer.Serialize(settings, Options));
                // Atomic replacement when target exists: retain an automatic .bak.
                if (File.Exists(Paths.Settings)) File.Replace(temp, Paths.Settings, Paths.Settings + ".bak");
                else File.Move(temp, Paths.Settings);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
        }
    }
}

public static class PhotoStore
{
    public static PhotoReference Import(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Photo not found", path);
        var ext = Path.GetExtension(path).ToLowerInvariant();
        if (ext is not (".png" or ".jpg" or ".jpeg" or ".webp"))
            throw new InvalidDataException("Use PNG, JPG, or WebP photos.");
        var info = new FileInfo(path);
        if (info.Length == 0 || info.Length > 20 * 1024 * 1024)
            throw new InvalidDataException("A reference photo must be at most 20 MiB.");
        Directory.CreateDirectory(Paths.References);
        var id = Guid.NewGuid();
        var name = id.ToString("N") + ext;
        File.Copy(path, Path.Combine(Paths.References, name), overwrite: false);
        return new PhotoReference { Id = id, Label = Path.GetFileName(path), FileName = name };
    }

    public static string GetPath(PhotoReference reference)
    {
        var file = reference.FileName;
        if (file != Path.GetFileName(file) || file.Contains("..", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(file))
            throw new InvalidDataException("Invalid reference path.");
        var full = Path.Combine(Paths.References, file);
        if (!File.Exists(full)) throw new FileNotFoundException("Reference image is missing.");
        return full;
    }
}

public static class AppLog
{
    public static void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Paths.Data);
            var log = Path.Combine(Paths.Data, "status.log");
            if (File.Exists(log) && new FileInfo(log).Length > 1_000_000)
                File.Move(log, log + ".old", overwrite: true);
            File.AppendAllText(log, $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
