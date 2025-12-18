namespace PixelSeal.Models;

/// <summary>
/// Shape types for redaction regions.
/// </summary>
public enum RegionShape
{
    /// <summary>
    /// Standard rectangle/square shape.
    /// </summary>
    Rectangle,

    /// <summary>
    /// Ellipse/circle shape.
    /// </summary>
    Ellipse,

    /// <summary>
    /// Free-form path drawn with brush tool.
    /// </summary>
    FreeForm
}
