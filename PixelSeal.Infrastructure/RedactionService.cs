using PixelSeal.Engine;
using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.Infrastructure;

/// <summary>
/// High-level service that orchestrates the complete redaction workflow.
/// Combines image loading, redaction engine, and secure export.
/// </summary>
public sealed class RedactionService : IDisposable
{
    private readonly ImageLoader _imageLoader;
    private readonly RedactionEngine _redactionEngine;
    private readonly SecureImageExporter _exporter;
    
    private SKBitmap? _currentImage;
    private string? _currentImagePath;

    public RedactionService()
    {
        _imageLoader = new ImageLoader();
        _redactionEngine = new RedactionEngine();
        _exporter = new SecureImageExporter();
    }

    /// <summary>
    /// Gets the currently loaded image.
    /// </summary>
    public SKBitmap? CurrentImage => _currentImage;

    /// <summary>
    /// Gets the path of the currently loaded image.
    /// </summary>
    public string? CurrentImagePath => _currentImagePath;

    /// <summary>
    /// Gets whether an image is currently loaded.
    /// </summary>
    public bool HasImage => _currentImage != null;

    /// <summary>
    /// Gets the dimensions of the current image.
    /// </summary>
    public (int Width, int Height) ImageDimensions => 
        _currentImage != null ? (_currentImage.Width, _currentImage.Height) : (0, 0);

    /// <summary>
    /// Loads an image from the specified path.
    /// </summary>
    public void LoadImage(string filePath)
    {
        // Dispose previous image if any
        _currentImage?.Dispose();

        _currentImage = _imageLoader.LoadImage(filePath);
        _currentImagePath = filePath;
    }

    /// <summary>
    /// Applies redactions and returns a preview bitmap.
    /// Does not affect the original loaded image.
    /// </summary>
    public SKBitmap ApplyRedactionsPreview(IEnumerable<RedactionRegion> regions)
    {
        if (_currentImage == null)
            throw new InvalidOperationException("No image loaded.");

        return _redactionEngine.ApplyRedactions(_currentImage, regions);
    }

    /// <summary>
    /// Applies redactions and exports to a file with full security measures.
    /// </summary>
    public void ApplyAndExport(IEnumerable<RedactionRegion> regions, string outputPath, ExportFormat format)
    {
        if (_currentImage == null)
            throw new InvalidOperationException("No image loaded.");

        // Apply redactions
        using var redactedBitmap = _redactionEngine.ApplyRedactions(_currentImage, regions);

        // Export securely
        _exporter.Export(redactedBitmap, outputPath, format);
    }

    /// <summary>
    /// Validates that a region is within the current image bounds.
    /// </summary>
    public bool IsRegionValid(RedactionRegion region)
    {
        if (_currentImage == null)
            return false;

        return RedactionEngine.IsRegionValid(region, _currentImage.Width, _currentImage.Height);
    }

    public void Dispose()
    {
        _currentImage?.Dispose();
        _currentImage = null;
    }
}
