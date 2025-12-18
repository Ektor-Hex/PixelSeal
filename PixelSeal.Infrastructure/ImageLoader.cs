using SkiaSharp;

namespace PixelSeal.Infrastructure;

/// <summary>
/// Service for loading images from disk.
/// Strips all metadata on load for security.
/// </summary>
public sealed class ImageLoader
{
    /// <summary>
    /// Loads an image from the specified path.
    /// Returns a clean bitmap without EXIF or other metadata.
    /// </summary>
    /// <param name="filePath">Path to the image file.</param>
    /// <returns>A clean SKBitmap.</returns>
    public SKBitmap LoadImage(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentNullException(nameof(filePath));

        if (!File.Exists(filePath))
            throw new FileNotFoundException("Image file not found.", filePath);

        // Load the image data
        using var stream = File.OpenRead(filePath);
        using var originalBitmap = SKBitmap.Decode(stream);

        if (originalBitmap == null)
            throw new InvalidOperationException("Failed to decode image file.");

        // Create a clean copy without any metadata
        // This ensures no EXIF, ICC profiles, or other metadata is retained
        var cleanBitmap = new SKBitmap(originalBitmap.Width, originalBitmap.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
        
        using var canvas = new SKCanvas(cleanBitmap);
        canvas.Clear(SKColors.White);
        canvas.DrawBitmap(originalBitmap, 0, 0);
        canvas.Flush();

        return cleanBitmap;
    }

    /// <summary>
    /// Gets basic image information without fully loading the image.
    /// </summary>
    public (int Width, int Height) GetImageDimensions(string filePath)
    {
        using var stream = File.OpenRead(filePath);
        using var codec = SKCodec.Create(stream);
        
        if (codec == null)
            throw new InvalidOperationException("Failed to read image dimensions.");

        return (codec.Info.Width, codec.Info.Height);
    }

    /// <summary>
    /// Validates that the file is a supported image format.
    /// </summary>
    public static bool IsSupportedFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension is ".png" or ".jpg" or ".jpeg" or ".bmp" or ".webp";
    }
}
