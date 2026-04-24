/// <summary>
/// Shared simulation constants matching the design rules document.
/// </summary>
public static class SimConstants
{
    /// <summary>Tick interval [T] = 10 seconds.</summary>
    public const float TickInterval = 10f;

    /// <summary>Passive food consumption per tick [C] = 1 food.</summary>
    public const float PassiveFoodDrain = 1f;

    /// <summary>Base negative-food limit before death.</summary>
    public const float NegativeFoodBase = -4f;

    /// <summary>Base size unit [S]. Arena and cell sizes are multiples of S.</summary>
    public const float S = 1f;

    /// <summary>Cell half-size: cell is 2S × 2S.</summary>
    public const float CellHalfSize = S;

    /// <summary>Food half-size: food is 1S × 1S.</summary>
    public const float FoodHalfSize = S * 0.5f;

    /// <summary>Arena width in S units.</summary>
    public const float ArenaWidth = 72f;

    /// <summary>Arena height in S units.</summary>
    public const float ArenaHeight = 40f;

    /// <summary>Base drag (speed units lost per second).</summary>
    public const float DragBase = 2.5f;

    /// <summary>Maximum random push applied per second (passive cell movement).</summary>
    public const float RandomPushMax = 1.8f;

    /// <summary>Angular drag (rad/s² loss per second).</summary>
    public const float AngularDrag = 1.5f;

    /// <summary>Speed given by the Random Engine organelle (speed units).</summary>
    public const float RandomEngineSpeed = 2f;

    /// <summary>Speed given by the Effective Engine organelle.</summary>
    public const float EffectiveEngineSpeed = 1f;

    /// <summary>Speed given by the Engine organelle.</summary>
    public const float EngineSpeed = 2f;

    /// <summary>Food cost for Random Engine activation.</summary>
    public const float RandomEngineFoodCost = 2f;

    /// <summary>Food cost for Effective Engine activation.</summary>
    public const float EffectiveEngineFoodCost = 1f;

    /// <summary>Food cost for Engine activation.</summary>
    public const float EngineFoodCost = 3f;

    /// <summary>Chloroplast produces 1 food every 40 seconds.</summary>
    public const float ChloroplastInterval = 40f;

    /// <summary>Slippery Membrane costs 1 food every 2 ticks (20 s).</summary>
    public const float SlipperyMembraneCostInterval = 20f;

    /// <summary>Toxin Producer costs 1 food every 2 ticks (20 s).</summary>
    public const float ToxinProducerCostInterval = 20f;

    /// <summary>Elastic restitution coefficient for cell-cell collisions.</summary>
    public const float CollisionRestitution = 0.4f;

    /// <summary>Viscous environment drag multiplier.</summary>
    public const float ViscousDragMultiplier = 2f;

    /// <summary>Toxic environment passive-drain multiplier.</summary>
    public const float ToxicDrainMultiplier = 2f;

    /// <summary>Turbulent environment random-movement multiplier.</summary>
    public const float TurbulentMovementMultiplier = 2f;

    /// <summary>Nutritious environment food collection multiplier.</summary>
    public const float NutritiousFoodMultiplier = 2f;

    /// <summary>Slippery Membrane drag reduction multiplier (halves drag).</summary>
    public const float SlipperyMembraneMultiplier = 0.5f;

    /// <summary>
    /// Direct-control speed multiplier for player cells.
    /// The player's velocity impulse per frame = EngineSpeed × PlayerSpeedMultiplier × dt.
    /// Tuned so player movement feels responsive at both console and pixel scales.
    /// </summary>
    public const float PlayerSpeedMultiplier = 10f;
}
