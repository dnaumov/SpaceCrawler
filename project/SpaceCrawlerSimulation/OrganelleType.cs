/// <summary>
/// All organelle types available in the cell builder.
/// Mirrors the Godot-project OrganelleType enum (no Godot dependency here).
/// </summary>
public enum OrganelleType
{
    Empty,
    Nucleus,

    // Movement (orientation: away from nucleus)
    RandomEngine,
    EffectiveEngine,
    Engine,

    // Storage & energy
    Mitochondria,
    Chloroplast,
    Ribosome,

    // Sensory (orientation: away from nucleus, connectable to movement organelles)
    CellsGradientDetector,
    FoodGradientDetector,
    ToxicGradientDetector,
    FoodVision,

    // Other
    SlipperyMembrane,
    ToxinProducer
}

public static class OrganelleTypeExtensions
{
    public static string DisplayName(this OrganelleType t) => t switch
    {
        OrganelleType.Empty                 => "(Empty)",
        OrganelleType.Nucleus               => "Nucleus",
        OrganelleType.RandomEngine          => "Random Engine",
        OrganelleType.EffectiveEngine       => "Eff. Engine",
        OrganelleType.Engine                => "Engine",
        OrganelleType.Mitochondria          => "Mitochondria",
        OrganelleType.Chloroplast           => "Chloroplast",
        OrganelleType.Ribosome              => "Ribosome",
        OrganelleType.CellsGradientDetector => "Cell Sensor",
        OrganelleType.FoodGradientDetector  => "Food Sensor",
        OrganelleType.ToxicGradientDetector => "Toxic Sensor",
        OrganelleType.FoodVision            => "Food Vision",
        OrganelleType.SlipperyMembrane      => "Slip. Membrane",
        OrganelleType.ToxinProducer         => "Toxin Prod.",
        _                                   => t.ToString()
    };

    public static bool IsMovement(this OrganelleType t) =>
        t is OrganelleType.RandomEngine or OrganelleType.EffectiveEngine or OrganelleType.Engine;

    public static bool IsSensor(this OrganelleType t) =>
        t is OrganelleType.CellsGradientDetector or OrganelleType.FoodGradientDetector
            or OrganelleType.ToxicGradientDetector or OrganelleType.FoodVision;

    /// <summary>Canonical name used in JSON serialization and scene component names.</summary>
    public static string SerializedName(this OrganelleType t) => t.ToString();

    public static OrganelleType FromSerializedName(string name)
    {
        if (Enum.TryParse<OrganelleType>(name, out var result))
        {
            return result;
        }

        // Legacy display-name fallback for configs created before the rename
        return name switch
        {
            "Random Engine"  => OrganelleType.RandomEngine,
            "Eff. Engine"    => OrganelleType.EffectiveEngine,
            "Engine"         => OrganelleType.Engine,
            "Mitochondria"   => OrganelleType.Mitochondria,
            "Chloroplast"    => OrganelleType.Chloroplast,
            "Ribosome"       => OrganelleType.Ribosome,
            "Cell Sensor"    => OrganelleType.CellsGradientDetector,
            "Food Sensor"    => OrganelleType.FoodGradientDetector,
            "Toxic Sensor"   => OrganelleType.ToxicGradientDetector,
            "Food Vision"    => OrganelleType.FoodVision,
            "Slip. Membrane" => OrganelleType.SlipperyMembrane,
            "Toxin Prod."    => OrganelleType.ToxinProducer,
            _                => OrganelleType.Empty
        };
    }

    public static OrganelleType FromName(string name) =>
        Enum.TryParse<OrganelleType>(name, out var r) ? r : OrganelleType.Empty;
}
