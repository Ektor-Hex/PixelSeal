using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine;

/// <summary>
/// Interface for redaction strategies.
/// Each implementation renders content that replaces original pixels.
/// 
/// IMPORTANT ARCHITECTURAL NOTE:
/// - Standard strategies (SolidOverwrite, GlassMorph, etc.) have NO access to original image content.
///   They operate purely on the canvas, rendering synthetic replacement content.
/// - Specialized strategies (e.g., AestheticBlurStrategy) MAY receive the source bitmap via
///   extended methods like ApplyWithSource() for pixel-based effects that require actual image data.
/// - This is an intentional architectural exception for aesthetic modes only.
/// - The engine handles this via type-checking and specialized method dispatch.
/// </summary>
public interface IRedactionStrategy
{
    /// <summary>
    /// Applies the redaction to the specified region on the canvas.
    /// The canvas already contains the image; this method OVERWRITES the region.
    /// </summary>
    /// <param name="canvas">The SkiaSharp canvas to draw on.</param>
    /// <param name="region">The rectangular region to redact (image coordinates).</param>
    /// <param name="options">Configuration options for the redaction.</param>
    void Apply(SKCanvas canvas, SKRect region, RedactionOptions options);

    /// <summary>
    /// The redaction mode this strategy implements.
    /// </summary>
    RedactionMode Mode { get; }
}
