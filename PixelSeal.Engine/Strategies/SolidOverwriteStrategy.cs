using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine.Strategies;

/// <summary>
/// Solid Overwrite redaction strategy.
/// Fills the region with a solid 100% opaque color.
/// Supports configurable color, border, and rounded corners.
/// </summary>
public sealed class SolidOverwriteStrategy : IRedactionStrategy
{
    public RedactionMode Mode => RedactionMode.SolidOverwrite;

    public void Apply(SKCanvas canvas, SKRect region, RedactionOptions options)
    {
        // Clamp corner radius to valid range (0-6)
        float cornerRadius = Math.Clamp(options.CornerRadius, 0, 6);

        // Create fill paint - always 100% opaque
        using var fillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ColorParser.Parse(options.FillColor).WithAlpha(255),
            IsAntialias = true
        };

        // Draw the filled rectangle
        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(region, cornerRadius, cornerRadius, fillPaint);
        }
        else
        {
            canvas.DrawRect(region, fillPaint);
        }

        // Draw border if configured
        if (options.BorderThickness > 0 && !string.IsNullOrEmpty(options.BorderColor))
        {
            using var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ColorParser.Parse(options.BorderColor).WithAlpha(255),
                StrokeWidth = options.BorderThickness,
                IsAntialias = true
            };

            // Inset the border rect to draw inside the region
            float inset = options.BorderThickness / 2;
            var borderRect = SKRect.Create(
                region.Left + inset,
                region.Top + inset,
                region.Width - options.BorderThickness,
                region.Height - options.BorderThickness);

            if (cornerRadius > 0)
            {
                canvas.DrawRoundRect(borderRect, cornerRadius, cornerRadius, borderPaint);
            }
            else
            {
                canvas.DrawRect(borderRect, borderPaint);
            }
        }
    }
}
