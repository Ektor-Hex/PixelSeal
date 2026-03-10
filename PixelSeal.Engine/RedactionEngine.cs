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
            ApplyRegion(canvas, sourceImage, region);
        }

        canvas.Flush();
        return resultBitmap;
    }

    /// <summary>
    /// Applies a single redaction region to the canvas.
    /// </summary>
    private void ApplyRegion(SKCanvas canvas, SKBitmap sourceBitmap, RedactionRegion region)
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
                ApplyEllipseRegion(canvas, sourceBitmap, skRect, strategy, region.Options);
                break;
            case RegionShape.FreeForm:
                ApplyFreeFormRegion(canvas, sourceBitmap, region, strategy);
                break;
            case RegionShape.Rectangle:
            default:
                // Pass source bitmap to strategy for pixel-based effects
                if (strategy is Strategies.AestheticBlurStrategy blurStrategy)
                {
                    blurStrategy.ApplyWithSource(canvas, sourceBitmap, skRect, region.Options);
                }
                else
                {
                    strategy.Apply(canvas, skRect, region.Options);
                }
                break;
        }
    }

    /// <summary>
    /// Applies redaction to an elliptical region.
    /// </summary>
    private void ApplyEllipseRegion(SKCanvas canvas, SKBitmap sourceBitmap, SKRect bounds, IRedactionStrategy strategy, RedactionOptions options)
    {
        // Save canvas state and clip to ellipse
        canvas.Save();
        
        using var clipPath = new SKPath();
        clipPath.AddOval(bounds);
        canvas.ClipPath(clipPath);
        
        // Apply the strategy within the clipped area
        if (strategy is Strategies.AestheticBlurStrategy blurStrategy)
        {
            blurStrategy.ApplyWithSource(canvas, sourceBitmap, bounds, options);
        }
        else
        {
            strategy.Apply(canvas, bounds, options);
        }
        
        canvas.Restore();
    }

    /// <summary>
    /// Applies redaction to a free-form brush stroke region.
    /// </summary>
    private void ApplyFreeFormRegion(SKCanvas canvas, SKBitmap sourceBitmap, RedactionRegion region, IRedactionStrategy strategy)
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
        if (strategy is Strategies.AestheticBlurStrategy blurStrategy)
        {
            blurStrategy.ApplyWithSource(canvas, sourceBitmap, bounds, region.Options);
        }
        else
        {
            strategy.Apply(canvas, bounds, region.Options);
        }
        
        canvas.Restore();
    }

    /// <summary>
    /// Validates that a region is within image bounds and meets all requirements.
    /// Enhanced validation prevents silent failures and edge cases.
    /// </summary>
    /// <param name="region">The region to validate</param>
    /// <param name="imageWidth">Image width in pixels</param>
    /// <param name="imageHeight">Image height in pixels</param>
    /// <returns>True if region is valid, false otherwise</returns>
    public static bool IsRegionValid(RedactionRegion region, int imageWidth, int imageHeight)
    {
        // Check for zero-area regions
        if (region.Width == 0 || region.Height == 0)
            return false;

        // Normalize coordinates
        float left = Math.Min(region.X, region.X + region.Width);
        float top = Math.Min(region.Y, region.Y + region.Height);
        float right = Math.Max(region.X, region.X + region.Width);
        float bottom = Math.Max(region.Y, region.Y + region.Height);
        
        // Check minimum dimensions (at least 1px each)
        float width = right - left;
        float height = bottom - top;
        if (width < 1 || height < 1)
            return false;

        // Check image bounds
        if (left < 0 || top < 0 || right > imageWidth || bottom > imageHeight)
            return false;
            
        // FreeForm-specific validation
        if (region.Shape == RegionShape.FreeForm)
        {
            // Must have at least 2 points to draw a stroke
            if (region.PathPoints == null || region.PathPoints.Count < 2)
                return false;
                
            // Validate all points are within bounds
            foreach (var point in region.PathPoints)
            {
                if (point.X < 0 || point.X > imageWidth || point.Y < 0 || point.Y > imageHeight)
                    return false;
            }
            
            // Validate brush size is reasonable
            if (region.BrushSize < 1 || region.BrushSize > 200)
                return false;
        }

        return true;
    }
}
