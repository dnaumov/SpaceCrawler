using System.Linq;

/// <summary>
/// Immutable snapshot of a cell's 4×4 organelle grid, as configured in the builder.
/// Grid indices:
///   Row 0: 0  1  2  3
///   Row 1: 4  5  6  7
///   Row 2: 8  9 10 11
///   Row 3: 12 13 14 15
/// Center four (indices 5, 6, 9, 10) are always Nucleus.
/// </summary>
public sealed class CellBlueprint
{
    /// <summary>The 16-element organelle grid (row-major, 0-indexed).</summary>
    public OrganelleType[] Grid { get; }

    /// <summary>Indices of the four nucleus slots in a 4×4 grid.</summary>
    public static readonly int[] NucleusIndices = { 5, 6, 9, 10 };

    public CellBlueprint(OrganelleType[] grid)
    {
        Grid = grid;
    }

    /// <summary>Creates a default blueprint with only the nucleus placed.</summary>
    public static CellBlueprint Default()
    {
        var grid = new OrganelleType[16];
        foreach (var idx in NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        return new CellBlueprint(grid);
    }

    /// <summary>Total occupied slots (nucleus + placed organelles).</summary>
    public int ElementCount => Grid.Count(o => o != OrganelleType.Empty);

    /// <summary>How many food items must be collected before the cell duplicates.</summary>
    public int FoodForDuplication => ElementCount - RibosomeCount;

    /// <summary>Number of Mitochondria organelles; each allows 1 extra unit of negative food.</summary>
    public int MitochondriaCount => Grid.Count(o => o == OrganelleType.Mitochondria);

    /// <summary>Minimum food before death (base -4 reduced further by Mitochondria).</summary>
    public float DeathFoodThreshold => -4f - MitochondriaCount;

    /// <summary>Number of Ribosome organelles; each reduces duplication food requirement by 1.</summary>
    public int RibosomeCount => Grid.Count(o => o == OrganelleType.Ribosome);

    /// <summary>Number of Chloroplast organelles; each produces 1 food every 40 s.</summary>
    public int ChloroplastCount => Grid.Count(o => o == OrganelleType.Chloroplast);

    /// <summary>Number of Slippery Membrane organelles; halves drag when any are present.</summary>
    public int SlipperyMembraneCount => Grid.Count(o => o == OrganelleType.SlipperyMembrane);

    /// <summary>Number of Toxin Producer organelles.</summary>
    public int ToxinProducerCount => Grid.Count(o => o == OrganelleType.ToxinProducer);

    /// <summary>Number of RandomEngine organelles.</summary>
    public int RandomEngineCount => Grid.Count(o => o == OrganelleType.RandomEngine);

    /// <summary>Number of EffectiveEngine organelles.</summary>
    public int EffectiveEngineCount => Grid.Count(o => o == OrganelleType.EffectiveEngine);

    /// <summary>Number of Engine organelles.</summary>
    public int EngineCount => Grid.Count(o => o == OrganelleType.Engine);

    /// <summary>Whether the blueprint contains any sensor organelle.</summary>
    public bool HasSensor => Grid.Any(o => o.IsSensor());

    /// <summary>Whether the blueprint contains any food gradient sensor.</summary>
    public bool HasFoodSensor =>
        Grid.Any(o => o is OrganelleType.FoodGradientDetector or OrganelleType.FoodVision);

    /// <summary>Whether the blueprint contains any cell-concentration sensor.</summary>
    public bool HasCellSensor =>
        Grid.Any(o => o == OrganelleType.CellsGradientDetector);
}
