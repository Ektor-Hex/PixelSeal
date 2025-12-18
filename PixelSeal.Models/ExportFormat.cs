namespace PixelSeal.Models;

/// <summary>
/// Export format for the final redacted image.
/// </summary>
public enum ExportFormat
{
    /// <summary>
    /// PNG format - lossless, no EXIF.
    /// </summary>
    PNG,

    /// <summary>
    /// JPEG format - lossy compression, no EXIF.
    /// </summary>
    JPG
}
