using DailyWallpaperAI.Core;

static void Expect(bool condition, string message)
{
    if (!condition) throw new Exception(message);
}

var state = WallpaperSettings.CreateDefault();
var scenario = state.Scenarios.Single();
var used = new HashSet<string>();
for (var index = 0; index < 20; ++index)
{
    var chosen = ScenarioComposer.Compose(scenario, false);
    Expect(!used.Contains(chosen.VariationKey), $"Variation repeated before cycle completed ({index}).");
    Expect(!chosen.Prompt.Contains("{{"), "Unresolved variable in prompt.");
    used.Add(chosen.VariationKey);
    ScenarioComposer.MarkSuccessful(scenario, chosen.VariationKey);
}
Expect(used.Count == 20, "Expected 20 distinct combinations.");
var next = ScenarioComposer.Compose(scenario, true);
Expect(next.Prompt.Contains("SAME person"), "Missing identity instruction.");
Expect(next.VariationKey != scenario.LastVariation, "Immediate repeat on new cycle.");
var stateWithBadVariable = new WallpaperScenario { Template = "{{nonsense}}" };
try
{
    ScenarioComposer.Compose(stateWithBadVariable, false);
    throw new Exception("Unknown variable was accepted.");
}
catch (InvalidDataException) { }
Console.WriteLine("Scenario smoke tests passed.");

// The compose/commit cycle must never consume a variation during a preview or a failed render.
var replay = ScenarioComposer.Compose(scenario, false);
var repeat = ScenarioComposer.Compose(scenario, false);
Expect(!string.IsNullOrWhiteSpace(replay.Prompt) && replay.Prompt.Contains("Style:"), "Scenario style missing.");
Expect(replay.VariationKey.Length == 64, "Variation hash must be SHA-256.");
Expect(ScenarioComposer.IdentityInstruction.Contains("SAME person"), "Manual reference prompt must preserve identity.");
Console.WriteLine("Additional prompt composition checks passed.");
