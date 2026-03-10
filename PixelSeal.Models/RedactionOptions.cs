namespace PixelSeal.Models;

/// <summary>
/// Configuration options for redaction rendering.
/// All properties are mode-agnostic; each strategy uses what it needs.
/// IMMUTABLE: Properties are init-only to prevent accidental mutations after creation.
/// </summary>
public sealed class RedactionOptions
{
    // ═══════════════════════════════════════════════════════════════
    // SOLID OVERWRITE OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Primary fill color (hex format: #RRGGBB).
    /// Default: #1A1A1A (dark gray)
    /// </summary>
    public string FillColor { get; init; } = "#1A1A1A";

    /// <summary>
    /// Border color (hex format: #RRGGBB). Empty string means no border.
    /// </summary>
    public string BorderColor { get; init; } = "";

    /// <summary>
    /// Border thickness in pixels (0 = no border).
    /// </summary>
    public float BorderThickness { get; init; } = 0;

    /// <summary>
    /// Corner radius in pixels (0-6 range enforced).
    /// </summary>
    public float CornerRadius { get; init; } = 0;

    // ═══════════════════════════════════════════════════════════════
    // SEMANTIC PLACEHOLDER OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Text to display in SemanticPlaceholder mode.
    /// </summary>
    public PlaceholderText PlaceholderLabel { get; init; } = PlaceholderText.Redacted;

    /// <summary>
    /// Text color for placeholder (hex format).
    /// </summary>
    public string TextColor { get; init; } = "#FFFFFF";

    /// <summary>
    /// Background color for placeholder panel.
    /// </summary>
    public string PlaceholderBackgroundColor { get; init; } = "#2D2D2D";

    // ═══════════════════════════════════════════════════════════════
    // GEOMETRIC PATTERN OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Type of pattern to render.
    /// </summary>
    public PatternType Pattern { get; init; } = PatternType.Lines;

    /// <summary>
    /// Pattern foreground color.
    /// </summary>
    public string PatternColor { get; init; } = "#3D3D3D";

    /// <summary>
    /// Pattern background color.
    /// </summary>
    public string PatternBackgroundColor { get; init; } = "#1A1A1A";

    /// <summary>
    /// Pattern density (spacing in pixels between elements).
    /// </summary>
    public float PatternDensity { get; init; } = 8;

    // ═══════════════════════════════════════════════════════════════
    // CONTEXT-AWARE PANEL OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Panel background color.
    /// </summary>
    public string PanelBackgroundColor { get; init; } = "#F5F5F5";

    /// <summary>
    /// Panel border color.
    /// </summary>
    public string PanelBorderColor { get; init; } = "#E0E0E0";

    /// <summary>
    /// Whether to show an icon in the panel.
    /// </summary>
    public bool ShowIcon { get; init; } = true;

    /// <summary>
    /// Shadow offset in pixels (drawn, not blurred).
    /// </summary>
    public float ShadowOffset { get; init; } = 3;

    /// <summary>
    /// Shadow color.
    /// </summary>
    public string ShadowColor { get; init; } = "#40000000";

    // ═══════════════════════════════════════════════════════════════
    // AESTHETIC BLUR OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Gaussian blur sigma value (5-50 range).
    /// Higher values create stronger blur effect.
    /// Default: 25.0
    /// </summary>
    public float BlurRadius { get; init; } = 25.0f;

    /// <summary>
    /// Desaturation amount (0-1 range).
    /// 0 = no desaturation, 1 = full grayscale.
    /// Default: 0.3
    /// </summary>
    public float BlurDesaturation { get; init; } = 0.3f;

    /// <summary>
    /// Optional subtle overlay color (hex format: #AARRGGBB).
    /// Creates slight tint over the blur, similar to iOS privacy blur.
    /// Default: #20000000 (subtle dark overlay)
    /// </summary>
    public string BlurOverlayColor { get; init; } = "#20000000";

    /// <summary>
    /// Overlay opacity (0-1 range).
    /// Default: 0.15
    /// </summary>
    public float BlurOverlayOpacity { get; init; } = 0.15f;

    /// <summary>
    /// Creates a shallow copy of the options.
    /// Since all properties are immutable (init-only), the copy is safe.
    /// </summary>
    public RedactionOptions Clone()
    {
        return new RedactionOptions
        {
            FillColor = FillColor,
            BorderColor = BorderColor,
            BorderThickness = BorderThickness,
            CornerRadius = CornerRadius,
            PlaceholderLabel = PlaceholderLabel,
            TextColor = TextColor,
            PlaceholderBackgroundColor = PlaceholderBackgroundColor,
            Pattern = Pattern,
            PatternColor = PatternColor,
            PatternBackgroundColor = PatternBackgroundColor,
            PatternDensity = PatternDensity,
            PanelBackgroundColor = PanelBackgroundColor,
            PanelBorderColor = PanelBorderColor,
            ShowIcon = ShowIcon,
            ShadowOffset = ShadowOffset,
            ShadowColor = ShadowColor,
            BlurRadius = BlurRadius,
            BlurDesaturation = BlurDesaturation,
            BlurOverlayColor = BlurOverlayColor,
            BlurOverlayOpacity = BlurOverlayOpacity
        };
    }
}
