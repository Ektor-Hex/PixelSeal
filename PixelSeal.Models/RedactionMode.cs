namespace PixelSeal.Models;

/// <summary>
/// Available redaction modes in PixelSeal.
/// Each mode completely replaces original pixels with synthetic content.
/// </summary>
public enum RedactionMode
{
    /// <summary>
    /// Solid color fill - 100% opaque, configurable color, optional rounded corners.
    /// </summary>
    SolidOverwrite,

    /// <summary>
    /// Elegant panel with fixed text ("REDACTED", "CONFIDENTIAL", "HIDDEN").
    /// </summary>
    SemanticPlaceholder,

    /// <summary>
    /// Synthetic geometric pattern (lines, grid, dots).
    /// </summary>
    GeometricPattern,

    /// <summary>
    /// UI card style panel with subtle border and drawn shadow.
    /// </summary>
    ContextAwarePanel,

    /// <summary>
    /// Frosted glass/morph effect with color tint.
    /// Stylish effect similar to Twitter/X stickers.
    /// </summary>
    GlassMorph,

    /// <summary>
    /// iOS-style aesthetic blur effect.
    /// WARNING: This is NOT a secure redaction mode.
    /// This blur is purely visual/aesthetic and may be reversible.
    /// Use only for non-sensitive content where visual obfuscation is sufficient.
    /// </summary>
    AestheticBlur
}
