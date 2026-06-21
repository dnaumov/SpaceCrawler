using Xunit;

public sealed class WiredEngineRuntimeTests
{
    [Fact]
    public void FanOutConnectionsApplyIndependentInversion()
    {
        var blueprint = WiredBlueprint();
        var simulation = new SimulationEngine(seed: 10, balance: TestBalance());
        var cell = simulation.CreateCell("Wired", new Vec2(36, 20), blueprint);
        AlignSensorWithToxicGradient(cell, opposed: false);

        simulation.Step(0.11f);

        Assert.True(cell.SensorOutputs[1]);
        Assert.Equal(2.5f, cell.Food, 3);
        Assert.InRange(cell.Velocity.Length, 3.99f, 4.01f);
    }

    [Fact]
    public void InvertedOutputActivatesWhenSensorIsOff()
    {
        var blueprint = WiredBlueprint();
        var simulation = new SimulationEngine(seed: 10, balance: TestBalance());
        var cell = simulation.CreateCell("Wired", new Vec2(36, 20), blueprint);
        AlignSensorWithToxicGradient(cell, opposed: true);

        simulation.Step(0.11f);

        Assert.False(cell.SensorOutputs[1]);
        Assert.Equal(2.5f, cell.Food, 3);
        Assert.InRange(cell.Velocity.Length, 7.99f, 8.01f);
    }

    private static CellBlueprint WiredBlueprint() => new(
        SensorConnectionTests.GridWith(
            (1, OrganelleType.ToxicGradientDetector),
            (2, OrganelleType.EffectiveEngine),
            (3, OrganelleType.Engine)),
        [
            new SensorConnection(1, 2, false),
            new SensorConnection(1, 3, true)
        ]);

    private static SimulationBalance TestBalance()
    {
        const string environment = """
        {
          "food_spawn_interval": 1000.0,
          "max_food": 0,
          "drag": 0.0,
          "angular_drag": 0.0,
          "random_movement_power": 0.0,
          "random_rotation_power": 0.0,
          "passive_upkeep": 0.0,
          "engine_activation_interval": 0.1
        }
        """;
        return SimulationBalance.Load(environment, type => type.AcceptsSensorInput()
            ? """{"upkeep":1.0,"strength_coefficient":1.0}"""
            : """{"upkeep":0.0,"strength_coefficient":1.0}""");
    }

    private static void AlignSensorWithToxicGradient(CellState cell, bool opposed)
    {
        var toxicCenter = new Vec2(72f * 0.85f, 40f * 0.2f);
        var targetAngle = MathF.Atan2(
            toxicCenter.Y - cell.Position.Y,
            toxicCenter.X - cell.Position.X);
        var sensorAngle = MathF.Atan2(-1.5f, -0.5f);
        cell.Rotation = targetAngle - sensorAngle + (opposed ? MathF.PI : 0f);
    }
}
