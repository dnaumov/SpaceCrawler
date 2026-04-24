/// <summary>Environment types that affect cell behaviour.</summary>
public enum EnvironmentType
{
    Normal,

    /// <summary>2× increased drag. Active when more than half the cell is inside.</summary>
    Viscous,

    /// <summary>2× increased passive food drain. Active when more than half the cell is inside.</summary>
    Toxic,

    /// <summary>2× random cell movement/rotation. Active when more than half the cell is inside.</summary>
    Turbulent,

    /// <summary>2× food generation (food items collected yield double).</summary>
    Nutritious
}

/// <summary>A rectangular environment zone in the simulation arena.</summary>
public sealed class EnvironmentZone
{
    public float X { get; init; }
    public float Y { get; init; }
    public float W { get; init; }
    public float H { get; init; }
    public EnvironmentType Type { get; init; }

    public bool Contains(Vec2 pos) =>
        pos.X >= X && pos.X <= X + W && pos.Y >= Y && pos.Y <= Y + H;
}
