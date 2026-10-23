#if NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit { }
}

namespace DailyWallpaperAI.Core
{
    internal static class ClassicCompat
    {
        public static string BuildCommandLine(IEnumerable<string> values)
        {
            var parts = new List<string>();
            foreach (var value in values)
            {
                if (value.IndexOfAny(new[] { ' ', '\t', '\n', '"' }) == -1 && value.Length > 0)
                {
                    parts.Add(value);
                    continue;
                }
                var b = new StringBuilder("\"");
                int backslashes = 0;
                foreach (var ch in value)
                {
                    if (ch == '\\') { backslashes++; continue; }
                    if (ch == '"')
                    {
                        b.Append('\\', backslashes * 2 + 1);
                        b.Append('"');
                        backslashes = 0;
                        continue;
                    }
                    b.Append('\\', backslashes);
                    backslashes = 0;
                    b.Append(ch);
                }
                b.Append('\\', backslashes * 2);
                b.Append('"');
                parts.Add(b.ToString());
            }
            return string.Join(" ", parts);
        }

        public static async Task WaitForExitAsync(Process process, CancellationToken token)
        {
            while (!process.HasExited)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(120, token);
            }
            process.WaitForExit();
        }

        public static int RandomInt(int maxExclusive)
        {
            if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
            using (var rng = RandomNumberGenerator.Create())
            {
                uint bound = (uint)maxExclusive;
                ulong limit = ((ulong)uint.MaxValue + 1) - (((ulong)uint.MaxValue + 1) % bound);
                var buffer = new byte[4];
                while (true)
                {
                    rng.GetBytes(buffer);
                    uint value = BitConverter.ToUInt32(buffer, 0);
                    if (value < limit) return (int)(value % bound);
                }
            }
        }
    }
}
#else
namespace DailyWallpaperAI.Core
{
    internal static class ClassicCompat
    {
        public static int RandomInt(int maxExclusive) =>
            System.Security.Cryptography.RandomNumberGenerator.GetInt32(maxExclusive);
    }
}
#endif
