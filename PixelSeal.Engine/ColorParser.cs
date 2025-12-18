using SkiaSharp;

namespace PixelSeal.Engine;

/// <summary>
/// Utility class for parsing color strings to SKColor.
/// </summary>
internal static class ColorParser
{
    /// <summary>
    /// Parses a hex color string to SKColor.
    /// Supports formats: #RGB, #RRGGBB, #AARRGGBB
    /// </summary>
    public static SKColor Parse(string hex)
    {
        if (string.IsNullOrWhiteSpace(hex))
            return SKColors.Transparent;

        hex = hex.TrimStart('#');

        return hex.Length switch
        {
            3 => new SKColor(
                (byte)(Convert.ToInt32(hex.Substring(0, 1), 16) * 17),
                (byte)(Convert.ToInt32(hex.Substring(1, 1), 16) * 17),
                (byte)(Convert.ToInt32(hex.Substring(2, 1), 16) * 17)),
            
            6 => new SKColor(
                (byte)Convert.ToInt32(hex.Substring(0, 2), 16),
                (byte)Convert.ToInt32(hex.Substring(2, 2), 16),
                (byte)Convert.ToInt32(hex.Substring(4, 2), 16)),
            
            8 => new SKColor(
                (byte)Convert.ToInt32(hex.Substring(2, 2), 16),
                (byte)Convert.ToInt32(hex.Substring(4, 2), 16),
                (byte)Convert.ToInt32(hex.Substring(6, 2), 16),
                (byte)Convert.ToInt32(hex.Substring(0, 2), 16)),
            
            _ => SKColors.Black
        };
    }

    /// <summary>
    /// Converts SKColor to hex string.
    /// </summary>
    public static string ToHex(SKColor color)
    {
        return $"#{color.Red:X2}{color.Green:X2}{color.Blue:X2}";
    }
}
