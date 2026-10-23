namespace DailyWallpaperAI.Core;

public static class SafeErrors
{
    public static string Describe(Exception e)
    {
        // Never log CLI output, account files, prompt bodies, or authentication values.
        if (e is OperationCanceledException) return "Cancelled.";
        if (e is TimeoutException) return "Image generation timed out. It may have consumed account quota.";
        if (e is FileNotFoundException) return "Required program or input file was not found.";
        if (e is InvalidDataException || e is ArgumentException) return e.Message;
        if (e is UnauthorizedAccessException) return "Permission denied for files or system integration.";
        if (e is CodexFailure f) return f.Message;
        return $"{e.GetType().Name}: operation failed; see local setup.";
    }
}

public sealed class CodexFailure(string message) : Exception(message);
