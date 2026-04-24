/// <summary>
/// Immutable snapshot of a cell's 4×4 organelle grid.
/// Grid layout (0-indexed row-major):
///   Row 0:  0  1  2  3
///   Row 1:  4  5  6  7
///   Row 2:  8  9 10 11
///   Row 3: 12 13 14 15
/// Center four indices (5, 6, 9, 10) are always Nucleus.
/// </summary>
public sealed class CellBlueprint
{
    public OrganelleType[] Grid { get; }

    public static readonly int[] NucleusIndices = { 5, 6, 9, 10 };

    public CellBlueprint(OrganelleType[] grid)
    {
        if (grid.Length != 16)
        {
            throw new ArgumentException("Grid must have exactly 16 elements.", nameof(grid));
        }

        Grid = grid;
    }

    /// <summary>Default blueprint: only the four nucleus slots filled.</summary>
    public static CellBlueprint Default()
    {
        var grid = new OrganelleType[16];
        foreach (var idx in NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        return new CellBlueprint(grid);
    }

    public int ElementCount => Grid.Count(o => o != OrganelleType.Empty);

    /// <summary>Minimum food items to collect before the cell duplicates (adjusted by Ribosome).</summary>
    public int FoodForDuplication => Math.Max(1, ElementCount - RibosomeCount);

    /// <summary>Each Mitochondria extends the negative-food survival limit by 1.</summary>
    public int MitochondriaCount => Grid.Count(o => o == OrganelleType.Mitochondria);

    public float DeathFoodThreshold => SimConstants.NegativeFoodBase - MitochondriaCount;

    public int RibosomeCount  => Grid.Count(o => o == OrganelleType.Ribosome);
    public int ChloroplastCount     => Grid.Count(o => o == OrganelleType.Chloroplast);
    public int SlipperyMembraneCount => Grid.Count(o => o == OrganelleType.SlipperyMembrane);
    public int ToxinProducerCount   => Grid.Count(o => o == OrganelleType.ToxinProducer);
    public int RandomEngineCount    => Grid.Count(o => o == OrganelleType.RandomEngine);
    public int EffectiveEngineCount => Grid.Count(o => o == OrganelleType.EffectiveEngine);
    public int EngineCount          => Grid.Count(o => o == OrganelleType.Engine);

    public bool HasFoodSensor =>
        Grid.Any(o => o is OrganelleType.FoodGradientDetector or OrganelleType.FoodVision);

    public bool HasCellSensor =>
        Grid.Any(o => o == OrganelleType.CellsGradientDetector);

    public bool HasToxicSensor =>
        Grid.Any(o => o == OrganelleType.ToxicGradientDetector);

    public bool HasSensor => Grid.Any(o => o.IsSensor());

    public string Describe()
    {
        var parts = Grid
            .Where(o => o != OrganelleType.Empty)
            .GroupBy(o => o)
            .Select(g => g.Count() == 1 ? g.Key.DisplayName() : $"{g.Count()}×{g.Key.DisplayName()}");
        return string.Join(", ", parts);
    }
}
