using System.Buffers.Binary;

namespace DailyWallpaperAI.Core;

public static class ImageValidator
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void ValidatePng(string path)
    {
        var bytes = File.ReadAllBytes(path);
        if (bytes.LongLength > 24 * 1024 * 1024 || bytes.Length < 57 ||
            !bytes.AsSpan(0, 8).SequenceEqual(PngSignature))
            throw new InvalidDataException("Image generator did not produce a valid PNG.");
        var width = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadUInt32BigEndian(bytes.AsSpan(20, 4));
        if (width == 0 || height == 0 || width > 8192 || height > 8192 ||
            (long)width * height > 36_000_000)
            throw new InvalidDataException("Generated image dimensions are invalid.");
        // WPF decoder catches PNG structural corruption that header-only checks miss.
        using var input = File.OpenRead(path);
        var decoder = System.Windows.Media.Imaging.BitmapDecoder.Create(input,
            System.Windows.Media.Imaging.BitmapCreateOptions.None,
            System.Windows.Media.Imaging.BitmapCacheOption.OnLoad);
        if (decoder.Frames.Count == 0 || decoder.Frames[0].PixelWidth != width || decoder.Frames[0].PixelHeight != height)
            throw new InvalidDataException("Generated image failed decoding.");
    }
}
