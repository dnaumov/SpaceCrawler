using System.Text.Json;
using System.Text.Json.Serialization;

public static class CellBlueprintJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public static CellBlueprint Deserialize(string json, Action<string>? warn = null)
    {
        var payload = JsonSerializer.Deserialize<BlueprintPayload>(json, Options)
            ?? throw new JsonException("Blueprint JSON must contain an object.");
        if (payload.Components is null || payload.Components.Length < 16)
        {
            throw new JsonException("Blueprint components must contain 16 entries.");
        }

        var grid = payload.Components.Take(16)
            .Select(name => OrganelleTypeExtensions.FromSerializedName(name ?? string.Empty))
            .ToArray();
        foreach (var index in CellBlueprint.NucleusIndices)
        {
            grid[index] = OrganelleType.Nucleus;
        }

        var connections = payload.Connections?.Select(connection => new SensorConnection(
            connection.SensorSlot, connection.EngineSlot, connection.Inverted)) ?? [];
        return CellBlueprint.FilterInvalidConnections(grid, connections, warn);
    }

    public static string Serialize(CellBlueprint blueprint) =>
        JsonSerializer.Serialize(new BlueprintPayload
        {
            GridSize = 4,
            Components = blueprint.Grid.Select(type =>
                type == OrganelleType.Empty ? string.Empty : type.SerializedName()).ToArray(),
            Connections = blueprint.Connections.Select(connection => new ConnectionPayload
            {
                SensorSlot = connection.SensorSlot,
                EngineSlot = connection.EngineSlot,
                Inverted = connection.Inverted
            }).ToArray()
        }, Options);

    private sealed class BlueprintPayload
    {
        public int GridSize { get; set; } = 4;
        public string?[]? Components { get; set; }
        public ConnectionPayload[]? Connections { get; set; }
    }

    private sealed class ConnectionPayload
    {
        public int SensorSlot { get; set; }
        public int EngineSlot { get; set; }
        public bool Inverted { get; set; }
    }
}
