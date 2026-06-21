/// <summary>Mutable runtime state of a single cell during simulation.</summary>
public sealed class CellState
{
    public string Name { get; set; } = string.Empty;
    public CellBlueprint Blueprint { get; set; } = CellBlueprint.Default();
    public Vec2 Position { get; set; }
    public float Rotation { get; set; }       // radians
    public Vec2 Velocity { get; set; }
    public float AngularVelocity { get; set; }

    /// <summary>Current food reserve. Can go negative.</summary>
    public float Food { get; set; }

    public float BiomassThreshold { get; set; }
    public float DeathFoodThreshold { get; set; }

    /// <summary>Total duplications this cell has triggered.</summary>
    public int DuplicationCount { get; set; }

    public bool Alive { get; set; } = true;
    /// <summary>Most recently evaluated output for each grid slot.</summary>
    public bool[] SensorOutputs { get; } = new bool[16];

    // Per-organelle timers
    public float TickAccum { get; set; }
    public float ChloroAccum { get; set; }
    public float SlipperyAccum { get; set; }
    public float ToxinAccum { get; set; }
    public float EngineAccum { get; set; }

    public override string ToString() =>
        $"{Name}: biomass={Food:F1}/{BiomassThreshold:F1}, " +
        $"dups={DuplicationCount}, pos={Position}, alive={Alive}";
}
