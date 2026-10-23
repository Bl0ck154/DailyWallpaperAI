namespace DailyWallpaperAI.Core;

public sealed class GenerationService(SettingsStore store)
{
    public async Task<GenerationResult> RunAsync(WallpaperSettings settings, CancellationToken ct)
    {
        using var singleFlight = new Semaphore(1, 1, @"Local\DailyWallpaperAI.ImageJob");
        var owned = singleFlight.WaitOne(0);
        if (!owned) throw new CodexFailure("Another wallpaper generation is already running.");
        try { return await ExecuteCoreAsync(settings, ct); }
        finally { singleFlight.Release(); }
    }

    private async Task<GenerationResult> ExecuteCoreAsync(WallpaperSettings settings, CancellationToken ct)
    {
        var scenario = settings.Scenarios.FirstOrDefault(x => x.Id == settings.ActiveScenarioId)
            ?? throw new InvalidDataException("Select an active scenario first.");
        var references = settings.References.Where(x => x.Enabled).ToArray();
        if (references.Length > 0 && !settings.AllowPhotoUploads)
            throw new CodexFailure("Approve sending the selected reference photos to ChatGPT before generating.");
        var prepared = ScenarioComposer.Compose(scenario, references.Length > 0);
        var photoPaths = references.Select(PhotoStore.GetPath).ToArray();
        Directory.CreateDirectory(Paths.Wallpapers);
        var temporary = Path.Combine(Paths.Data, "jobs", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            var rawImage = Path.Combine(temporary, "result.png");
            await new CodexHelper().GenerateAsync(settings.HelperPath, prepared.Prompt, photoPaths,
                rawImage, settings.Size, settings.Quality, ct);
            ImageValidator.ValidatePng(rawImage);
            ct.ThrowIfCancellationRequested();
            var file = $"wallpaper-{DateTimeOffset.Now:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.png";
            var finalPath = Path.Combine(Paths.Wallpapers, file);
            File.Move(rawImage, finalPath);
            // Persist valid output and consume one variation even if wallpaper apply fails.
            ScenarioComposer.MarkSuccessful(scenario, prepared.VariationKey);
            settings.LastWallpaper = finalPath;
            store.Save(settings);
            var applied = false;
            if (settings.ApplyAutomatically)
            {
                WallpaperService.Apply(finalPath);
                applied = true;
            }
            AppLog.Write(applied ? "Image generated and set as wallpaper." : "Image generated and saved.");
            return new GenerationResult(finalPath, applied);
        }
        finally
        {
            try { Directory.Delete(temporary, recursive: true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }
}
