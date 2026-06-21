/// <summary>
/// Structural simulation constants. Runtime balance values belong in
/// project/balance and are exposed through <see cref="SimulationBalance"/>.
/// </summary>
public static class SimConstants
{
    /// <summary>Number of deterministic simulation updates processed per second.</summary>
    public const int SimulationStepsPerSecond = 60;

    /// <summary>Duration of one deterministic simulation update.</summary>
    public const float FixedTimeStep = 1f / SimulationStepsPerSecond;

    /// <summary>Base size unit [S]. Arena and cell sizes are multiples of S.</summary>
    public const float S = 1f;

    /// <summary>Cell half-size: cell is 2S x 2S.</summary>
    public const float CellHalfSize = S;

    /// <summary>Food half-size: food is 1S x 1S.</summary>
    public const float FoodHalfSize = S * 0.5f;

    /// <summary>Default arena width in S units.</summary>
    public const float ArenaWidth = 72f;

    /// <summary>Default arena height in S units.</summary>
    public const float ArenaHeight = 40f;
}
