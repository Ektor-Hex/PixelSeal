using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine.Strategies;

/// <summary>
/// Geometric Pattern redaction strategy.
/// Renders synthetic patterns (lines, grid, dots) that completely replace content.
/// All patterns are clipped strictly to the region bounds.
/// </summary>
public sealed class GeometricPatternStrategy : IRedactionStrategy
{
    public RedactionMode Mode => RedactionMode.GeometricPattern;

    public void Apply(SKCanvas canvas, SKRect region, RedactionOptions options)
    {
        // Save canvas state for clipping
        canvas.Save();
        canvas.ClipRect(region);

        // Draw solid background first - 100% opaque
        using var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ColorParser.Parse(options.PatternBackgroundColor).WithAlpha(255),
            IsAntialias = false
        };
        canvas.DrawRect(region, bgPaint);

        // Draw pattern
        using var patternPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ColorParser.Parse(options.PatternColor).WithAlpha(255),
            StrokeWidth = 1,
            IsAntialias = true
        };

        float density = Math.Max(4, options.PatternDensity);

        switch (options.Pattern)
        {
            case PatternType.Lines:
                DrawLines(canvas, region, patternPaint, density);
                break;
            case PatternType.Grid:
                DrawGrid(canvas, region, patternPaint, density);
                break;
            case PatternType.Dots:
                DrawDots(canvas, region, patternPaint, density);
                break;
        }

        // Restore canvas state
        canvas.Restore();
    }

    private static void DrawLines(SKCanvas canvas, SKRect region, SKPaint paint, float spacing)
    {
        // Draw diagonal lines from top-left to bottom-right
        float totalDiagonal = region.Width + region.Height;
        
        for (float offset = 0; offset < totalDiagonal; offset += spacing)
        {
            float x1 = region.Left + offset;
            float y1 = region.Top;
            float x2 = region.Left;
            float y2 = region.Top + offset;

            // Clamp to region bounds
            if (x1 > region.Right)
            {
                y1 += x1 - region.Right;
                x1 = region.Right;
            }
            if (y2 > region.Bottom)
            {
                x2 += y2 - region.Bottom;
                y2 = region.Bottom;
            }

            if (x2 <= region.Right && y1 <= region.Bottom)
            {
                canvas.DrawLine(x1, y1, x2, y2, paint);
            }
        }
    }

    private static void DrawGrid(SKCanvas canvas, SKRect region, SKPaint paint, float spacing)
    {
        // Draw vertical lines
        for (float x = region.Left; x <= region.Right; x += spacing)
        {
            canvas.DrawLine(x, region.Top, x, region.Bottom, paint);
        }

        // Draw horizontal lines
        for (float y = region.Top; y <= region.Bottom; y += spacing)
        {
            canvas.DrawLine(region.Left, y, region.Right, y, paint);
        }
    }

    private static void DrawDots(SKCanvas canvas, SKRect region, SKPaint paint, float spacing)
    {
        paint.Style = SKPaintStyle.Fill;
        float dotRadius = Math.Max(1, spacing / 4);

        for (float x = region.Left + spacing / 2; x < region.Right; x += spacing)
        {
            for (float y = region.Top + spacing / 2; y < region.Bottom; y += spacing)
            {
                canvas.DrawCircle(x, y, dotRadius, paint);
            }
        }
    }
}
