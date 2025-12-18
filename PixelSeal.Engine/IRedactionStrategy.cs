using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine;

/// <summary>
/// Interface for redaction strategies.
/// Each implementation renders synthetic content that completely replaces original pixels.
/// Strategies have NO access to original image content.
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
