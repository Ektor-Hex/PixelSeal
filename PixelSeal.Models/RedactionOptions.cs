namespace PixelSeal.Models;

/// <summary>
/// Configuration options for redaction rendering.
/// All properties are mode-agnostic; each strategy uses what it needs.
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
    public string FillColor { get; set; } = "#1A1A1A";

    /// <summary>
    /// Border color (hex format: #RRGGBB). Empty string means no border.
    /// </summary>
    public string BorderColor { get; set; } = "";

    /// <summary>
    /// Border thickness in pixels (0 = no border).
    /// </summary>
    public float BorderThickness { get; set; } = 0;

    /// <summary>
    /// Corner radius in pixels (0-6 range enforced).
    /// </summary>
    public float CornerRadius { get; set; } = 0;

    // ═══════════════════════════════════════════════════════════════
    // SEMANTIC PLACEHOLDER OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Text to display in SemanticPlaceholder mode.
    /// </summary>
    public PlaceholderText PlaceholderLabel { get; set; } = PlaceholderText.Redacted;

    /// <summary>
    /// Text color for placeholder (hex format).
    /// </summary>
    public string TextColor { get; set; } = "#FFFFFF";

    /// <summary>
    /// Background color for placeholder panel.
    /// </summary>
    public string PlaceholderBackgroundColor { get; set; } = "#2D2D2D";

    // ═══════════════════════════════════════════════════════════════
    // GEOMETRIC PATTERN OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Type of pattern to render.
    /// </summary>
    public PatternType Pattern { get; set; } = PatternType.Lines;

    /// <summary>
    /// Pattern foreground color.
    /// </summary>
    public string PatternColor { get; set; } = "#3D3D3D";

    /// <summary>
    /// Pattern background color.
    /// </summary>
    public string PatternBackgroundColor { get; set; } = "#1A1A1A";

    /// <summary>
    /// Pattern density (spacing in pixels between elements).
    /// </summary>
    public float PatternDensity { get; set; } = 8;

    // ═══════════════════════════════════════════════════════════════
    // CONTEXT-AWARE PANEL OPTIONS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Panel background color.
    /// </summary>
    public string PanelBackgroundColor { get; set; } = "#F5F5F5";

    /// <summary>
    /// Panel border color.
    /// </summary>
    public string PanelBorderColor { get; set; } = "#E0E0E0";

    /// <summary>
    /// Whether to show an icon in the panel.
    /// </summary>
    public bool ShowIcon { get; set; } = true;

    /// <summary>
    /// Shadow offset in pixels (drawn, not blurred).
    /// </summary>
    public float ShadowOffset { get; set; } = 3;

    /// <summary>
    /// Shadow color.
    /// </summary>
    public string ShadowColor { get; set; } = "#40000000";

    /// <summary>
    /// Creates a deep copy of the options.
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
            ShadowColor = ShadowColor
        };
    }
}
