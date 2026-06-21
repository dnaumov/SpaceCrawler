/// <summary>Immutable snapshot of a cell's 4x4 organelle grid and sensor wiring.</summary>
public sealed class CellBlueprint
{
    private readonly Dictionary<int, SensorConnection> _engineInputs;

    public OrganelleType[] Grid { get; }
    public IReadOnlyList<SensorConnection> Connections { get; }

    public static readonly int[] NucleusIndices = { 5, 6, 9, 10 };

    public CellBlueprint(
        OrganelleType[] grid,
        IEnumerable<SensorConnection>? connections = null)
    {
        if (grid.Length != 16)
        {
            throw new ArgumentException("Grid must have exactly 16 elements.", nameof(grid));
        }

        Grid = (OrganelleType[])grid.Clone();
        var connectionList = connections?.ToList() ?? [];
        _engineInputs = new Dictionary<int, SensorConnection>();

        foreach (var connection in connectionList)
        {
            if (!TryValidateConnection(Grid, connection, _engineInputs.Keys, out var error))
            {
                throw new ArgumentException(error, nameof(connections));
            }

            _engineInputs.Add(connection.EngineSlot, connection);
        }

        Connections = connectionList.AsReadOnly();
    }

    public static CellBlueprint Default()
    {
        var grid = new OrganelleType[16];
        foreach (var idx in NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        return new CellBlueprint(grid);
    }

    /// <summary>Builds a blueprint from untrusted JSON, dropping invalid edges individually.</summary>
    public static CellBlueprint FilterInvalidConnections(
        OrganelleType[] grid,
        IEnumerable<SensorConnection> connections,
        Action<string>? warn = null)
    {
        var accepted = new List<SensorConnection>();
        var occupiedEngines = new HashSet<int>();
        foreach (var connection in connections)
        {
            if (!TryValidateConnection(grid, connection, occupiedEngines, out var error))
            {
                warn?.Invoke(error);
                continue;
            }

            accepted.Add(connection);
            occupiedEngines.Add(connection.EngineSlot);
        }

        return new CellBlueprint(grid, accepted);
    }

    public static bool TryValidateConnection(
        IReadOnlyList<OrganelleType> grid,
        SensorConnection connection,
        IEnumerable<int> occupiedEngineSlots,
        out string error)
    {
        if (connection.SensorSlot is < 0 or >= 16 || connection.EngineSlot is < 0 or >= 16)
        {
            error = $"Connection slots must be between 0 and 15: {connection}.";
            return false;
        }

        if (!grid[connection.SensorSlot].IsSensor())
        {
            error = $"Connection source slot {connection.SensorSlot} is not a sensor.";
            return false;
        }

        if (!grid[connection.EngineSlot].AcceptsSensorInput())
        {
            error = $"Connection target slot {connection.EngineSlot} cannot accept sensor input.";
            return false;
        }

        if (occupiedEngineSlots.Contains(connection.EngineSlot))
        {
            error = $"Engine slot {connection.EngineSlot} already has an input.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    public bool TryGetEngineInput(int engineSlot, out SensorConnection connection) =>
        _engineInputs.TryGetValue(engineSlot, out connection);

    public int ElementCount => Grid.Count(o => o != OrganelleType.Empty);
    public int MitochondriaCount => Grid.Count(o => o == OrganelleType.Mitochondria);
    public int RibosomeCount => Grid.Count(o => o == OrganelleType.Ribosome);
    public int ChloroplastCount => Grid.Count(o => o == OrganelleType.Chloroplast);
    public int SlipperyMembraneCount => Grid.Count(o => o == OrganelleType.SlipperyMembrane);
    public int ToxinProducerCount => Grid.Count(o => o == OrganelleType.ToxinProducer);

    public bool HasSensor => Grid.Any(o => o.IsSensor());

    public string Describe()
    {
        var parts = Grid
            .Where(o => o != OrganelleType.Empty)
            .GroupBy(o => o)
            .Select(g => g.Count() == 1 ? g.Key.DisplayName() : $"{g.Count()}x{g.Key.DisplayName()}");
        return string.Join(", ", parts);
    }
}
