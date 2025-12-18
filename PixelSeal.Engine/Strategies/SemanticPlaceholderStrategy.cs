using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine.Strategies;

/// <summary>
/// Semantic Placeholder redaction strategy.
/// Renders an elegant panel with fixed text label.
/// Uses sans-serif typography on a solid neutral background.
/// </summary>
public sealed class SemanticPlaceholderStrategy : IRedactionStrategy
{
    public RedactionMode Mode => RedactionMode.SemanticPlaceholder;

    public void Apply(SKCanvas canvas, SKRect region, RedactionOptions options)
    {
        // Draw solid background - 100% opaque
        using var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ColorParser.Parse(options.PlaceholderBackgroundColor).WithAlpha(255),
            IsAntialias = true
        };
        
        float cornerRadius = Math.Clamp(options.CornerRadius, 0, 6);
        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(region, cornerRadius, cornerRadius, bgPaint);
        }
        else
        {
            canvas.DrawRect(region, bgPaint);
        }

        // Get the label text
        string labelText = options.PlaceholderLabel switch
        {
            PlaceholderText.Redacted => "REDACTED",
            PlaceholderText.Confidential => "CONFIDENTIAL",
            PlaceholderText.Hidden => "HIDDEN",
            _ => "REDACTED"
        };

        // Calculate font size based on region size
        float maxFontSize = Math.Min(region.Height * 0.4f, region.Width / labelText.Length * 1.5f);
        float fontSize = Math.Max(10, Math.Min(48, maxFontSize));

        // Create text paint
        using var textPaint = new SKPaint
        {
            Color = ColorParser.Parse(options.TextColor).WithAlpha(255),
            IsAntialias = true,
            TextSize = fontSize,
            Typeface = SKTypeface.FromFamilyName("Segoe UI", SKFontStyleWeight.SemiBold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
            TextAlign = SKTextAlign.Center
        };

        // Measure text to center vertically
        var textBounds = new SKRect();
        textPaint.MeasureText(labelText, ref textBounds);

        // Calculate center position
        float x = region.MidX;
        float y = region.MidY - textBounds.MidY;

        // Draw text
        canvas.DrawText(labelText, x, y, textPaint);

        // Draw subtle border
        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ColorParser.Parse(options.TextColor).WithAlpha(50),
            StrokeWidth = 1,
            IsAntialias = true
        };

        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(region, cornerRadius, cornerRadius, borderPaint);
        }
        else
        {
            canvas.DrawRect(region, borderPaint);
        }
    }
}
