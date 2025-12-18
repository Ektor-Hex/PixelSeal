using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using PixelSeal.Models;
using PixelSeal.UI.ViewModels;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace PixelSeal.UI.Controls;

/// <summary>
/// Custom control for displaying and editing the image with redaction regions.
/// Handles region creation, selection, movement, and resizing.
/// </summary>
public partial class ImageCanvas : UserControl
{
    private MainViewModel? _viewModel;
    private SKBitmap? _currentBitmap;
    private SKBitmap? _previewBitmap;
    
    // Zoom and transform
    private double _zoomLevel = 1.0;
    private const double MinZoom = 0.1;
    private const double MaxZoom = 5.0;
    private const double ZoomStep = 0.1;

    // Drawing state
    private bool _isDrawing;
    private Point _drawStart;
    private Rectangle? _drawingRect;
    private Ellipse? _drawingEllipse;
    private Polyline? _drawingPath;
    private List<Point> _freeFormPoints = new();

    // Editing state
    private bool _isDragging;
    private bool _isResizing;
    private Point _dragStart;
    private RegionViewModel? _activeRegion;
    private double _originalX, _originalY, _originalWidth, _originalHeight;
    private ResizeHandle _activeHandle;

    // Region visuals
    private readonly Dictionary<Guid, RegionOverlay> _regionOverlays = new();

    // Handle size
    private const double HandleSize = 8;

    public ImageCanvas()
    {
        InitializeComponent();
        
        DataContextChanged += OnDataContextChanged;
        Loaded += OnLoaded;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (_viewModel != null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _viewModel.Regions.CollectionChanged -= OnRegionsChanged;
        }

        _viewModel = e.NewValue as MainViewModel;

        if (_viewModel != null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _viewModel.Regions.CollectionChanged += OnRegionsChanged;
        }
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        UpdateDisplay();
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(MainViewModel.ImageSource):
            case nameof(MainViewModel.HasImage):
                Dispatcher.Invoke(UpdateDisplay);
                Dispatcher.Invoke(UpdateModeIndicator);
                break;
            case nameof(MainViewModel.IsAddingRegion):
            case nameof(MainViewModel.HasRegions):
                Dispatcher.Invoke(UpdateModeIndicator);
                break;
            case nameof(MainViewModel.IsPreviewMode):
                Dispatcher.Invoke(UpdatePreview);
                Dispatcher.Invoke(UpdateModeIndicator);
                break;
            case nameof(MainViewModel.SelectedRegion):
                Dispatcher.Invoke(UpdateRegionSelection);
                break;
        }
    }

    private void OnRegionsChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        Dispatcher.Invoke(RebuildRegionOverlays);
        Dispatcher.Invoke(UpdateModeIndicator);
    }

    private void UpdateDisplay()
    {
        if (_viewModel?.CurrentBitmap != null)
        {
            _currentBitmap = _viewModel.CurrentBitmap;
            UpdateCanvasSize();
            SkiaElement.InvalidateVisual();
        }
    }

    private void UpdateCanvasSize()
    {
        if (_currentBitmap != null)
        {
            double scaledWidth = _currentBitmap.Width * _zoomLevel;
            double scaledHeight = _currentBitmap.Height * _zoomLevel;

            SkiaElement.Width = scaledWidth;
            SkiaElement.Height = scaledHeight;
            OverlayCanvas.Width = scaledWidth;
            OverlayCanvas.Height = scaledHeight;
            CanvasContainer.Width = scaledWidth;
            CanvasContainer.Height = scaledHeight;

            ZoomText.Text = $"{(int)(_zoomLevel * 100)}%";
        }
    }

    private void UpdateModeIndicator()
    {
        // Don't show indicator if no image or in preview mode
        if (_viewModel?.HasImage != true || _viewModel?.IsPreviewMode == true)
        {
            ModeIndicator.Visibility = Visibility.Collapsed;
            Cursor = Cursors.Arrow;
            return;
        }

        if (_viewModel?.IsAddingRegion == true)
        {
            ModeIndicator.Visibility = Visibility.Visible;
            ModeIndicatorText.Text = "🎯 Click and drag to create region";
            Cursor = Cursors.Cross;
        }
        else if (_viewModel?.HasRegions == false)
        {
            // No regions yet - show hint that user can draw directly
            ModeIndicator.Visibility = Visibility.Visible;
            ModeIndicatorText.Text = "🎯 Click and drag to select area to redact";
            Cursor = Cursors.Cross;
        }
        else
        {
            // Has regions - show Shift hint
            ModeIndicator.Visibility = Visibility.Visible;
            ModeIndicatorText.Text = "💡 Hold Shift + drag to add new region";
            Cursor = Cursors.Arrow;
        }
    }

    private void UpdatePreview()
    {
        if (_viewModel?.IsPreviewMode == true)
        {
            _previewBitmap?.Dispose();
            _previewBitmap = _viewModel.GetPreviewBitmap();
            
            // Hide region overlays in preview mode
            foreach (var overlay in _regionOverlays.Values)
            {
                overlay.Container.Visibility = Visibility.Collapsed;
            }
        }
        else
        {
            _previewBitmap?.Dispose();
            _previewBitmap = null;
            
            // Show region overlays
            foreach (var overlay in _regionOverlays.Values)
            {
                overlay.Container.Visibility = Visibility.Visible;
            }
        }
        
        SkiaElement.InvalidateVisual();
    }

    private void UpdateRegionSelection()
    {
        foreach (var kvp in _regionOverlays)
        {
            var region = _viewModel?.Regions.FirstOrDefault(r => r.Id == kvp.Key);
            kvp.Value.SetSelected(region?.IsSelected == true);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // SKIA RENDERING
    // ═══════════════════════════════════════════════════════════════

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        var bitmapToRender = _previewBitmap ?? _currentBitmap;
        
        if (bitmapToRender != null)
        {
            // Scale to fit the element
            float scaleX = e.Info.Width / (float)bitmapToRender.Width;
            float scaleY = e.Info.Height / (float)bitmapToRender.Height;
            
            canvas.Save();
            canvas.Scale(scaleX, scaleY);
            canvas.DrawBitmap(bitmapToRender, 0, 0);
            canvas.Restore();
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // REGION OVERLAY MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    private void RebuildRegionOverlays()
    {
        // Remove old overlays
        foreach (var overlay in _regionOverlays.Values)
        {
            OverlayCanvas.Children.Remove(overlay.Container);
        }
        _regionOverlays.Clear();

        if (_viewModel == null) return;

        // Create overlays for each region
        foreach (var region in _viewModel.Regions)
        {
            var overlay = CreateRegionOverlay(region);
            _regionOverlays[region.Id] = overlay;
            OverlayCanvas.Children.Add(overlay.Container);
            UpdateOverlayPosition(overlay, region);
        }
    }

    private RegionOverlay CreateRegionOverlay(RegionViewModel region)
    {
        var container = new Grid();

        // Background fill based on shape
        Shape fill;
        if (region.Shape == RegionShape.Ellipse)
        {
            fill = new Ellipse
            {
                Fill = new SolidColorBrush(Color.FromArgb(48, 233, 69, 96)),
                Stroke = new SolidColorBrush(Color.FromRgb(233, 69, 96)),
                StrokeThickness = 2
            };
        }
        else if (region.Shape == RegionShape.FreeForm)
        {
            // For freeform, use a Rectangle as container but with dashed border
            fill = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(24, 233, 69, 96)),
                Stroke = new SolidColorBrush(Color.FromRgb(233, 69, 96)),
                StrokeThickness = 2,
                StrokeDashArray = new DoubleCollection { 3, 2 },
                RadiusX = 4,
                RadiusY = 4
            };
        }
        else
        {
            fill = new Rectangle
            {
                Fill = new SolidColorBrush(Color.FromArgb(48, 233, 69, 96)),
                Stroke = new SolidColorBrush(Color.FromRgb(233, 69, 96)),
                StrokeThickness = 2,
                RadiusX = 2,
                RadiusY = 2
            };
        }
        container.Children.Add(fill);

        // Resize handles (not for freeform)
        var handles = new Dictionary<ResizeHandle, Rectangle>();
        if (region.Shape != RegionShape.FreeForm)
        {
            foreach (ResizeHandle handle in Enum.GetValues<ResizeHandle>())
            {
                var handleRect = new Rectangle
                {
                    Width = HandleSize,
                    Height = HandleSize,
                    Fill = new SolidColorBrush(Colors.White),
                    Stroke = new SolidColorBrush(Color.FromRgb(233, 69, 96)),
                    StrokeThickness = 1,
                    Visibility = Visibility.Collapsed,
                    Tag = handle
                };
                
                handleRect.MouseLeftButtonDown += OnHandleMouseDown;
                handles[handle] = handleRect;
                container.Children.Add(handleRect);
            }
        }

        // Mode label
        var label = new TextBlock
        {
            Text = GetModeLabel(region.Mode),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromArgb(180, 0, 0, 0)),
            Padding = new Thickness(4, 2, 4, 2),
            FontSize = 10,
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(2)
        };
        container.Children.Add(label);

        container.MouseLeftButtonDown += (s, e) => OnRegionClick(region, e);
        container.MouseEnter += (s, e) => Cursor = Cursors.SizeAll;
        container.MouseLeave += (s, e) => Cursor = _viewModel?.IsAddingRegion == true ? Cursors.Cross : Cursors.Arrow;

        return new RegionOverlay(container, fill, handles, label);
    }

    private void UpdateOverlayPosition(RegionOverlay overlay, RegionViewModel region)
    {
        double x = region.X * _zoomLevel;
        double y = region.Y * _zoomLevel;
        double width = region.Width * _zoomLevel;
        double height = region.Height * _zoomLevel;

        Canvas.SetLeft(overlay.Container, x);
        Canvas.SetTop(overlay.Container, y);
        overlay.Container.Width = width;
        overlay.Container.Height = height;

        // Update label
        overlay.Label.Text = GetModeLabel(region.Mode);

        // Position handles (only if they exist - not for FreeForm regions)
        if (overlay.Handles.Count > 0)
        {
            PositionHandle(overlay.Handles[ResizeHandle.TopLeft], 0, 0);
            PositionHandle(overlay.Handles[ResizeHandle.TopRight], width - HandleSize, 0);
            PositionHandle(overlay.Handles[ResizeHandle.BottomLeft], 0, height - HandleSize);
            PositionHandle(overlay.Handles[ResizeHandle.BottomRight], width - HandleSize, height - HandleSize);
            PositionHandle(overlay.Handles[ResizeHandle.Top], (width - HandleSize) / 2, 0);
            PositionHandle(overlay.Handles[ResizeHandle.Bottom], (width - HandleSize) / 2, height - HandleSize);
            PositionHandle(overlay.Handles[ResizeHandle.Left], 0, (height - HandleSize) / 2);
            PositionHandle(overlay.Handles[ResizeHandle.Right], width - HandleSize, (height - HandleSize) / 2);
        }
    }

    private static void PositionHandle(Rectangle handle, double x, double y)
    {
        Canvas.SetLeft(handle, x);
        Canvas.SetTop(handle, y);
    }

    private static string GetModeLabel(PixelSeal.Models.RedactionMode mode)
    {
        return mode switch
        {
            PixelSeal.Models.RedactionMode.SolidOverwrite => "■ Solid",
            PixelSeal.Models.RedactionMode.SemanticPlaceholder => "📝 Placeholder",
            PixelSeal.Models.RedactionMode.GeometricPattern => "⬡ Pattern",
            PixelSeal.Models.RedactionMode.ContextAwarePanel => "🏷️ Panel",
            PixelSeal.Models.RedactionMode.GlassMorph => "✨ Glass",
            _ => "?"
        };
    }

    // ═══════════════════════════════════════════════════════════════
    // MOUSE HANDLERS - DRAWING
    // ═══════════════════════════════════════════════════════════════

    private void OnCanvasMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel == null || _viewModel.IsPreviewMode) return;

        var pos = e.GetPosition(OverlayCanvas);
        bool shiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);

        // Determine if we should start drawing a new region:
        // 1. If "Add Region" mode is active (button was clicked)
        // 2. If there are no regions yet (direct draw)
        // 3. If Shift is held (force new region even with existing regions)
        bool shouldStartDrawing = _viewModel.IsAddingRegion || 
                                   !_viewModel.HasRegions || 
                                   shiftPressed;

        if (shouldStartDrawing)
        {
            // Check if clicking on an existing region first (unless Shift is held)
            if (!shiftPressed && _viewModel.HasRegions)
            {
                var clickedRegion = GetRegionAtPoint(pos);
                if (clickedRegion != null)
                {
                    // Clicked on existing region - select it instead of drawing
                    _viewModel.SelectedRegion = clickedRegion;
                    StartDragging(clickedRegion, pos);
                    e.Handled = true;
                    return;
                }
            }
            
            StartDrawing(pos);
            e.Handled = true;
        }
    }

    /// <summary>
    /// Gets the region at the specified canvas point, if any.
    /// </summary>
    private RegionViewModel? GetRegionAtPoint(Point canvasPoint)
    {
        if (_viewModel == null) return null;

        // Convert canvas point to image coordinates
        double imageX = canvasPoint.X / _zoomLevel;
        double imageY = canvasPoint.Y / _zoomLevel;

        // Check regions in reverse order (top-most first)
        for (int i = _viewModel.Regions.Count - 1; i >= 0; i--)
        {
            var region = _viewModel.Regions[i];
            if (imageX >= region.X && imageX <= region.X + region.Width &&
                imageY >= region.Y && imageY <= region.Y + region.Height)
            {
                return region;
            }
        }
        return null;
    }

    /// <summary>
    /// Starts dragging a region.
    /// </summary>
    private void StartDragging(RegionViewModel region, Point pos)
    {
        _activeRegion = region;
        _isDragging = true;
        _dragStart = pos;
        _originalX = region.X;
        _originalY = region.Y;
        Mouse.Capture(OverlayCanvas);
    }

    private void OnCanvasMouseMove(object sender, MouseEventArgs e)
    {
        if (_viewModel == null) return;

        var pos = e.GetPosition(OverlayCanvas);

        if (_isDrawing)
        {
            UpdateDrawingRect(pos);
        }
        else if (_isDragging && _activeRegion != null)
        {
            UpdateDragging(pos);
        }
        else if (_isResizing && _activeRegion != null)
        {
            UpdateResizing(pos);
        }
    }

    private void OnCanvasMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (_isDrawing)
        {
            FinishDrawing();
        }
        else if (_isDragging || _isResizing)
        {
            _isDragging = false;
            _isResizing = false;
            _activeRegion = null;
            Mouse.Capture(null);
        }
    }

    private void OnCanvasMouseLeave(object sender, MouseEventArgs e)
    {
        // Don't cancel operations on leave - they're captured
    }

    private void StartDrawing(Point pos)
    {
        _isDrawing = true;
        _drawStart = pos;

        var selectedShape = _viewModel?.SelectedShape ?? RegionShape.Rectangle;
        var strokeBrush = new SolidColorBrush(Color.FromRgb(233, 69, 96));
        var fillBrush = new SolidColorBrush(Color.FromArgb(48, 233, 69, 96));

        switch (selectedShape)
        {
            case RegionShape.Ellipse:
                _drawingEllipse = new Ellipse
                {
                    Stroke = strokeBrush,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = fillBrush
                };
                Canvas.SetLeft(_drawingEllipse, pos.X);
                Canvas.SetTop(_drawingEllipse, pos.Y);
                _drawingEllipse.Width = 0;
                _drawingEllipse.Height = 0;
                OverlayCanvas.Children.Add(_drawingEllipse);
                break;

            case RegionShape.FreeForm:
                _freeFormPoints = new List<Point> { pos };
                _drawingPath = new Polyline
                {
                    Stroke = strokeBrush,
                    StrokeThickness = _viewModel?.BrushSize ?? 20,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap = PenLineCap.Round,
                    Opacity = 0.6
                };
                _drawingPath.Points.Add(pos);
                OverlayCanvas.Children.Add(_drawingPath);
                break;

            case RegionShape.Rectangle:
            default:
                _drawingRect = new Rectangle
                {
                    Stroke = strokeBrush,
                    StrokeThickness = 2,
                    StrokeDashArray = new DoubleCollection { 4, 2 },
                    Fill = fillBrush
                };
                Canvas.SetLeft(_drawingRect, pos.X);
                Canvas.SetTop(_drawingRect, pos.Y);
                _drawingRect.Width = 0;
                _drawingRect.Height = 0;
                OverlayCanvas.Children.Add(_drawingRect);
                break;
        }

        Mouse.Capture(OverlayCanvas);
    }

    private void UpdateDrawingRect(Point pos)
    {
        var selectedShape = _viewModel?.SelectedShape ?? RegionShape.Rectangle;

        switch (selectedShape)
        {
            case RegionShape.Ellipse:
                if (_drawingEllipse == null) return;
                double ex = Math.Min(_drawStart.X, pos.X);
                double ey = Math.Min(_drawStart.Y, pos.Y);
                double ewidth = Math.Abs(pos.X - _drawStart.X);
                double eheight = Math.Abs(pos.Y - _drawStart.Y);
                Canvas.SetLeft(_drawingEllipse, ex);
                Canvas.SetTop(_drawingEllipse, ey);
                _drawingEllipse.Width = ewidth;
                _drawingEllipse.Height = eheight;
                break;

            case RegionShape.FreeForm:
                if (_drawingPath == null) return;
                _freeFormPoints.Add(pos);
                _drawingPath.Points.Add(pos);
                break;

            case RegionShape.Rectangle:
            default:
                if (_drawingRect == null) return;
                double x = Math.Min(_drawStart.X, pos.X);
                double y = Math.Min(_drawStart.Y, pos.Y);
                double width = Math.Abs(pos.X - _drawStart.X);
                double height = Math.Abs(pos.Y - _drawStart.Y);
                Canvas.SetLeft(_drawingRect, x);
                Canvas.SetTop(_drawingRect, y);
                _drawingRect.Width = width;
                _drawingRect.Height = height;
                break;
        }
    }

    private void FinishDrawing()
    {
        _isDrawing = false;
        Mouse.Capture(null);

        if (_viewModel == null) return;

        var selectedShape = _viewModel.SelectedShape;

        switch (selectedShape)
        {
            case RegionShape.Ellipse:
                if (_drawingEllipse != null)
                {
                    double ex = Canvas.GetLeft(_drawingEllipse) / _zoomLevel;
                    double ey = Canvas.GetTop(_drawingEllipse) / _zoomLevel;
                    double ewidth = _drawingEllipse.Width / _zoomLevel;
                    double eheight = _drawingEllipse.Height / _zoomLevel;

                    OverlayCanvas.Children.Remove(_drawingEllipse);
                    _drawingEllipse = null;

                    if (ewidth > 5 && eheight > 5)
                    {
                        _viewModel.CreateRegion(new Rect(ex, ey, ewidth, eheight));
                    }
                    else if (_viewModel.IsAddingRegion)
                    {
                        _viewModel.CancelAddRegionCommand.Execute(null);
                    }
                }
                break;

            case RegionShape.FreeForm:
                {
                    // Always clean up the path visual
                    if (_drawingPath != null)
                    {
                        OverlayCanvas.Children.Remove(_drawingPath);
                        _drawingPath = null;
                    }

                    // Convert points to image coordinates
                    if (_freeFormPoints.Count >= 2)
                    {
                        var imagePoints = _freeFormPoints
                            .Select(p => new Point(p.X / _zoomLevel, p.Y / _zoomLevel))
                            .ToList();

                        _viewModel.CreateFreeFormRegion(imagePoints);
                    }
                    else if (_viewModel.IsAddingRegion)
                    {
                        _viewModel.CancelAddRegionCommand.Execute(null);
                    }

                    // Always clear the points list
                    _freeFormPoints = new List<Point>();
                }
                break;

            case RegionShape.Rectangle:
            default:
                if (_drawingRect != null)
                {
                    double x = Canvas.GetLeft(_drawingRect) / _zoomLevel;
                    double y = Canvas.GetTop(_drawingRect) / _zoomLevel;
                    double width = _drawingRect.Width / _zoomLevel;
                    double height = _drawingRect.Height / _zoomLevel;

                    OverlayCanvas.Children.Remove(_drawingRect);
                    _drawingRect = null;

                    if (width > 5 && height > 5)
                    {
                        _viewModel.CreateRegion(new Rect(x, y, width, height));
                    }
                    else if (_viewModel.IsAddingRegion)
                    {
                        _viewModel.CancelAddRegionCommand.Execute(null);
                    }
                }
                break;
        }

        // Ensure all drawing state is cleaned up
        CleanupDrawingState();
    }

    private void CleanupDrawingState()
    {
        // Clean up any leftover drawing elements
        if (_drawingRect != null)
        {
            OverlayCanvas.Children.Remove(_drawingRect);
            _drawingRect = null;
        }
        if (_drawingEllipse != null)
        {
            OverlayCanvas.Children.Remove(_drawingEllipse);
            _drawingEllipse = null;
        }
        if (_drawingPath != null)
        {
            OverlayCanvas.Children.Remove(_drawingPath);
            _drawingPath = null;
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // MOUSE HANDLERS - SELECTION & DRAGGING
    // ═══════════════════════════════════════════════════════════════

    private void OnRegionClick(RegionViewModel region, MouseButtonEventArgs e)
    {
        if (_viewModel == null || _viewModel.IsAddingRegion || _viewModel.IsPreviewMode) 
            return;

        _viewModel.SelectedRegion = region;

        // Start dragging
        _activeRegion = region;
        _isDragging = true;
        _dragStart = e.GetPosition(OverlayCanvas);
        _originalX = region.X;
        _originalY = region.Y;

        Mouse.Capture(OverlayCanvas);
        e.Handled = true;
    }

    private void OnHandleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (_viewModel?.SelectedRegion == null || _viewModel.IsPreviewMode) 
            return;

        if (sender is Rectangle rect && rect.Tag is ResizeHandle handle)
        {
            _activeRegion = _viewModel.SelectedRegion;
            _isResizing = true;
            _activeHandle = handle;
            _dragStart = e.GetPosition(OverlayCanvas);
            _originalX = _activeRegion.X;
            _originalY = _activeRegion.Y;
            _originalWidth = _activeRegion.Width;
            _originalHeight = _activeRegion.Height;

            Mouse.Capture(OverlayCanvas);
            e.Handled = true;
        }
    }

    private void UpdateDragging(Point pos)
    {
        if (_activeRegion == null) return;

        double deltaX = (pos.X - _dragStart.X) / _zoomLevel;
        double deltaY = (pos.Y - _dragStart.Y) / _zoomLevel;

        _activeRegion.X = Math.Max(0, _originalX + deltaX);
        _activeRegion.Y = Math.Max(0, _originalY + deltaY);

        // Clamp to image bounds
        if (_currentBitmap != null)
        {
            _activeRegion.X = Math.Min(_activeRegion.X, _currentBitmap.Width - _activeRegion.Width);
            _activeRegion.Y = Math.Min(_activeRegion.Y, _currentBitmap.Height - _activeRegion.Height);
        }

        UpdateRegionOverlay(_activeRegion);
    }

    private void UpdateResizing(Point pos)
    {
        if (_activeRegion == null) return;

        double deltaX = (pos.X - _dragStart.X) / _zoomLevel;
        double deltaY = (pos.Y - _dragStart.Y) / _zoomLevel;

        double newX = _originalX;
        double newY = _originalY;
        double newWidth = _originalWidth;
        double newHeight = _originalHeight;

        switch (_activeHandle)
        {
            case ResizeHandle.TopLeft:
                newX = _originalX + deltaX;
                newY = _originalY + deltaY;
                newWidth = _originalWidth - deltaX;
                newHeight = _originalHeight - deltaY;
                break;
            case ResizeHandle.TopRight:
                newY = _originalY + deltaY;
                newWidth = _originalWidth + deltaX;
                newHeight = _originalHeight - deltaY;
                break;
            case ResizeHandle.BottomLeft:
                newX = _originalX + deltaX;
                newWidth = _originalWidth - deltaX;
                newHeight = _originalHeight + deltaY;
                break;
            case ResizeHandle.BottomRight:
                newWidth = _originalWidth + deltaX;
                newHeight = _originalHeight + deltaY;
                break;
            case ResizeHandle.Top:
                newY = _originalY + deltaY;
                newHeight = _originalHeight - deltaY;
                break;
            case ResizeHandle.Bottom:
                newHeight = _originalHeight + deltaY;
                break;
            case ResizeHandle.Left:
                newX = _originalX + deltaX;
                newWidth = _originalWidth - deltaX;
                break;
            case ResizeHandle.Right:
                newWidth = _originalWidth + deltaX;
                break;
        }

        // Apply minimum size
        if (newWidth >= 10 && newHeight >= 10)
        {
            _activeRegion.X = Math.Max(0, newX);
            _activeRegion.Y = Math.Max(0, newY);
            _activeRegion.Width = newWidth;
            _activeRegion.Height = newHeight;
            UpdateRegionOverlay(_activeRegion);
        }
    }

    private void UpdateRegionOverlay(RegionViewModel region)
    {
        if (_regionOverlays.TryGetValue(region.Id, out var overlay))
        {
            UpdateOverlayPosition(overlay, region);
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // ZOOM CONTROLS
    // ═══════════════════════════════════════════════════════════════

    private void OnZoomIn(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomLevel + ZoomStep);
    }

    private void OnZoomOut(object sender, RoutedEventArgs e)
    {
        SetZoom(_zoomLevel - ZoomStep);
    }

    private void OnZoomReset(object sender, RoutedEventArgs e)
    {
        SetZoom(1.0);
    }

    private void SetZoom(double newZoom)
    {
        _zoomLevel = Math.Clamp(newZoom, MinZoom, MaxZoom);
        UpdateCanvasSize();
        RebuildRegionOverlays();
        SkiaElement.InvalidateVisual();
    }

    // ═══════════════════════════════════════════════════════════════
    // HELPER TYPES
    // ═══════════════════════════════════════════════════════════════

    private enum ResizeHandle
    {
        TopLeft, Top, TopRight,
        Left, Right,
        BottomLeft, Bottom, BottomRight
    }

    private class RegionOverlay
    {
        public Grid Container { get; }
        public Shape Fill { get; }
        public Dictionary<ResizeHandle, Rectangle> Handles { get; }
        public TextBlock Label { get; }

        public RegionOverlay(Grid container, Shape fill, Dictionary<ResizeHandle, Rectangle> handles, TextBlock label)
        {
            Container = container;
            Fill = fill;
            Handles = handles;
            Label = label;
        }

        public void SetSelected(bool selected)
        {
            foreach (var handle in Handles.Values)
            {
                handle.Visibility = selected ? Visibility.Visible : Visibility.Collapsed;
            }

            Fill.StrokeThickness = selected ? 3 : 2;
        }
    }
}
