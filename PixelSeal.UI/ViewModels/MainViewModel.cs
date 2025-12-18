using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using PixelSeal.Infrastructure;
using PixelSeal.Models;
using SkiaSharp;

namespace PixelSeal.UI.ViewModels;

/// <summary>
/// Main ViewModel for the PixelSeal application.
/// Manages the application state, regions, and commands.
/// </summary>
public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly RedactionService _redactionService;
    private int _regionCounter = 1;

    public MainViewModel()
    {
        _redactionService = new RedactionService();
        Regions = new ObservableCollection<RegionViewModel>();
        
        // Set default mode options
        CurrentOptions = new RedactionOptions();
    }

    // ═══════════════════════════════════════════════════════════════
    // OBSERVABLE PROPERTIES
    // ═══════════════════════════════════════════════════════════════

    [ObservableProperty]
    private ImageSource? _imageSource;

    [ObservableProperty]
    private string _windowTitle = "PixelSeal — Secure Image Redaction";

    [ObservableProperty]
    private string _statusMessage = "Ready. Open an image to begin.";

    [ObservableProperty]
    private bool _hasImage;

    [ObservableProperty]
    private int _imageWidth;

    [ObservableProperty]
    private int _imageHeight;

    [ObservableProperty]
    private RedactionMode _selectedMode = RedactionMode.SolidOverwrite;

    [ObservableProperty]
    private RegionShape _selectedShape = RegionShape.Rectangle;

    [ObservableProperty]
    private double _brushSize = 20.0;

    [ObservableProperty]
    private RedactionOptions _currentOptions;

    [ObservableProperty]
    private ExportFormat _selectedExportFormat = ExportFormat.PNG;

    [ObservableProperty]
    private RegionViewModel? _selectedRegion;

    [ObservableProperty]
    private bool _isAddingRegion;

    [ObservableProperty]
    private bool _hasRegions;

    [ObservableProperty]
    private bool _isPreviewMode;

    public ObservableCollection<RegionViewModel> Regions { get; }

    /// <summary>
    /// Gets the current SKBitmap from the service.
    /// </summary>
    public SKBitmap? CurrentBitmap => _redactionService.CurrentImage;

    // ═══════════════════════════════════════════════════════════════
    // COMMANDS
    // ═══════════════════════════════════════════════════════════════

    [RelayCommand]
    private void OpenImage()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Open Image",
            Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp;*.webp|All Files|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                _redactionService.LoadImage(dialog.FileName);
                UpdateImageDisplay();
                
                // Clear existing regions
                Regions.Clear();
                _regionCounter = 1;
                SelectedRegion = null;
                HasRegions = false;
                IsPreviewMode = false;

                StatusMessage = $"Loaded: {System.IO.Path.GetFileName(dialog.FileName)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanExport))]
    private void ExportImage()
    {
        if (!HasImage || !HasRegions)
            return;

        var dialog = new SaveFileDialog
        {
            Title = "Export Redacted Image",
            Filter = SecureImageExporter.GetFileFilter(SelectedExportFormat),
            DefaultExt = SecureImageExporter.GetFileExtension(SelectedExportFormat),
            FileName = GenerateExportFileName()
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var regions = Regions.Select(r => r.ToModel()).ToList();
                _redactionService.ApplyAndExport(regions, dialog.FileName, SelectedExportFormat);
                
                StatusMessage = $"Exported: {System.IO.Path.GetFileName(dialog.FileName)}";
                MessageBox.Show("Image exported successfully.\n\nThe redacted areas have been permanently destroyed and replaced.", 
                    "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to export image: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private bool CanExport() => HasImage && HasRegions;

    [RelayCommand(CanExecute = nameof(CanStartAddRegion))]
    private void StartAddRegion()
    {
        IsAddingRegion = true;
        StatusMessage = "Click and drag on the image to create a region.";
    }

    private bool CanStartAddRegion() => HasImage;

    [RelayCommand]
    private void CancelAddRegion()
    {
        IsAddingRegion = false;
        StatusMessage = "Region creation cancelled.";
    }

    [RelayCommand(CanExecute = nameof(CanDeleteRegion))]
    private void DeleteRegion()
    {
        if (SelectedRegion != null)
        {
            Regions.Remove(SelectedRegion);
            SelectedRegion = null;
            HasRegions = Regions.Count > 0;
            StatusMessage = "Region deleted.";
        }
    }

    private bool CanDeleteRegion() => SelectedRegion != null;

    [RelayCommand(CanExecute = nameof(CanApplyRedaction))]
    private void ApplyRedaction()
    {
        IsPreviewMode = true;
        StatusMessage = "Preview mode. Export to save the final image.";
    }

    private bool CanApplyRedaction() => HasImage && HasRegions;

    [RelayCommand]
    private void ClearPreview()
    {
        IsPreviewMode = false;
        StatusMessage = "Returned to editing mode.";
    }

    // ═══════════════════════════════════════════════════════════════
    // PUBLIC METHODS
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// Creates a new region from the specified bounds.
    /// </summary>
    public void CreateRegion(Rect bounds)
    {
        var region = new RedactionRegion
        {
            X = (float)bounds.X,
            Y = (float)bounds.Y,
            Width = (float)bounds.Width,
            Height = (float)bounds.Height,
            Mode = SelectedMode,
            Shape = SelectedShape,
            BrushSize = (float)BrushSize,
            Options = CurrentOptions.Clone(),
            DisplayName = $"Region {_regionCounter++}"
        };

        region.Normalize();

        var viewModel = new RegionViewModel(region);
        Regions.Add(viewModel);
        SelectedRegion = viewModel;
        HasRegions = true;
        IsAddingRegion = false;
        
        StatusMessage = $"Created: {region.DisplayName}";
    }

    /// <summary>
    /// Creates a new free-form region from brush stroke points.
    /// </summary>
    public void CreateFreeFormRegion(List<Point> points)
    {
        if (points.Count < 2)
            return;

        // Calculate bounding box
        double minX = points.Min(p => p.X);
        double minY = points.Min(p => p.Y);
        double maxX = points.Max(p => p.X);
        double maxY = points.Max(p => p.Y);

        var region = new RedactionRegion
        {
            X = (float)minX,
            Y = (float)minY,
            Width = (float)(maxX - minX),
            Height = (float)(maxY - minY),
            Mode = SelectedMode,
            Shape = RegionShape.FreeForm,
            BrushSize = (float)BrushSize,
            PathPoints = points.Select(p => ((float)p.X, (float)p.Y)).ToList(),
            Options = CurrentOptions.Clone(),
            DisplayName = $"Brush {_regionCounter++}"
        };

        var viewModel = new RegionViewModel(region);
        Regions.Add(viewModel);
        SelectedRegion = viewModel;
        HasRegions = true;
        IsAddingRegion = false;
        
        StatusMessage = $"Created: {region.DisplayName}";
    }

    /// <summary>
    /// Updates the selected region's position.
    /// </summary>
    public void UpdateRegionPosition(RegionViewModel region, double x, double y)
    {
        region.X = x;
        region.Y = y;
    }

    /// <summary>
    /// Updates the selected region's size.
    /// </summary>
    public void UpdateRegionSize(RegionViewModel region, double width, double height)
    {
        region.Width = Math.Max(10, width);
        region.Height = Math.Max(10, height);
    }

    /// <summary>
    /// Applies the current mode and options to the selected region.
    /// </summary>
    public void ApplyOptionsToSelectedRegion()
    {
        if (SelectedRegion != null)
        {
            SelectedRegion.Mode = SelectedMode;
            SelectedRegion.UpdateOptions(CurrentOptions);
        }
    }

    /// <summary>
    /// Gets a preview bitmap with redactions applied.
    /// </summary>
    public SKBitmap? GetPreviewBitmap()
    {
        if (!HasImage || !HasRegions)
            return null;

        var regions = Regions.Select(r => r.ToModel()).ToList();
        return _redactionService.ApplyRedactionsPreview(regions);
    }

    // ═══════════════════════════════════════════════════════════════
    // PRIVATE METHODS
    // ═══════════════════════════════════════════════════════════════

    private void UpdateImageDisplay()
    {
        if (_redactionService.CurrentImage != null)
        {
            var bitmap = _redactionService.CurrentImage;
            ImageSource = ConvertToBitmapSource(bitmap);
            ImageWidth = bitmap.Width;
            ImageHeight = bitmap.Height;
            HasImage = true;
        }
    }

    private static BitmapSource ConvertToBitmapSource(SKBitmap bitmap)
    {
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        
        using var stream = new System.IO.MemoryStream();
        data.SaveTo(stream);
        stream.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    private string GenerateExportFileName()
    {
        var originalPath = _redactionService.CurrentImagePath;
        if (string.IsNullOrEmpty(originalPath))
            return $"redacted_{DateTime.Now:yyyyMMdd_HHmmss}";

        var name = System.IO.Path.GetFileNameWithoutExtension(originalPath);
        return $"{name}_redacted";
    }

    partial void OnSelectedModeChanged(RedactionMode value)
    {
        ApplyOptionsToSelectedRegion();
    }

    partial void OnHasImageChanged(bool value)
    {
        // Notify commands that depend on HasImage
        StartAddRegionCommand.NotifyCanExecuteChanged();
        ExportImageCommand.NotifyCanExecuteChanged();
        ApplyRedactionCommand.NotifyCanExecuteChanged();
    }

    partial void OnHasRegionsChanged(bool value)
    {
        // Notify commands that depend on HasRegions
        ExportImageCommand.NotifyCanExecuteChanged();
        ApplyRedactionCommand.NotifyCanExecuteChanged();
    }

    partial void OnSelectedRegionChanged(RegionViewModel? value)
    {
        // Deselect all regions
        foreach (var region in Regions)
        {
            region.IsSelected = false;
        }

        // Select the new region
        if (value != null)
        {
            value.IsSelected = true;
            SelectedMode = value.Mode;
        }

        DeleteRegionCommand.NotifyCanExecuteChanged();
    }

    public void Dispose()
    {
        _redactionService.Dispose();
    }
}
