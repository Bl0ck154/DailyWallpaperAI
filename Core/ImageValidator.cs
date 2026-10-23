namespace DailyWallpaperAI.Core;

public static class ImageValidator
{
    private static readonly byte[] PngSignature = [137, 80, 78, 71, 13, 10, 26, 10];

    public static void ValidatePng(string path)
    {
        var size = new FileInfo(path).Length;
        if (size is < 57 or > 24 * 1024 * 1024)
            throw new InvalidDataException("Image generator returned an invalid or oversized PNG.");
        var bytes = File.ReadAllBytes(path);
        if (bytes.Length < 57 ||
            !bytes.Take(8).SequenceEqual(PngSignature))
            throw new InvalidDataException("Image generator did not produce a valid PNG.");
        uint width = ReadBigEndian(bytes, 16);
        uint height = ReadBigEndian(bytes, 20);
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
    private static uint ReadBigEndian(byte[] bytes, int offset) =>
        ((uint)bytes[offset] << 24) | ((uint)bytes[offset + 1] << 16) |
        ((uint)bytes[offset + 2] << 8) | bytes[offset + 3];
}
