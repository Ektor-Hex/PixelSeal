namespace PixelSeal.Models;

/// <summary>
/// Represents a rectangular region to be redacted.
/// Coordinates are relative to the original image dimensions.
/// </summary>
public sealed class RedactionRegion
{
    /// <summary>
    /// Unique identifier for this region.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// X coordinate of the region's left edge (image-relative pixels).
    /// </summary>
    public float X { get; set; }

    /// <summary>
    /// Y coordinate of the region's top edge (image-relative pixels).
    /// </summary>
    public float Y { get; set; }

    /// <summary>
    /// Width of the region in pixels.
    /// </summary>
    public float Width { get; set; }

    /// <summary>
    /// Height of the region in pixels.
    /// </summary>
    public float Height { get; set; }

    /// <summary>
    /// Redaction mode to apply to this region.
    /// </summary>
    public RedactionMode Mode { get; set; } = RedactionMode.SolidOverwrite;

    /// <summary>
    /// Shape of the region (rectangle, ellipse, or free-form).
    /// </summary>
    public RegionShape Shape { get; set; } = RegionShape.Rectangle;

    /// <summary>
    /// Path points for free-form regions (brush strokes).
    /// Only used when Shape is FreeForm.
    /// </summary>
    public List<(float X, float Y)> PathPoints { get; set; } = new();

    /// <summary>
    /// Brush size for free-form regions.
    /// </summary>
    public float BrushSize { get; set; } = 20f;

    /// <summary>
    /// Configuration options for the redaction.
    /// </summary>
    public RedactionOptions Options { get; set; } = new();

    /// <summary>
    /// Display name for the region (auto-generated or user-defined).
    /// </summary>
    public string DisplayName { get; set; } = "Region";

    /// <summary>
    /// Whether this region is currently selected in the UI.
    /// UI-only property, not used by engine.
    /// </summary>
    public bool IsSelected { get; set; }

    /// <summary>
    /// Normalizes the region bounds to ensure positive width/height.
    /// </summary>
    public void Normalize()
    {
        if (Width < 0)
        {
            X += Width;
            Width = -Width;
        }
        if (Height < 0)
        {
            Y += Height;
            Height = -Height;
        }
    }

    /// <summary>
    /// Checks if a point (image coordinates) is inside this region.
    /// </summary>
    public bool Contains(float px, float py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }

    /// <summary>
    /// Creates a copy of this region with a new ID.
    /// </summary>
    public RedactionRegion Clone()
    {
        return new RedactionRegion
        {
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Mode = Mode,
            Shape = Shape,
            PathPoints = new List<(float X, float Y)>(PathPoints),
            BrushSize = BrushSize,
            Options = Options.Clone(),
            DisplayName = DisplayName,
            IsSelected = false
        };
    }
}
