using Xunit;

public sealed class SensorConnectionTests
{
    [Fact]
    public void SensorMayFanOutToMultipleEngines()
    {
        var grid = GridWith(
            (1, OrganelleType.FoodGradientDetector),
            (2, OrganelleType.EffectiveEngine),
            (3, OrganelleType.Engine));

        var blueprint = new CellBlueprint(grid,
        [
            new SensorConnection(1, 2, false),
            new SensorConnection(1, 3, true)
        ]);

        Assert.Equal(2, blueprint.Connections.Count);
        Assert.False(blueprint.Connections[0].Inverted);
        Assert.True(blueprint.Connections[1].Inverted);
    }

    [Fact]
    public void EngineRejectsMultipleInputs()
    {
        var grid = GridWith(
            (0, OrganelleType.FoodVision),
            (1, OrganelleType.FoodGradientDetector),
            (2, OrganelleType.EffectiveEngine));

        Assert.Throws<ArgumentException>(() => new CellBlueprint(grid,
        [
            new SensorConnection(0, 2),
            new SensorConnection(1, 2)
        ]));
    }

    [Fact]
    public void ToxinProducerAcceptsOneSensorInput()
    {
        var grid = GridWith(
            (1, OrganelleType.ToxicGradientDetector),
            (2, OrganelleType.ToxinProducer));

        var blueprint = new CellBlueprint(grid, [new SensorConnection(1, 2)]);

        Assert.Single(blueprint.Connections);
    }

    [Theory]
    [InlineData(-1, 2)]
    [InlineData(1, 16)]
    [InlineData(4, 2)]
    [InlineData(1, 3)]
    public void InvalidEndpointsAreRejected(int sensorSlot, int engineSlot)
    {
        var grid = GridWith(
            (1, OrganelleType.FoodGradientDetector),
            (2, OrganelleType.EffectiveEngine),
            (3, OrganelleType.RandomEngine));

        Assert.Throws<ArgumentException>(() =>
            new CellBlueprint(grid, [new SensorConnection(sensorSlot, engineSlot)]));
    }

    [Fact]
    public void UntrustedConnectionsDropOnlyInvalidEdges()
    {
        var warnings = new List<string>();
        var grid = GridWith(
            (1, OrganelleType.FoodGradientDetector),
            (2, OrganelleType.EffectiveEngine),
            (3, OrganelleType.RandomEngine));

        var blueprint = CellBlueprint.FilterInvalidConnections(grid,
        [
            new SensorConnection(1, 2),
            new SensorConnection(1, 3),
            new SensorConnection(1, 2, true)
        ], warnings.Add);

        Assert.Single(blueprint.Connections);
        Assert.Equal(2, warnings.Count);
    }

    internal static OrganelleType[] GridWith(params (int Slot, OrganelleType Type)[] entries)
    {
        var grid = CellBlueprint.Default().Grid.ToArray();
        foreach (var (slot, type) in entries) grid[slot] = type;
        return grid;
    }
}
