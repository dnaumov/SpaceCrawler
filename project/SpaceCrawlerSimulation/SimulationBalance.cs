using System.Text.Json;

public sealed class OrganelleBalance
{
    public float Upkeep { get; set; }
    public float StrengthCoefficient { get; set; } = 1f;
}

public sealed class EnvironmentBalance
{
    public float FoodSpawnInterval { get; set; } = 0.8f;
    public int MaxFood { get; set; } = 80;
    public float Drag { get; set; } = 2.5f;
    public float AngularDrag { get; set; } = 1.5f;
    public float RandomMovementPower { get; set; } = 6f;
    public float RandomRotationPower { get; set; } = 6f;
    public double RandomRotationChance { get; set; } = 0.5;
    public float MetabolismInterval { get; set; } = 10f;
    public float PassiveUpkeep { get; set; } = 1f;
    public float BaseDeathThreshold { get; set; } = -4f;
    public float EngineActivationInterval { get; set; } = 1f;
    public float ChloroplastInterval { get; set; } = 40f;
    public float SlipperyMembraneUpkeepInterval { get; set; } = 20f;
    public float ToxinProducerUpkeepInterval { get; set; } = 20f;
    public float ViscousDragMultiplier { get; set; } = 2f;
    public float ToxicUpkeepMultiplier { get; set; } = 2f;
    public float TurbulentMovementMultiplier { get; set; } = 2f;
    public float NutritiousFoodMultiplier { get; set; } = 2f;
    public float SlipperyMembraneDragMultiplier { get; set; } = 0.5f;
    public float RandomEnginePower { get; set; } = 8f;
    public float EffectiveEnginePower { get; set; } = 4f;
    public float EnginePower { get; set; } = 8f;
    public float RotationEnginePower { get; set; } = 2f;
    public float ChloroplastProduction { get; set; } = 1f;
    public float RibosomeThresholdReduction { get; set; } = 2f;
    public float MitochondriaSurvivalBonus { get; set; } = 1f;
    public float SensorAlignmentDegrees { get; set; } = 45f;
    public float FoodVisionRange { get; set; } = 8f;
    public float FoodVisionHalfAngleDegrees { get; set; } = 30f;
    public double NutritiousFoodSpawnChance { get; set; } = 0.3;
}

public sealed class SimulationBalance
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    private readonly Dictionary<OrganelleType, OrganelleBalance> _organelles;

    public EnvironmentBalance Environment { get; }

    private SimulationBalance(
        EnvironmentBalance environment,
        Dictionary<OrganelleType, OrganelleBalance> organelles)
    {
        Environment = environment;
        _organelles = organelles;
    }

    public OrganelleBalance For(OrganelleType type) => _organelles[type];

    public static SimulationBalance Default() =>
        new(new EnvironmentBalance(), DefaultOrganelles());

    public static SimulationBalance Load(
        string? environmentJson,
        Func<OrganelleType, string?> organelleJson,
        Action<string>? warn = null)
    {
        var defaults = Default();
        var environment = Deserialize(environmentJson, defaults.Environment, "environment", warn);
        var organelles = new Dictionary<OrganelleType, OrganelleBalance>();

        foreach (var type in Enum.GetValues<OrganelleType>())
        {
            if (type == OrganelleType.Empty)
            {
                organelles[type] = defaults.For(type);
                continue;
            }

            organelles[type] = Deserialize(
                organelleJson(type), defaults.For(type), type.SerializedName(), warn);
        }

        return new SimulationBalance(environment, organelles);
    }

    public static SimulationBalance LoadFromDirectory(
        string directory,
        Action<string>? warn = null)
    {
        string? Read(string path)
        {
            if (!File.Exists(path))
            {
                warn?.Invoke($"Balance file not found: {path}; using defaults.");
                return null;
            }

            return File.ReadAllText(path);
        }

        return Load(
            Read(Path.Combine(directory, "environment.json")),
            type => Read(Path.Combine(directory, "organelles", $"{type.SerializedName()}.json")),
            warn);
    }

    private static T Deserialize<T>(string? json, T fallback, string name, Action<string>? warn)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return fallback;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json, JsonOptions) ?? fallback;
        }
        catch (Exception exception)
        {
            warn?.Invoke($"Invalid {name} balance config: {exception.Message}; using defaults.");
            return fallback;
        }
    }

    private static Dictionary<OrganelleType, OrganelleBalance> DefaultOrganelles() =>
        Enum.GetValues<OrganelleType>().ToDictionary(
            type => type,
            type => new OrganelleBalance
            {
                Upkeep = type switch
                {
                    OrganelleType.RandomEngine => 2f,
                    OrganelleType.EffectiveEngine => 1f,
                    OrganelleType.Engine => 3f,
                    OrganelleType.RotationEngine => 1f,
                    OrganelleType.SlipperyMembrane => 1f,
                    OrganelleType.ToxinProducer => 1f,
                    _ => 0f
                },
                StrengthCoefficient = 1f
            });
}
