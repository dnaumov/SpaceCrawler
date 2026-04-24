using System;
using System.Linq;

/// <summary>
/// All organelle types that can occupy a cell grid slot.
/// Nucleus is always placed at center 4 positions and cannot be changed by the player.
/// </summary>
public enum OrganelleType
{
    Empty,
    Nucleus,

    // Movement organelles – orientation is away from nucleus
    RandomEngine,
    EffectiveEngine,
    Engine,

    // Storage & energy organelles
    Mitochondria,
    Chloroplast,
    Ribosome,

    // Sensory organelles – orientation is away from nucleus, connectable to movement organelles
    CellsGradientDetector,
    FoodGradientDetector,
    ToxicGradientDetector,
    FoodVision,

    // Other organelles
    SlipperyMembrane,
    ToxinProducer
}

public static class OrganelleTypeExtensions
{
    public static string DisplayName(this OrganelleType type) => type switch
    {
        OrganelleType.Empty                  => "(Empty)",
        OrganelleType.Nucleus                => "Nucleus",
        OrganelleType.RandomEngine           => "Random Engine",
        OrganelleType.EffectiveEngine        => "Eff. Engine",
        OrganelleType.Engine                 => "Engine",
        OrganelleType.Mitochondria           => "Mitochondria",
        OrganelleType.Chloroplast            => "Chloroplast",
        OrganelleType.Ribosome               => "Ribosome",
        OrganelleType.CellsGradientDetector  => "Cell Sensor",
        OrganelleType.FoodGradientDetector   => "Food Sensor",
        OrganelleType.ToxicGradientDetector  => "Toxic Sensor",
        OrganelleType.FoodVision             => "Food Vision",
        OrganelleType.SlipperyMembrane       => "Slip. Membrane",
        OrganelleType.ToxinProducer          => "Toxin Prod.",
        _                                    => type.ToString()
    };

    public static bool IsMovement(this OrganelleType type) =>
        type is OrganelleType.RandomEngine or OrganelleType.EffectiveEngine or OrganelleType.Engine;

    public static bool IsSensor(this OrganelleType type) =>
        type is OrganelleType.CellsGradientDetector or OrganelleType.FoodGradientDetector
            or OrganelleType.ToxicGradientDetector or OrganelleType.FoodVision;

    /// <summary>Canonical name used in JSON serialization and scene component names.</summary>
    public static string SerializedName(this OrganelleType type) => type.ToString();

    public static OrganelleType FromSerializedName(string name)
    {
        if (Enum.TryParse<OrganelleType>(name, out var result))
        {
            return result;
        }

        // Legacy display-name fallback for configs created before the rename
        return name switch
        {
            "Random Engine"    => OrganelleType.RandomEngine,
            "Eff. Engine"      => OrganelleType.EffectiveEngine,
            "Engine"           => OrganelleType.Engine,
            "Mitochondria"     => OrganelleType.Mitochondria,
            "Chloroplast"      => OrganelleType.Chloroplast,
            "Ribosome"         => OrganelleType.Ribosome,
            "Cell Sensor"      => OrganelleType.CellsGradientDetector,
            "Food Sensor"      => OrganelleType.FoodGradientDetector,
            "Toxic Sensor"     => OrganelleType.ToxicGradientDetector,
            "Food Vision"      => OrganelleType.FoodVision,
            "Slip. Membrane"   => OrganelleType.SlipperyMembrane,
            "Toxin Prod."      => OrganelleType.ToxinProducer,
            _                  => OrganelleType.Empty
        };
    }
}
