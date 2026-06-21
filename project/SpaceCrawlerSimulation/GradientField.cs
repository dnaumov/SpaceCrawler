/// <summary>
/// Calculates a concentration-gradient direction at a requested position.
/// No arena-wide grid is cached: callers pay only for sources needed by an
/// active sensor evaluation.
/// </summary>
public static class GradientField
{
    /// <summary>
    /// Returns the direction in which concentration increases for sources whose
    /// concentration contribution is 1 / (distance squared + softening squared).
    /// </summary>
    public static Vec2 DirectionAt(
        Vec2 position,
        IEnumerable<Vec2> sources,
        float softeningRadius)
    {
        var direction = Vec2.Zero;
        var softeningSq = softeningRadius * softeningRadius;

        foreach (var source in sources)
        {
            var delta = source - position;
            var denominator = delta.LengthSq + softeningSq;
            direction += delta * (1f / (denominator * denominator));
        }

        return direction.Normalized();
    }
}
