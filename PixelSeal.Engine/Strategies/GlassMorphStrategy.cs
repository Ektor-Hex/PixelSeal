using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine.Strategies;

/// <summary>
/// GlassMorph redaction strategy.
/// Creates a frosted glass effect with color tint, similar to Twitter/X stickers.
/// IMPORTANT: First completely destroys original pixels with solid fill,
/// then applies the glass effect on top. No original content is visible.
/// </summary>
public sealed class GlassMorphStrategy : IRedactionStrategy
{
    public RedactionMode Mode => RedactionMode.GlassMorph;

    public void Apply(SKCanvas canvas, SKRect region, RedactionOptions options)
    {
        float cornerRadius = Math.Clamp(options.CornerRadius, 0, 20);
        
        // Parse tint color (default to a subtle blue-purple glass tint)
        var tintColor = !string.IsNullOrEmpty(options.FillColor) 
            ? ColorParser.Parse(options.FillColor) 
            : new SKColor(180, 160, 255); // Default lavender tint

        int width = (int)Math.Ceiling(region.Width);
        int height = (int)Math.Ceiling(region.Height);
        
        if (width <= 0 || height <= 0) return;

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: COMPLETELY DESTROY ORIGINAL PIXELS WITH SOLID BASE
        // This ensures NO original content is visible through the glass
        // ═══════════════════════════════════════════════════════════════
        
        // Create base color - darker version of tint for complete opacity
        var baseColor = new SKColor(
            (byte)Math.Max(0, tintColor.Red - 60),
            (byte)Math.Max(0, tintColor.Green - 60),
            (byte)Math.Max(0, tintColor.Blue - 60),
            255); // 100% opaque - destroys all original pixels

        using var basePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = baseColor,
            IsAntialias = true
        };

        // Draw solid base first to destroy original pixels
        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(region, cornerRadius, cornerRadius, basePaint);
        }
        else
        {
            canvas.DrawRect(region, basePaint);
        }

        // ═══════════════════════════════════════════════════════════════
        // STEP 2: CREATE GLASS EFFECT BITMAP (to overlay on solid base)
        // ═══════════════════════════════════════════════════════════════

        using var glassBitmap = new SKBitmap(width, height);
        using var glassCanvas = new SKCanvas(glassBitmap);
        
        // Clear with transparent
        glassCanvas.Clear(SKColors.Transparent);

        // Create gradient overlay for glass depth
        var gradientColors = new SKColor[]
        {
            new SKColor(255, 255, 255, 80),  // Light at top-left
            new SKColor(255, 255, 255, 20),  // Fade in middle  
            new SKColor(0, 0, 0, 40)         // Shadow at bottom-right
        };

        using var gradientPaint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, height),
                gradientColors,
                new float[] { 0f, 0.5f, 1f },
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        glassCanvas.DrawRect(0, 0, width, height, gradientPaint);

        // Add noise/grain texture for realistic frosted glass
        AddNoiseTexture(glassCanvas, width, height, 12);

        // Add light reflection streak
        AddLightReflection(glassCanvas, width, height);

        // Apply subtle blur for frosted effect
        using var blurredBitmap = ApplyBlur(glassBitmap, 2);

        // ═══════════════════════════════════════════════════════════════
        // STEP 3: DRAW GLASS OVERLAY ON TOP OF SOLID BASE
        // ═══════════════════════════════════════════════════════════════

        // Create clipping path for rounded corners
        canvas.Save();
        
        using var clipPath = new SKPath();
        if (cornerRadius > 0)
        {
            clipPath.AddRoundRect(region, cornerRadius, cornerRadius);
        }
        else
        {
            clipPath.AddRect(region);
        }
        canvas.ClipPath(clipPath);

        // Draw the glass effect overlay
        using var bitmapPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };
        canvas.DrawBitmap(blurredBitmap, region, bitmapPaint);

        canvas.Restore();

        // ═══════════════════════════════════════════════════════════════
        // STEP 4: ADD GLASS BORDER HIGHLIGHTS
        // ═══════════════════════════════════════════════════════════════

        // Inner white highlight (top-left edge glow)
        using var innerHighlightPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1.5f,
            Color = new SKColor(255, 255, 255, 100),
            IsAntialias = true
        };
        
        var innerRect = SKRect.Create(
            region.Left + 1.5f,
            region.Top + 1.5f,
            region.Width - 3,
            region.Height - 3);
        
        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(innerRect, Math.Max(0, cornerRadius - 1.5f), Math.Max(0, cornerRadius - 1.5f), innerHighlightPaint);
        }
        else
        {
            canvas.DrawRect(innerRect, innerHighlightPaint);
        }

        // Outer subtle shadow
        using var outerShadowPaint = new SKPaint
        {
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 2f,
            Color = new SKColor(0, 0, 0, 60),
            IsAntialias = true,
            MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 2)
        };

        if (cornerRadius > 0)
        {
            canvas.DrawRoundRect(region, cornerRadius, cornerRadius, outerShadowPaint);
        }
        else
        {
            canvas.DrawRect(region, outerShadowPaint);
        }

        // Optional colored border
        if (options.BorderThickness > 0 && !string.IsNullOrEmpty(options.BorderColor))
        {
            using var borderPaint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                Color = ColorParser.Parse(options.BorderColor).WithAlpha(200),
                StrokeWidth = options.BorderThickness,
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

    private static void AddNoiseTexture(SKCanvas canvas, int width, int height, int intensity)
    {
        var random = new Random(42); // Fixed seed for consistency

        using var noisePaint = new SKPaint();
        
        // Draw sparse noise points
        for (int y = 0; y < height; y += 3)
        {
            for (int x = 0; x < width; x += 3)
            {
                int noise = random.Next(-intensity, intensity);
                byte alpha = (byte)Math.Clamp(40 + noise, 10, 70);
                byte gray = (byte)Math.Clamp(180 + noise * 3, 100, 255);
                
                noisePaint.Color = new SKColor(gray, gray, gray, alpha);
                canvas.DrawPoint(x, y, noisePaint);
            }
        }
    }

    private static void AddLightReflection(SKCanvas canvas, int width, int height)
    {
        // Diagonal light streak from top-left
        using var reflectionPaint = new SKPaint
        {
            Shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width * 0.5f, height * 0.3f),
                new SKColor[] 
                { 
                    new SKColor(255, 255, 255, 100),
                    new SKColor(255, 255, 255, 40),
                    new SKColor(255, 255, 255, 0)
                },
                new float[] { 0f, 0.4f, 1f },
                SKShaderTileMode.Clamp),
            IsAntialias = true
        };
        
        // Draw reflection area (top portion)
        canvas.DrawRect(0, 0, width * 0.7f, height * 0.5f, reflectionPaint);
    }

    private static SKBitmap ApplyBlur(SKBitmap source, float sigma)
    {
        var result = new SKBitmap(source.Width, source.Height);
        
        using var canvas = new SKCanvas(result);
        using var paint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(sigma, sigma)
        };
        
        canvas.DrawBitmap(source, 0, 0, paint);
        
        return result;
    }
}
