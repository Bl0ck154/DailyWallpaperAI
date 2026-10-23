using System.Text.Json.Serialization;

namespace DailyWallpaperAI.Core;

public sealed class WallpaperSettings
{
    public int SchemaVersion { get; set; } = 1;
    public string HelperPath { get; set; } = "";
    public string Quality { get; set; } = "high";
    public string Size { get; set; } = "1536x1024";
    public bool ApplyAutomatically { get; set; } = true;
    public bool AllowPhotoUploads { get; set; }
    public bool CloseToTray { get; set; }
    public bool ScheduleEnabled { get; set; }
    public string ScheduleTime { get; set; } = "09:00";
    public Guid ActiveScenarioId { get; set; }
    public List<WallpaperScenario> Scenarios { get; set; } = [];
    public List<PhotoReference> References { get; set; } = [];
    public string? LastWallpaper { get; set; }

    public static WallpaperSettings CreateDefault()
    {
        var scenario = new WallpaperScenario
        {
            Name = "World explorer",
            Template = "A cinematic, photorealistic wide-angle wallpaper of {{place}} during {{mood}}. Beautiful composition with a clear focal point.",
            Places = "Kyoto, Japan\nDolomites, Italy\nIcelandic mountains\nLisbon, Portugal\nNorwegian fjords",
            Moods = "golden hour\nsoft morning mist\nafter rainfall\nstarry night",
            Style = "Natural details, elegant color grading, landscape photography.",
            Avoid = "Text, logos, watermarks, split screens, frames."
        };
        return new WallpaperSettings { Scenarios = [scenario], ActiveScenarioId = scenario.Id };
    }
}

public sealed class WallpaperScenario
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New scenario";
    public string Template { get; set; } = "";
    public string Places { get; set; } = "";
    public string Moods { get; set; } = "";
    public string Style { get; set; } = "Photorealistic landscape wallpaper";
    public string Avoid { get; set; } = "No text or watermark";
    public List<string> UsedVariations { get; set; } = [];
    public string? LastVariation { get; set; }
}

public sealed class PhotoReference
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Label { get; set; } = "";
    public string FileName { get; set; } = "";
    public bool Enabled { get; set; } = true;
}

public sealed record ComposedPrompt(string Prompt, string VariationKey);
public sealed record GenerationResult(string SavedPath, bool Applied);
