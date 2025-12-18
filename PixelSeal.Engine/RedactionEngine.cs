using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine;

/// <summary>
/// Core redaction engine that applies redaction strategies to images.
/// This engine has NO knowledge of WPF, mouse events, or UI concerns.
/// It only processes images and regions.
/// </summary>
public sealed class RedactionEngine
{
    /// <summary>
    /// Applies all redaction regions to the image.
    /// Creates a new bitmap with all redactions applied.
    /// The original image is not modified.
    /// </summary>
    /// <param name="sourceImage">The source image bitmap.</param>
    /// <param name="regions">The regions to redact.</param>
    /// <returns>A new bitmap with all redactions applied.</returns>
    public SKBitmap ApplyRedactions(SKBitmap sourceImage, IEnumerable<RedactionRegion> regions)
    {
        if (sourceImage == null)
            throw new ArgumentNullException(nameof(sourceImage));

        // Create a new bitmap to avoid modifying the original
        var resultBitmap = new SKBitmap(sourceImage.Width, sourceImage.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);

        using var canvas = new SKCanvas(resultBitmap);

        // Draw the original image first
        canvas.DrawBitmap(sourceImage, 0, 0);

        // Apply each redaction
        foreach (var region in regions)
        {
            ApplyRegion(canvas, region);
        }

        canvas.Flush();
        return resultBitmap;
    }

    /// <summary>
    /// Applies a single redaction region to the canvas.
    /// </summary>
    private void ApplyRegion(SKCanvas canvas, RedactionRegion region)
    {
        // Normalize region bounds
        float x = region.X;
        float y = region.Y;
        float width = region.Width;
        float height = region.Height;

        if (width < 0)
        {
            x += width;
            width = -width;
        }
        if (height < 0)
        {
            y += height;
            height = -height;
        }

        var skRect = SKRect.Create(x, y, width, height);

        // Get the appropriate strategy
        var strategy = RedactionStrategyFactory.GetStrategy(region.Mode);

        // Apply based on region shape
        switch (region.Shape)
        {
            case RegionShape.Ellipse:
                ApplyEllipseRegion(canvas, skRect, strategy, region.Options);
                break;
            case RegionShape.FreeForm:
                ApplyFreeFormRegion(canvas, region, strategy);
                break;
            case RegionShape.Rectangle:
            default:
                strategy.Apply(canvas, skRect, region.Options);
                break;
        }
    }

    /// <summary>
    /// Applies redaction to an elliptical region.
    /// </summary>
    private void ApplyEllipseRegion(SKCanvas canvas, SKRect bounds, IRedactionStrategy strategy, RedactionOptions options)
    {
        // Save canvas state and clip to ellipse
        canvas.Save();
        
        using var clipPath = new SKPath();
        clipPath.AddOval(bounds);
        canvas.ClipPath(clipPath);
        
        // Apply the strategy within the clipped area
        strategy.Apply(canvas, bounds, options);
        
        canvas.Restore();
    }

    /// <summary>
    /// Applies redaction to a free-form brush stroke region.
    /// </summary>
    private void ApplyFreeFormRegion(SKCanvas canvas, RedactionRegion region, IRedactionStrategy strategy)
    {
        if (region.PathPoints.Count < 2)
            return;

        // Create a path from the brush stroke points
        using var strokePath = new SKPath();
        strokePath.MoveTo(region.PathPoints[0].X, region.PathPoints[0].Y);
        
        for (int i = 1; i < region.PathPoints.Count; i++)
        {
            strokePath.LineTo(region.PathPoints[i].X, region.PathPoints[i].Y);
        }

        // Create a widened path (stroke to fill)
        using var strokePaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = region.BrushSize,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round,
            IsAntialias = true
        };

        // Get the fill path from the stroke
        using var fillPath = strokePaint.GetFillPath(strokePath);
        
        if (fillPath == null) return;

        // Get bounds of the path
        var bounds = fillPath.Bounds;
        
        // Save and clip to the path
        canvas.Save();
        canvas.ClipPath(fillPath);
        
        // Apply the strategy within the clipped brush stroke area
        strategy.Apply(canvas, bounds, region.Options);
        
        canvas.Restore();
    }

    /// <summary>
    /// Validates that a region is within image bounds.
    /// </summary>
    public static bool IsRegionValid(RedactionRegion region, int imageWidth, int imageHeight)
    {
        if (region.Width == 0 || region.Height == 0)
            return false;

        float left = Math.Min(region.X, region.X + region.Width);
        float top = Math.Min(region.Y, region.Y + region.Height);
        float right = Math.Max(region.X, region.X + region.Width);
        float bottom = Math.Max(region.Y, region.Y + region.Height);

        return left >= 0 && top >= 0 && right <= imageWidth && bottom <= imageHeight;
    }
}
