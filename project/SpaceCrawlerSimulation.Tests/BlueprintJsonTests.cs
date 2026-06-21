using Xunit;

public sealed class BlueprintJsonTests
{
    [Fact]
    public void MissingConnectionsRemainsBackwardCompatible()
    {
        var components = Enumerable.Repeat("", 16).ToArray();
        var json = $$"""{"grid_size":4,"components":{{System.Text.Json.JsonSerializer.Serialize(components)}}}""";

        var blueprint = CellBlueprintJson.Deserialize(json);

        Assert.Empty(blueprint.Connections);
        Assert.All(CellBlueprint.NucleusIndices,
            index => Assert.Equal(OrganelleType.Nucleus, blueprint.Grid[index]));
    }

    [Fact]
    public void RoundTripPreservesFanOutAndInversion()
    {
        var blueprint = new CellBlueprint(
            SensorConnectionTests.GridWith(
                (1, OrganelleType.FoodGradientDetector),
                (2, OrganelleType.EffectiveEngine),
                (3, OrganelleType.RotationEngine)),
            [new SensorConnection(1, 2), new SensorConnection(1, 3, true)]);

        var restored = CellBlueprintJson.Deserialize(CellBlueprintJson.Serialize(blueprint));

        Assert.Equal(blueprint.Grid, restored.Grid);
        Assert.Equal(blueprint.Connections, restored.Connections);
    }

    [Fact]
    public void InvalidJsonEdgesWarnAndAreDropped()
    {
        var components = Enumerable.Repeat("", 16).ToArray();
        components[1] = "FoodGradientDetector";
        components[2] = "EffectiveEngine";
        var json = $$"""
        {
          "components": {{System.Text.Json.JsonSerializer.Serialize(components)}},
          "connections": [
            {"sensor_slot":1,"engine_slot":2,"inverted":false},
            {"sensor_slot":1,"engine_slot":15,"inverted":false}
          ]
        }
        """;
        var warnings = new List<string>();

        var blueprint = CellBlueprintJson.Deserialize(json, warnings.Add);

        Assert.Single(blueprint.Connections);
        Assert.Single(warnings);
    }
}
