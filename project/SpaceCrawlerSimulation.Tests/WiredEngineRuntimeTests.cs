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

        simulation.Step(1.01f);

        Assert.True(cell.SensorOutputs[1]);
        Assert.Equal(3.4f, cell.Food, 3);
        Assert.InRange(cell.Velocity.Length, 0.399f, 0.401f);
    }

    [Fact]
    public void InvertedOutputActivatesWhenSensorIsOff()
    {
        var blueprint = WiredBlueprint();
        var simulation = new SimulationEngine(seed: 10, balance: TestBalance());
        var cell = simulation.CreateCell("Wired", new Vec2(36, 20), blueprint);
        AlignSensorWithToxicGradient(cell, opposed: true);

        simulation.Step(1.01f);

        Assert.False(cell.SensorOutputs[1]);
        Assert.Equal(3.4f, cell.Food, 3);
        Assert.InRange(cell.Velocity.Length, 0.799f, 0.801f);
    }

    [Fact]
    public void RandomEnginesRetainTenSecondFullStrengthActivations()
    {
        var randomSlots = Enumerable.Range(0, 16)
            .Where(slot => !CellBlueprint.NucleusIndices.Contains(slot))
            .Select(slot => (slot, OrganelleType.RandomEngine))
            .ToArray();
        var blueprint = new CellBlueprint(SensorConnectionTests.GridWith(randomSlots));
        var simulation = new SimulationEngine(seed: 10, balance: RandomEngineTestBalance());
        var cell = simulation.CreateCell("Random", new Vec2(36, 20), blueprint);
        var initialFood = cell.Food;

        simulation.Step(9.99f);

        Assert.Equal(initialFood, cell.Food, 3);
        Assert.Equal(0f, cell.Velocity.Length, 3);

        simulation.Step(0.02f);

        Assert.True(cell.Food < initialFood);
        Assert.True(cell.Velocity.Length >= 10f);
        Assert.Equal(initialFood - cell.Food, cell.Velocity.Length / 10f, 3);
    }

    [Fact]
    public void UnconnectedToxinProducerIsActiveCreatesAuraAndPaysUpkeep()
    {
        var simulation = new SimulationEngine(seed: 10, balance: ToxinTestBalance());
        var producer = simulation.CreateCell(
            "Producer",
            new Vec2(20, 20),
            new CellBlueprint(SensorConnectionTests.GridWith((0, OrganelleType.ToxinProducer))));
        var victim = simulation.CreateCell(
            "Victim",
            new Vec2(21, 20),
            CellBlueprint.Default());

        simulation.Step(10.01f);

        Assert.Equal(1, producer.ActiveToxinProducerCount);
        Assert.Equal(0.5f, producer.Food, 3);
        Assert.Equal(0f, victim.Food, 3);
    }

    [Fact]
    public void MultipleActiveToxinProducersStackAuraRadius()
    {
        var simulation = new SimulationEngine(seed: 10, balance: ToxinTestBalance());
        var producer = simulation.CreateCell(
            "Producer",
            new Vec2(20, 20),
            new CellBlueprint(SensorConnectionTests.GridWith(
                (0, OrganelleType.ToxinProducer),
                (1, OrganelleType.ToxinProducer))));
        var victim = simulation.CreateCell(
            "Victim",
            new Vec2(23, 20),
            CellBlueprint.Default());

        simulation.Step(10.01f);

        Assert.Equal(2, producer.ActiveToxinProducerCount);
        Assert.Equal(0f, producer.Food, 3);
        Assert.Equal(0f, victim.Food, 3);
    }

    [Fact]
    public void SensorConnectedToxinProducerOnlyRunsWhenSensorIsActive()
    {
        var blueprint = new CellBlueprint(
            SensorConnectionTests.GridWith(
                (1, OrganelleType.ToxicGradientDetector),
                (2, OrganelleType.ToxinProducer)),
            [new SensorConnection(1, 2)]);
        var simulation = new SimulationEngine(seed: 10, balance: ToxinTestBalance());
        var cell = simulation.CreateCell("Gated", new Vec2(36, 20), blueprint);
        AlignSensorWithToxicGradient(cell, opposed: true);

        simulation.Step(10.01f);

        Assert.False(cell.SensorOutputs[1]);
        Assert.Equal(0, cell.ActiveToxinProducerCount);
        Assert.Equal(2f, cell.Food, 3);
    }

    [Fact]
    public void ToxicSensorsDetectDynamicAuras()
    {
        var source = new CellBlueprint(SensorConnectionTests.GridWith(
            (0, OrganelleType.ToxinProducer)));
        var gated = new CellBlueprint(
            SensorConnectionTests.GridWith(
                (1, OrganelleType.ToxicGradientDetector),
                (2, OrganelleType.ToxinProducer)),
            [new SensorConnection(1, 2)]);
        var simulation = new SimulationEngine(seed: 10, balance: ToxinTestBalance());
        simulation.CreateCell("Source", new Vec2(30, 20), source);
        var detector = simulation.CreateCell("Detector", new Vec2(20, 20), gated);
        AlignSlotOneSensorToward(detector, new Vec2(30, 20));

        simulation.Step(0.05f);

        Assert.True(detector.SensorOutputs[1]);
        Assert.Equal(1, detector.ActiveToxinProducerCount);
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
          "engine_activation_interval": 1.0
        }
        """;
        return SimulationBalance.Load(environment, type => type.AcceptsSensorInput()
            ? """{"upkeep":1.0,"strength_coefficient":1.0}"""
            : """{"upkeep":0.0,"strength_coefficient":1.0}""");
    }

    private static SimulationBalance RandomEngineTestBalance()
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
          "engine_activation_interval": 1.0,
          "random_engine_power": 10.0
        }
        """;
        return SimulationBalance.Load(environment, type => type == OrganelleType.RandomEngine
            ? """{"upkeep":1.0,"strength_coefficient":1.0}"""
            : """{"upkeep":0.0,"strength_coefficient":1.0}""");
    }

    private static SimulationBalance ToxinTestBalance()
    {
        const string environment = """
        {
          "food_spawn_interval": 1000.0,
          "max_food": 0,
          "drag": 0.0,
          "angular_drag": 0.0,
          "random_movement_power": 0.0,
          "random_rotation_power": 0.0,
          "passive_upkeep": 1.0,
          "metabolism_interval": 10.0,
          "toxin_producer_upkeep_interval": 10.0,
          "toxic_upkeep_multiplier": 2.0,
          "engine_activation_interval": 1.0
        }
        """;
        return SimulationBalance.Load(environment, type => type == OrganelleType.ToxinProducer
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

    private static void AlignSlotOneSensorToward(CellState cell, Vec2 target)
    {
        var targetAngle = MathF.Atan2(
            target.Y - cell.Position.Y,
            target.X - cell.Position.X);
        var sensorAngle = MathF.Atan2(-1.5f, -0.5f);
        cell.Rotation = targetAngle - sensorAngle;
    }
}
