using System.Diagnostics;
using System.ComponentModel;
using System.Text;

namespace DailyWallpaperAI.Core;

/// <summary>
/// No token parsing: invoke the separately installed, explicitly chosen codex-imagegen
/// executable. It alone manages Codex sign-in and image service compatibility.
/// Based on WallTrek's experimental subprocess bridge, with no WinUI dependencies.
/// </summary>
public sealed class CodexHelper
{
    public static string Resolve(string configured)
    {
        if (!string.IsNullOrWhiteSpace(configured))
        {
            var path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(configured));
            if (!File.Exists(path) || !path.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                throw new CodexFailure("Choose a valid codex-imagegen.exe path in Settings.");
            return path;
        }
        var dirs = (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim().Trim('"'))
            .Append(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "bin"));
        foreach (var dir in dirs)
        {
            foreach (var name in new[] { "codex-imagegen.exe", "codex-imagegen-cli.exe" })
            {
                try
                {
                    var candidate = Path.Combine(dir, name);
                    if (File.Exists(candidate)) return Path.GetFullPath(candidate);
                }
                catch (ArgumentException) { }
            }
        }
        throw new CodexFailure("codex-imagegen not found. Install its uv tool or select its .exe in Settings.");
    }

    public async Task<string> TestAsync(string configured, CancellationToken ct)
    {
        var exe = Resolve(configured);
        var res = await InvokeAsync(exe, ["--version"], Paths.Data, TimeSpan.FromSeconds(15), ct);
        if (res.ExitCode != 0) throw Classify(res.Diagnostic);
        // This tests the executable, not authentication or image-generation entitlement.
        return "Image helper found. Sign-in and image access are checked by a real generation.";
    }

    public async Task GenerateAsync(string configured, string prompt, IReadOnlyList<string> photoFiles,
        string destination, string size, string quality, CancellationToken ct)
    {
        if (photoFiles.Count > 5) throw new InvalidDataException("No more than five reference photos.");
        if (size is not ("1536x1024" or "1024x1024" or "1024x1536" or "auto"))
            throw new InvalidDataException("Unsupported image size.");
        if (quality is not ("auto" or "low" or "medium" or "high"))
            throw new InvalidDataException("Unsupported quality.");
        if (string.IsNullOrWhiteSpace(prompt) || prompt.Length > 16000)
            throw new InvalidDataException("Prompt is empty or too long.");
        var executable = Resolve(configured);
        var promptFile = Path.Combine(Path.GetDirectoryName(destination)!, "prompt.txt");
        await File.WriteAllTextAsync(promptFile, prompt, new UTF8Encoding(false), ct);
        var args = new List<string>
        {
            photoFiles.Count > 0 ? "edit" : "generate",
            "--prompt-file", promptFile,
            "--out", destination,
            "--output-format", "png",
            "--backend", "native",
            "--size", size,
            "--quality", quality,
            "--size-policy", "warn"
        };
        foreach (var photo in photoFiles)
        {
            args.Add("--image");
            args.Add(photo);
        }
        var run = await InvokeAsync(executable, args, Path.GetDirectoryName(destination)!, TimeSpan.FromMinutes(6), ct);
        if (run.ExitCode != 0) throw Classify(run.Diagnostic);
        if (!File.Exists(destination)) throw new CodexFailure("Image helper exited without creating an image.");
    }

    private static CodexFailure Classify(string diagnostic)
    {
        var value = diagnostic.ToLowerInvariant();
        if (value.Contains("429") || value.Contains("rate limit") || value.Contains("quota"))
            return new CodexFailure("Image generation is rate-limited. Try later; quotas may be consumed.");
        if (value.Contains("401") || value.Contains("not logged in") || value.Contains("token expired"))
            return new CodexFailure("Codex is not signed in or its session has expired.");
        if (value.Contains("403") || value.Contains("not eligible") || value.Contains("unsupported"))
            return new CodexFailure("This account may not be eligible for image generation, or the endpoint changed.");
        return new CodexFailure("The image helper failed. Check its installation and account eligibility.");
    }

    private sealed record ProcessOutcome(int ExitCode, string Diagnostic);

    private static async Task<ProcessOutcome> InvokeAsync(string executable, IReadOnlyList<string> args,
        string workingDirectory, TimeSpan timeout, CancellationToken ct)
    {
        Directory.CreateDirectory(workingDirectory);
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = workingDirectory
        };
        foreach (var arg in args) start.ArgumentList.Add(arg);
        using var process = new Process { StartInfo = start };
        try { if (!process.Start()) throw new CodexFailure("Unable to launch codex-imagegen.exe."); }
        catch (Win32Exception) { throw new CodexFailure("Unable to launch codex-imagegen.exe."); }
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeoutCts.CancelAfter(timeout);
        var outTask = ReadBoundedAsync(process.StandardOutput);
        var errTask = ReadBoundedAsync(process.StandardError);
        try
        {
            await process.WaitForExitAsync(timeoutCts.Token);
            var output = await outTask;
            var error = await errTask;
            // Discard raw stdout and stderr immediately after classifying them.
            return new ProcessOutcome(process.ExitCode, output + " " + error);
        }
        catch (OperationCanceledException)
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
            catch (InvalidOperationException) { }
            try { await process.WaitForExitAsync(CancellationToken.None); } catch (InvalidOperationException) { }
            throw ct.IsCancellationRequested ? new OperationCanceledException(ct) :
                new TimeoutException("Image generation exceeded its time limit.");
        }
    }

    private static async Task<string> ReadBoundedAsync(StreamReader reader)
    {
        var buffer = new char[4096];
        var captured = new StringBuilder();
        int count;
        while ((count = await reader.ReadAsync(buffer)) > 0)
            if (captured.Length < 1024) captured.Append(buffer, 0, Math.Min(count, 1024 - captured.Length));
        return captured.ToString();
    }
}
