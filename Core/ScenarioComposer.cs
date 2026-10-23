using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace DailyWallpaperAI.Core;

public static partial class ScenarioComposer
{
    public const string IdentityInstruction = "Include the SAME person from the supplied reference photographs prominently in the scene. Preserve recognizable facial identity, face shape, eyes, natural proportions and age. Do not replace the person with a lookalike.";

    [GeneratedRegex(@"\{\{\s*([a-zA-Z][a-zA-Z0-9_]*)\s*\}\}")]
    private static partial Regex TokenRegex();

    public static ComposedPrompt Compose(WallpaperScenario scenario, bool hasIdentityPhotos)
    {
        if (string.IsNullOrWhiteSpace(scenario.Template) || scenario.Template.Length > 12000)
            throw new InvalidDataException("Scenario prompt must be between 1 and 12000 characters.");
        var places = ReadChoices(scenario.Places);
        var moods = ReadChoices(scenario.Moods);
        if (places.Length * moods.Length > 4096) throw new InvalidDataException("Too many possible variations.");
        var tokens = TokenRegex().Matches(scenario.Template).Select(x => x.Groups[1].Value).Distinct().ToArray();
        if (tokens.Any(x => x is not ("place" or "mood")))
            throw new InvalidDataException("Supported variables: {{place}} and {{mood}}.");
        if (tokens.Contains("place") && places.Length == 1 && places[0] == "" ||
            tokens.Contains("mood") && moods.Length == 1 && moods[0] == "")
            throw new InvalidDataException("Fill in the variation lists used by your prompt.");
        if (!tokens.Contains("place")) places = [""];
        if (!tokens.Contains("mood")) moods = [""];
        var combos = (from p in places from m in moods select new { Place = p, Mood = m, Key = Hash(p + "\0" + m) }).ToArray();
        var used = scenario.UsedVariations.ToHashSet(StringComparer.Ordinal);
        var options = combos.Where(x => !used.Contains(x.Key)).ToArray();
        if (options.Length == 0)
        {
            options = combos.Where(x => combos.Length == 1 || x.Key != scenario.LastVariation).ToArray();
        }
        var selected = options[RandomNumberGenerator.GetInt32(options.Length)];
        var prompt = TokenRegex().Replace(scenario.Template, x => x.Groups[1].Value switch
        {
            "place" => selected.Place,
            "mood" => selected.Mood,
            _ => throw new InvalidDataException("Unknown placeholder")
        });
        var parts = new List<string>();
        if (hasIdentityPhotos)
            parts.Add(IdentityInstruction);
        parts.Add(prompt);
        if (!string.IsNullOrWhiteSpace(scenario.Style)) parts.Add("Style: " + scenario.Style.Trim());
        if (!string.IsNullOrWhiteSpace(scenario.Avoid)) parts.Add("Avoid: " + scenario.Avoid.Trim());
        return new ComposedPrompt(string.Join("\n\n", parts), selected.Key);
    }

    public static void MarkSuccessful(WallpaperScenario scenario, string key)
    {
        if (scenario.UsedVariations.Contains(key)) scenario.UsedVariations.Clear();
        scenario.UsedVariations.Add(key);
        scenario.LastVariation = key;
        if (scenario.UsedVariations.Count > 4096) scenario.UsedVariations = [key];
    }

    private static string[] ReadChoices(string value) =>
        (value ?? "").Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Where(x => x.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase).Take(100).DefaultIfEmpty("").ToArray();

    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
}
