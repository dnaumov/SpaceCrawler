/// <summary>Mutable runtime state of a single cell during simulation.</summary>
public sealed class CellState
{
    public string Name { get; set; } = string.Empty;
    public CellBlueprint Blueprint { get; set; } = CellBlueprint.Default();
    public Vec2 Position { get; set; }
    public float Rotation { get; set; }       // radians
    public Vec2 Velocity { get; set; }
    public float AngularVelocity { get; set; }

    /// <summary>
    /// When true the cell is controlled by the host application (e.g. keyboard in Godot).
    /// The simulation engine skips autonomous engine activation for this cell and instead
    /// uses <see cref="SimulationEngine.PlayerInputDirection"/>.
    /// </summary>
    public bool IsPlayer { get; set; }

    /// <summary>Current food reserve. Can go negative.</summary>
    public float Food { get; set; }

    /// <summary>Food items collected since last duplication.</summary>
    public int FoodCollectedForDup { get; set; }

    /// <summary>Total duplications this cell has triggered.</summary>
    public int DuplicationCount { get; set; }

    public bool Alive { get; set; } = true;
    public bool SensorActive { get; set; }

    // Per-organelle timers
    public float TickAccum { get; set; }
    public float ChloroAccum { get; set; }
    public float SlipperyAccum { get; set; }
    public float ToxinAccum { get; set; }

    public override string ToString() =>
        $"{Name}: food={Food:F1}, dupFood={FoodCollectedForDup}/{Blueprint.FoodForDuplication}, " +
        $"dups={DuplicationCount}, pos={Position}, alive={Alive}";
}
