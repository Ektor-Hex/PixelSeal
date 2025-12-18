using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine.Strategies;

/// <summary>
/// Context-Aware Panel redaction strategy.
/// Renders a UI card-style panel with subtle border and drawn shadow.
/// Optional icon support. No real transparency - all drawn elements are opaque.
/// </summary>
public sealed class ContextAwarePanelStrategy : IRedactionStrategy
{
    public RedactionMode Mode => RedactionMode.ContextAwarePanel;

    public void Apply(SKCanvas canvas, SKRect region, RedactionOptions options)
    {
        float shadowOffset = Math.Max(0, options.ShadowOffset);
        float cornerRadius = Math.Clamp(options.CornerRadius, 0, 6);

        // Draw shadow (offset solid rectangle, not blur)
        if (shadowOffset > 0)
        {
            var shadowRect = SKRect.Create(
                region.Left + shadowOffset,
                region.Top + shadowOffset,
                region.Width,
                region.Height);

            using var shadowPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = ColorParser.Parse(options.ShadowColor).WithAlpha(100),
                IsAntialias = true
            };

            if (cornerRadius > 0)
            {
                canvas.DrawRoundRect(shadowRect, cornerRadius, cornerRadius, shadowPaint);
            }
            else
            {
                canvas.DrawRect(shadowRect, shadowPaint);
            }
        }

        // Draw main panel background - 100% opaque
        using var bgPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = ColorParser.Parse(options.PanelBackgroundColor).WithAlpha(255),
            IsAntialias = true
        };

        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(region, cornerRadius, cornerRadius, bgPaint);
        }
        else
        {
            canvas.DrawRect(region, bgPaint);
        }

        // Draw subtle border
        using var borderPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = ColorParser.Parse(options.PanelBorderColor).WithAlpha(255),
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

        // Draw optional icon (shield/lock symbol)
        if (options.ShowIcon && region.Width > 40 && region.Height > 40)
        {
            DrawShieldIcon(canvas, region);
        }
    }

    private static void DrawShieldIcon(SKCanvas canvas, SKRect region)
    {
        // Calculate icon size and position
        float iconSize = Math.Min(region.Width, region.Height) * 0.3f;
        iconSize = Math.Min(iconSize, 32);
        
        float iconX = region.MidX;
        float iconY = region.MidY;

        using var iconPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            Color = new SKColor(120, 120, 120),
            StrokeWidth = 2,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        // Draw a simple shield shape
        using var path = new SKPath();
        float halfWidth = iconSize / 2;
        float height = iconSize * 0.6f;

        path.MoveTo(iconX, iconY - height / 2);  // Top center
        path.LineTo(iconX + halfWidth, iconY - height / 3);  // Top right
        path.LineTo(iconX + halfWidth, iconY + height / 6);  // Middle right
        path.LineTo(iconX, iconY + height / 2);  // Bottom center
        path.LineTo(iconX - halfWidth, iconY + height / 6);  // Middle left
        path.LineTo(iconX - halfWidth, iconY - height / 3);  // Top left
        path.Close();

        canvas.DrawPath(path, iconPaint);

        // Draw checkmark inside shield
        iconPaint.StrokeWidth = 1.5f;
        float checkSize = iconSize * 0.25f;
        canvas.DrawLine(
            iconX - checkSize / 2, iconY,
            iconX - checkSize / 6, iconY + checkSize / 3,
            iconPaint);
        canvas.DrawLine(
            iconX - checkSize / 6, iconY + checkSize / 3,
            iconX + checkSize / 2, iconY - checkSize / 3,
            iconPaint);
    }
}
