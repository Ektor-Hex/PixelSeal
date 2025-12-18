using CommunityToolkit.Mvvm.ComponentModel;
using PixelSeal.Models;

namespace PixelSeal.UI.ViewModels;

/// <summary>
/// ViewModel wrapper for RedactionRegion.
/// Provides observable properties for WPF binding.
/// </summary>
public partial class RegionViewModel : ObservableObject
{
    private readonly RedactionRegion _model;

    public RegionViewModel(RedactionRegion model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public Guid Id => _model.Id;

    [ObservableProperty]
    private bool _isSelected;

    public double X
    {
        get => _model.X;
        set
        {
            if (_model.X != (float)value)
            {
                _model.X = (float)value;
                OnPropertyChanged();
            }
        }
    }

    public double Y
    {
        get => _model.Y;
        set
        {
            if (_model.Y != (float)value)
            {
                _model.Y = (float)value;
                OnPropertyChanged();
            }
        }
    }

    public double Width
    {
        get => _model.Width;
        set
        {
            if (_model.Width != (float)value)
            {
                _model.Width = (float)value;
                OnPropertyChanged();
            }
        }
    }

    public double Height
    {
        get => _model.Height;
        set
        {
            if (_model.Height != (float)value)
            {
                _model.Height = (float)value;
                OnPropertyChanged();
            }
        }
    }

    public RedactionMode Mode
    {
        get => _model.Mode;
        set
        {
            if (_model.Mode != value)
            {
                _model.Mode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Description));
            }
        }
    }

    public RegionShape Shape
    {
        get => _model.Shape;
        set
        {
            if (_model.Shape != value)
            {
                _model.Shape = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(Description));
            }
        }
    }

    public double BrushSize
    {
        get => _model.BrushSize;
        set
        {
            if (_model.BrushSize != (float)value)
            {
                _model.BrushSize = (float)value;
                OnPropertyChanged();
            }
        }
    }

    public string DisplayName
    {
        get => _model.DisplayName;
        set
        {
            if (_model.DisplayName != value)
            {
                _model.DisplayName = value;
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// Gets a display-friendly description of the region.
    /// </summary>
    public string Description
    {
        get
        {
            string shapeIcon = Shape switch
            {
                RegionShape.Ellipse => "⬭",
                RegionShape.FreeForm => "✏️",
                _ => "▭"
            };
            return $"{shapeIcon} {DisplayName} ({Mode})";
        }
    }

    /// <summary>
    /// Updates the options from a source.
    /// </summary>
    public void UpdateOptions(RedactionOptions options)
    {
        _model.Options = options.Clone();
    }

    /// <summary>
    /// Gets the underlying model.
    /// </summary>
    public RedactionRegion ToModel() => _model;
}
