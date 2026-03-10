using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Engine.Strategies;

/// <summary>
/// Aesthetic Blur redaction strategy.
/// Creates an iOS-style privacy blur effect by blurring actual image content.
/// 
/// ⚠️ WARNING: THIS IS NOT A SECURE REDACTION MODE ⚠️
/// 
/// This blur is purely aesthetic/visual and may be partially reversible.
/// It does NOT provide cryptographic security or irreversible data destruction.
/// 
/// USE CASES:
/// - Visual preview/aesthetic purposes
/// - Non-sensitive content obfuscation  
/// - Design mockups where full security is not required
/// 
/// DO NOT USE FOR:
/// - Personally Identifiable Information (PII)
/// - Financial data  
/// - Medical records
/// - Any sensitive information requiring secure redaction
/// 
/// For secure redaction, use SolidOverwrite or other destructive strategies.
/// </summary>
public sealed class AestheticBlurStrategy : IRedactionStrategy
{
    public RedactionMode Mode => RedactionMode.AestheticBlur;

    /// <summary>
    /// Standard Apply method - uses fallback frosted glass effect WITHOUT environmental color.
    /// This is called when the engine doesn't provide source bitmap access.
    /// </summary>
    public void Apply(SKCanvas canvas, SKRect region, RedactionOptions options)
    {
        float cornerRadius = Math.Clamp(options.CornerRadius, 0, 20);
        ApplyFallbackBlur(canvas, null, region, cornerRadius, options);
    }

    /// <summary>
    /// Extended Apply method with source bitmap access for real pixel blurring.
    /// </summary>
    public void ApplyWithSource(SKCanvas canvas, SKBitmap sourceBitmap, SKRect region, RedactionOptions options)
    {
        // Clamp blur parameters to safe ranges
        float blurRadius = Math.Clamp(options.BlurRadius, 5, 50);
        float desaturation = Math.Clamp(options.BlurDesaturation, 0, 1);
        float overlayOpacity = Math.Clamp(options.BlurOverlayOpacity, 0, 1);
        float cornerRadius = Math.Clamp(options.CornerRadius, 0, 20);

        // ═══════════════════════════════════════════════════════════════
        // STEP 1: EXPAND REGION TO AVOID EDGE HALOS
        // Expand by blurRadius * 2.5 to ensure smooth blending
        // ═══════════════════════════════════════════════════════════════

        float expansion = blurRadius * 2.5f;
        var expandedRegion = SKRect.Create(
            Math.Max(0, region.Left - expansion),
            Math.Max(0, region.Top - expansion),
            Math.Min(sourceBitmap.Width - Math.Max(0, region.Left - expansion), region.Width + (expansion * 2)),
            Math.Min(sourceBitmap.Height - Math.Max(0, region.Top - expansion), region.Height + (expansion * 2)));

        int width = (int)Math.Ceiling(expandedRegion.Width);
        int height = (int)Math.Ceiling(expandedRegion.Height);
        
        if (width <= 0 || height <= 0) 
        {
            ApplyFallbackBlur(canvas, sourceBitmap, region, cornerRadius, options);
            return;
        }

        // ═══════════════════════════════════════════════════════════════
        // STEP 2: EXTRACT REGION FROM SOURCE BITMAP
        // ═══════════════════════════════════════════════════════════════

        using var capturedBitmap = new SKBitmap(width, height);
        using var captureCanvas = new SKCanvas(capturedBitmap);

        // Extract the expanded region from the source bitmap
        var sourceRect = SKRect.Create(expandedRegion.Left, expandedRegion.Top, width, height);
        var destRect = SKRect.Create(0, 0, width, height);
        
        using var extractPaint = new SKPaint
        {
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };
        
        captureCanvas.Clear(SKColors.Transparent);
        captureCanvas.DrawBitmap(sourceBitmap, sourceRect, destRect, extractPaint);

        // ═══════════════════════════════════════════════════════════════
        // STEP 2.5: SAMPLE ENVIRONMENTAL COLOR FROM ORIGINAL SOURCE
        // CRITICAL: Sample from source bitmap, NOT from captured/processed pixels
        // ═══════════════════════════════════════════════════════════════

        var environmentalColor = SampleEnvironmentalColor(sourceBitmap, expandedRegion);
        float environmentalLuminance = CalculateLuminance(environmentalColor);

        // ═════════════════════════════════════════════════════════════
        // STEP 3: CREATE DESATURATION COLOR MATRIX
        // ═══════════════════════════════════════════════════════════════

        float saturation = 1.0f - desaturation;
        float sr = (1 - saturation) * 0.299f;
        float sg = (1 - saturation) * 0.587f;
        float sb = (1 - saturation) * 0.114f;

        var colorMatrix = new float[]
        {
            sr + saturation, sr,              sr,              0, 0,
            sg,              sg + saturation, sg,              0, 0,
            sb,              sb,              sb + saturation, 0, 0,
            0,               0,               0,               1, 0
        };

        // ═══════════════════════════════════════════════════════════════
        // STEP 4: APPLY BLUR + DESATURATION TO CAPTURED PIXELS
        // ═══════════════════════════════════════════════════════════════

        using var blurredBitmap = new SKBitmap(width, height);
        using var blurCanvas = new SKCanvas(blurredBitmap);

        using var blurFilter = SKImageFilter.CreateBlur(blurRadius, blurRadius, SKShaderTileMode.Clamp);
        using var colorFilter = SKColorFilter.CreateColorMatrix(colorMatrix);
        using var colorImageFilter = SKImageFilter.CreateColorFilter(colorFilter);
        using var combinedFilter = SKImageFilter.CreateCompose(colorImageFilter, blurFilter);

        using var blurPaint = new SKPaint
        {
            ImageFilter = combinedFilter,
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };

        blurCanvas.Clear(SKColors.Transparent);
        blurCanvas.DrawBitmap(capturedBitmap, 0, 0, blurPaint);

        // ═══════════════════════════════════════════════════════════════
        // STEP 4.5: REDUCE CONTRAST TO BREAK SILHOUETTE READABILITY
        // Collapses contrast by 20% to make blur behave like frosted glass
        // ═══════════════════════════════════════════════════════════════

        ReduceContrast(blurredBitmap, 0.20f); // 20% contrast reduction

        // ═══════════════════════════════════════════════════════════════
        // STEP 5: APPLY TWO-LAYER BLUR SYSTEM (REBALANCED)
        // Layer 1: Strong neutral base (obscures shape, 65% contribution)
        // Layer 2: Environmental tint (subtle adaptation, 35% contribution)
        // ═══════════════════════════════════════════════════════════════

        // Choose neutral base color based on environmental luminance
        // Light scenes: #F2F2F7 (iOS light glass)
        // Dark scenes: #1C1C1E (iOS dark glass)
        var neutralBase = environmentalLuminance > 0.5f
            ? ColorParser.Parse("#F2F2F7")  // Light neutral
            : ColorParser.Parse("#1C1C1E"); // Dark neutral

        // Layer 1: Strong neutral base (92% opacity) - dominates to obscure shape
        byte baseAlpha = (byte)(255 * 0.92f); // 92% opacity - strengthened base
        using (var basePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = neutralBase.WithAlpha(baseAlpha),
            IsAntialias = true
        })
        {
            blurCanvas.DrawRect(0, 0, width, height, basePaint);
        }

        // Layer 2: Environmental tint (35% environmental, 22% opacity)
        // Increased from 30% to 35% environmental influence for better color adaptation
        var appleGray = ColorParser.Parse("#D1D1D6");
        var environmentalTint = BlendColors(environmentalColor, appleGray, 0.35f); // 35% environmental
        
        // Increased tint opacity to 22% for better visibility
        byte tintAlpha = (byte)(255 * 0.22f); // 22% opacity - strengthened tint
        using (var tintPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = environmentalTint.WithAlpha(tintAlpha),
            IsAntialias = true
        })
        {
            blurCanvas.DrawRect(0, 0, width, height, tintPaint);
        }
        
        // ═══════════════════════════════════════════════════════════════
        // STEP 6: CREATE SOFT FEATHERED MASK
        // ═══════════════════════════════════════════════════════════════

        float featherSize = Math.Min(blurRadius * 0.6f, 20f);
        
        using var maskBitmap = new SKBitmap(width, height);
        using var maskCanvas = new SKCanvas(maskBitmap);
        
        maskCanvas.Clear(SKColors.Transparent);

        // Calculate mask region (original region relative to expanded region)
        var maskRegion = SKRect.Create(
            region.Left - expandedRegion.Left,
            region.Top - expandedRegion.Top,
            region.Width,
            region.Height);

        // Create path for mask
        using var maskPath = new SKPath();
        if (cornerRadius > 0)
        {
            maskPath.AddRoundRect(maskRegion, cornerRadius, cornerRadius);
        }
        else
        {
            maskPath.AddRect(maskRegion);
        }

        using var maskFillPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = SKColors.White,
            IsAntialias = true
        };

        maskCanvas.DrawPath(maskPath, maskFillPaint);

        // Apply blur to mask for soft feathering
        using var featheredMask = new SKBitmap(width, height);
        using var featherCanvas = new SKCanvas(featheredMask);
        
        using var featherPaint = new SKPaint
        {
            ImageFilter = SKImageFilter.CreateBlur(featherSize, featherSize, SKShaderTileMode.Clamp),
            FilterQuality = SKFilterQuality.High,
            IsAntialias = true
        };
        
        featherCanvas.DrawBitmap(maskBitmap, 0, 0, featherPaint);

        // ═══════════════════════════════════════════════════════════════
        // STEP 7: COMPOSITE BLURRED RESULT WITH SOFT MASK
        // ═══════════════════════════════════════════════════════════════

        using var maskedBitmap = new SKBitmap(width, height);
        using var maskedCanvas = new SKCanvas(maskedBitmap);
        
        maskedCanvas.Clear(SKColors.Transparent);
        maskedCanvas.DrawBitmap(blurredBitmap, 0, 0);
        
        // Apply mask using DstIn blend mode (destination kept where source is opaque)
        using var maskBlendPaint = new SKPaint
        {
            BlendMode = SKBlendMode.DstIn,
            FilterQuality = SKFilterQuality.High
        };
        maskedCanvas.DrawBitmap(featheredMask, 0, 0, maskBlendPaint);

        // Draw final result onto main canvas at the expanded region position
        using var finalPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High
        };
        
        canvas.DrawBitmap(maskedBitmap, expandedRegion.Left, expandedRegion.Top, finalPaint);
    }

    /// <summary>
    /// Fallback blur using two-layer system: neutral base + environmental tint.
    /// Layer 1: Opaque neutral base for content obscuration
    /// Layer 2: Subtle environmental color modulation
    /// </summary>
    private void ApplyFallbackBlur(SKCanvas canvas, SKBitmap? sourceBitmap, SKRect region, float cornerRadius, RedactionOptions options)
    {
        SKColor neutralBase;
        SKColor environmentalTint;
        
        if (sourceBitmap != null)
        {
            // Sample environmental color from source bitmap
            float blurRadius = Math.Clamp(options.BlurRadius, 5, 50);
            float expansion = blurRadius * 2.5f;
            var expandedRegion = SKRect.Create(
                Math.Max(0, region.Left - expansion),
                Math.Max(0, region.Top - expansion),
                Math.Min(sourceBitmap.Width - Math.Max(0, region.Left - expansion), region.Width + expansion * 2),
                Math.Min(sourceBitmap.Height - Math.Max(0, region.Top - expansion), region.Height + expansion * 2)
            );
            
            var environmentalColor = SampleEnvironmentalColor(sourceBitmap, expandedRegion);
            float environmentalLuminance = CalculateLuminance(environmentalColor);
            
            // Choose neutral base based on scene brightness
            neutralBase = environmentalLuminance > 0.5f
                ? ColorParser.Parse("#F2F2F7")  // Light neutral
                : ColorParser.Parse("#1C1C1E"); // Dark neutral
            
            // Create subtle environmental tint (30% influence)
            var appleGray = ColorParser.Parse("#D1D1D6");
            environmentalTint = BlendColors(environmentalColor, appleGray, 0.35f); // 35% environmental
        }
        else
        {
            // No source - use medium neutral base
            neutralBase = ColorParser.Parse("#E5E5EA");
            environmentalTint = ColorParser.Parse("#D1D1D6");
        }
        
        // Layer 1: Strong neutral base (92% opacity) - strengthened
        using (var basePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = neutralBase.WithAlpha(235), // 92% opacity (255 * 0.92 ≈ 235)
            IsAntialias = true
        })
        {
            if (cornerRadius > 0)
                canvas.DrawRoundRect(region, cornerRadius, cornerRadius, basePaint);
            else
                canvas.DrawRect(region, basePaint);
        }
        
        // Layer 2: Environmental tint (22% opacity) - strengthened
        using (var tintPaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            Color = environmentalTint.WithAlpha(56), // 22% opacity (255 * 0.22 ≈ 56)
            IsAntialias = true
        })
        {
            if (cornerRadius > 0)
                canvas.DrawRoundRect(region, cornerRadius, cornerRadius, tintPaint);
            else
                canvas.DrawRect(region, tintPaint);
        }
    }

    /// <summary>
    /// Samples the dominant color from the ORIGINAL source image at the specified region.
    /// CRITICAL: This samples from the unmodified source bitmap, not from any processed/captured data.
    /// Excludes extreme highlights to get natural environmental color.
    /// </summary>
    /// <param name="sourceBitmap">The original, unmodified source image</param>
    /// <param name="region">The region to sample in original image coordinates</param>
    private SKColor SampleEnvironmentalColor(SKBitmap sourceBitmap, SKRect region)
    {
        long totalR = 0, totalG = 0, totalB = 0;
        int validPixelCount = 0;

        // Calculate sampling bounds, clamped to source bitmap dimensions
        int startX = (int)Math.Max(0, region.Left);
        int startY = (int)Math.Max(0, region.Top);
        int endX = (int)Math.Min(sourceBitmap.Width, region.Right);
        int endY = (int)Math.Min(sourceBitmap.Height, region.Bottom);

        // Sample at reduced resolution for performance (every 4th pixel)
        for (int y = startY; y < endY; y += 4)
        {
            for (int x = startX; x < endX; x += 4)
            {
                // Sample directly from ORIGINAL source bitmap
                var pixel = sourceBitmap.GetPixel(x, y);
                
                // Ignore extreme highlights (luminance > 0.9) and very dark pixels (< 0.1)
                float luminance = CalculateLuminance(pixel);
                if (luminance > 0.9f || luminance < 0.1f)
                    continue;

                totalR += pixel.Red;
                totalG += pixel.Green;
                totalB += pixel.Blue;
                validPixelCount++;
            }
        }

        // Fallback to neutral gray if no valid pixels found
        if (validPixelCount == 0)
            return new SKColor(209, 209, 214); // #D1D1D6

        byte avgR = (byte)(totalR / validPixelCount);
        byte avgG = (byte)(totalG / validPixelCount);
        byte avgB = (byte)(totalB / validPixelCount);

        return new SKColor(avgR, avgG, avgB);
    }

    /// <summary>
    /// Calculates the relative luminance of a color (0-1 range).
    /// Uses standard sRGB luminance formula.
    /// </summary>
    private float CalculateLuminance(SKColor color)
    {
        float r = color.Red / 255.0f;
        float g = color.Green / 255.0f;
        float b = color.Blue / 255.0f;

        return (0.299f * r + 0.587f * g + 0.114f * b);
    }

    /// <summary>
    /// Blends two colors with the specified ratio.
    /// </summary>
    /// <param name="color1">First color</param>
    /// <param name="color2">Second color</param>
    /// <param name="ratio">Ratio of color1 (0-1, where 1 = 100% color1)</param>
    private SKColor BlendColors(SKColor color1, SKColor color2, float ratio)
    {
        ratio = Math.Clamp(ratio, 0, 1);
        float invRatio = 1.0f - ratio;

        byte r = (byte)(color1.Red * ratio + color2.Red * invRatio);
        byte g = (byte)(color1.Green * ratio + color2.Green * invRatio);
        byte b = (byte)(color1.Blue * ratio + color2.Blue * invRatio);

        return new SKColor(r, g, b);
    }

    /// <summary>
    /// Adjusts a color's luminance to match a target value within a tolerance.
    /// Preserves hue and saturation.
    /// </summary>
    private SKColor AdjustColorLuminance(SKColor color, float targetLuminance, float targetDeviation)
    {
        float currentLuminance = CalculateLuminance(color);
        
        // Only adjust if outside the acceptable range
        float minLuminance = targetLuminance - targetDeviation;
        float maxLuminance = targetLuminance + targetDeviation;
        
        if (currentLuminance >= minLuminance && currentLuminance <= maxLuminance)
            return color; // Already in range

        // Calculate adjustment factor
        float factor = currentLuminance > 0 ? targetLuminance / currentLuminance : 1.0f;
        factor = Math.Clamp(factor, 0.5f, 2.0f); // Prevent extreme adjustments

        byte r = (byte)Math.Clamp(color.Red * factor, 0, 255);
        byte g = (byte)Math.Clamp(color.Green * factor, 0, 255);
        byte b = (byte)Math.Clamp(color.Blue * factor, 0, 255);

        return new SKColor(r, g, b);
    }

    /// <summary>
    /// Reduces contrast in a bitmap to break silhouette readability.
    /// Pushes pixels toward midpoint gray (128) to collapse shape perception.
    /// This makes the blur behave like frosted glass material.
    /// </summary>
    /// <param name="bitmap">Bitmap to modify in-place</param>
    /// <param name="amount">Contrast reduction (0.0-1.0, e.g., 0.20 = 20% reduction)</param>
    private void ReduceContrast(SKBitmap bitmap, float amount)
    {
        amount = Math.Clamp(amount, 0f, 1f);
        
        // Access pixels directly for performance
        IntPtr pixelsAddr = bitmap.GetPixels();
        int pixelCount = bitmap.Width * bitmap.Height;
        
        unsafe
        {
            uint* pixels = (uint*)pixelsAddr.ToPointer();
            
            for (int i = 0; i < pixelCount; i++)
            {
                uint pixel = pixels[i];
                
                // Extract ARGB components (SkiaSharp uses BGRA byte order)
                byte a = (byte)((pixel >> 24) & 0xFF);
                byte r = (byte)((pixel >> 16) & 0xFF);
                byte g = (byte)((pixel >> 8) & 0xFF);
                byte b = (byte)(pixel & 0xFF);
                
                // Push each channel toward middle gray (128)
                // This coll apses contrast while preserving color relationships
                r = (byte)(r + (128 - r) * amount);
                g = (byte)(g + (128 - g) * amount);
                b = (byte)(b + (128 - b) * amount);
                
                // Reconstruct pixel
                pixels[i] = ((uint)a << 24) | ((uint)r << 16) | ((uint)g << 8) | b;
            }
        }
    }
}
