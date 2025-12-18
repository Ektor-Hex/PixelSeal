using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Infrastructure;

/// <summary>
/// Service for exporting redacted images securely.
/// Ensures complete flattening, no alpha channel, no EXIF, and full re-encoding.
/// </summary>
public sealed class SecureImageExporter
{
    /// <summary>
    /// JPEG quality setting (0-100).
    /// </summary>
    public int JpegQuality { get; set; } = 95;

    /// <summary>
    /// PNG compression level (0-9, where 9 is maximum compression).
    /// </summary>
    public int PngCompressionLevel { get; set; } = 6;

    /// <summary>
    /// Exports the bitmap to a file with complete security measures.
    /// - Creates a new flattened image
    /// - Removes alpha channel (fully opaque)
    /// - No EXIF or metadata
    /// - Complete re-encoding
    /// </summary>
    /// <param name="bitmap">The bitmap to export.</param>
    /// <param name="outputPath">The output file path.</param>
    /// <param name="format">The export format.</param>
    public void Export(SKBitmap bitmap, string outputPath, ExportFormat format)
    {
        if (bitmap == null)
            throw new ArgumentNullException(nameof(bitmap));

        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentNullException(nameof(outputPath));

        // Step 1: Create a completely new bitmap with no alpha channel
        // This is the "flatten" operation - all pixels become fully opaque
        var flattenedBitmap = CreateFlattenedBitmap(bitmap);

        try
        {
            // Step 2: Encode to the specified format (complete re-encode, no metadata)
            using var image = SKImage.FromBitmap(flattenedBitmap);
            using var data = EncodeImage(image, format);

            if (data == null)
                throw new InvalidOperationException("Failed to encode image.");

            // Step 3: Write to file
            using var outputStream = File.Create(outputPath);
            data.SaveTo(outputStream);
        }
        finally
        {
            flattenedBitmap.Dispose();
        }
    }

    /// <summary>
    /// Creates a flattened bitmap with no alpha channel.
    /// All pixels are composited against a white background.
    /// </summary>
    private static SKBitmap CreateFlattenedBitmap(SKBitmap source)
    {
        // Create new bitmap with opaque alpha type
        var flattened = new SKBitmap(source.Width, source.Height, SKColorType.Rgb888x, SKAlphaType.Opaque);

        using var canvas = new SKCanvas(flattened);
        
        // Fill with white background first
        canvas.Clear(SKColors.White);
        
        // Draw the source image on top
        canvas.DrawBitmap(source, 0, 0);
        canvas.Flush();

        return flattened;
    }

    /// <summary>
    /// Encodes the image to the specified format.
    /// </summary>
    private SKData EncodeImage(SKImage image, ExportFormat format)
    {
        return format switch
        {
            ExportFormat.PNG => image.Encode(SKEncodedImageFormat.Png, 100),
            ExportFormat.JPG => image.Encode(SKEncodedImageFormat.Jpeg, JpegQuality),
            _ => throw new ArgumentException($"Unsupported format: {format}", nameof(format))
        };
    }

    /// <summary>
    /// Gets the file extension for the specified format.
    /// </summary>
    public static string GetFileExtension(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.PNG => ".png",
            ExportFormat.JPG => ".jpg",
            _ => ".png"
        };
    }

    /// <summary>
    /// Gets the file filter for save dialogs.
    /// </summary>
    public static string GetFileFilter(ExportFormat format)
    {
        return format switch
        {
            ExportFormat.PNG => "PNG Image|*.png",
            ExportFormat.JPG => "JPEG Image|*.jpg;*.jpeg",
            _ => "PNG Image|*.png"
        };
    }
}
