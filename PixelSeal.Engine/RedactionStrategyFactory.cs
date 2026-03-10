using PixelSeal.Models;

namespace PixelSeal.Engine;

/// <summary>
/// Factory for creating redaction strategy instances.
/// </summary>
public static class RedactionStrategyFactory
{
    private static readonly Dictionary<RedactionMode, IRedactionStrategy> _strategies = new();

    static RedactionStrategyFactory()
    {
        // Register all strategies
        Register(new Strategies.SolidOverwriteStrategy());
        Register(new Strategies.SemanticPlaceholderStrategy());
        Register(new Strategies.GeometricPatternStrategy());
        Register(new Strategies.ContextAwarePanelStrategy());
        Register(new Strategies.GlassMorphStrategy());
        Register(new Strategies.AestheticBlurStrategy());
    }

    private static void Register(IRedactionStrategy strategy)
    {
        _strategies[strategy.Mode] = strategy;
    }

    /// <summary>
    /// Gets the appropriate redaction strategy for the specified mode.
    /// </summary>
    public static IRedactionStrategy GetStrategy(RedactionMode mode)
    {
        if (_strategies.TryGetValue(mode, out var strategy))
        {
            return strategy;
        }

        throw new ArgumentException($"No strategy registered for mode: {mode}", nameof(mode));
    }

    /// <summary>
    /// Gets all available redaction strategies.
    /// </summary>
    public static IEnumerable<IRedactionStrategy> GetAllStrategies()
    {
        return _strategies.Values;
    }
}
