/// <summary>
/// Pure-C# simulation engine implementing all game rules.
/// Advances the simulation by discrete time steps; no rendering dependency.
///
/// Unit scaling: all distance and speed constants from <see cref="SimConstants"/> are
/// defined at S=1 (console scale). Pass <paramref name="unitScale"/> = S-in-pixels when
/// creating an engine for a pixel-space renderer (e.g. unitScale=16 for the Godot game).
/// The arena dimensions should be provided in the same units as <paramref name="unitScale"/>.
/// </summary>
public sealed class SimulationEngine
{
    private readonly Random _rng;
    private readonly SimulationBalance _balance;
    private readonly EnvironmentBalance _environment;
    private readonly List<CellState> _cells = [];
    private readonly List<Vec2> _foods = [];
    private readonly List<EnvironmentZone> _zones;

    // Scaled physical constants (multiplied by unitScale in constructor)
    private readonly float _cellHalfSize;
    private readonly float _foodHalfSize;
    private readonly float _dragBase;
    private readonly float _randomPushMax;
    private readonly float _randomEngineSpeed;
    private readonly float _effectiveEngineSpeed;
    private readonly float _engineSpeed;
    private readonly float _foodVisionRange;

    public float ArenaWidth  { get; }
    public float ArenaHeight { get; }
    public float ElapsedTime { get; private set; }
    public float FoodSpawnInterval { get; set; }
    public int   MaxFood           { get; set; }

    private float _foodSpawnAccum;
    private float _fixedStepAccum;

    public IReadOnlyList<CellState> Cells => _cells;
    public IReadOnlyList<Vec2>      Foods  => _foods;
    public IReadOnlyList<EnvironmentZone> Zones => _zones;

    /// <param name="arenaW">Arena width in units matching <paramref name="unitScale"/>.</param>
    /// <param name="arenaH">Arena height in units matching <paramref name="unitScale"/>.</param>
    /// <param name="unitScale">
    /// Size of one S-unit in the calling coordinate system.
    /// Console: 1.0 (default). Godot: 16.0 (pixels per S-unit).
    /// </param>
    /// <param name="seed">Random seed for deterministic runs.</param>
    public SimulationEngine(float arenaW     = SimConstants.ArenaWidth,
                            float arenaH     = SimConstants.ArenaHeight,
                            float unitScale  = 1f,
                            int   seed       = 42,
                            SimulationBalance? balance = null)
    {
        ArenaWidth  = arenaW;
        ArenaHeight = arenaH;
        _rng        = new Random(seed);
        _balance    = balance ?? SimulationBalance.Default();
        _environment = _balance.Environment;
        _zones      = BuildDefaultZones(arenaW, arenaH);
        FoodSpawnInterval = Interval(_environment.FoodSpawnInterval);
        MaxFood = Math.Max(0, _environment.MaxFood);

        // Scale all spatial constants by unitScale
        _cellHalfSize       = SimConstants.CellHalfSize       * unitScale;
        _foodHalfSize       = SimConstants.FoodHalfSize       * unitScale;
        _dragBase           = _environment.Drag * unitScale;
        _randomPushMax      = _environment.RandomMovementPower * unitScale;
        _randomEngineSpeed  = _environment.RandomEnginePower * unitScale;
        _effectiveEngineSpeed = _environment.EffectiveEnginePower * unitScale;
        _engineSpeed        = _environment.EnginePower * unitScale;
        _foodVisionRange    = _environment.FoodVisionRange * unitScale;
    }

    // ── population management ─────────────────────────────────────────────────

    public CellState CreateCell(string name, Vec2 position, CellBlueprint blueprint)
    {
        var cell = new CellState
        {
            Name      = name,
            Blueprint = blueprint,
            Position  = position,
            Rotation  = (float)(_rng.NextDouble() * MathF.PI * 2),
            Food      = blueprint.ElementCount * 0.5f,
            BiomassThreshold = GetBiomassThreshold(blueprint),
            DeathFoodThreshold = GetDeathFoodThreshold(blueprint)
        };
        _cells.Add(cell);
        return cell;
    }

    // ── main update ───────────────────────────────────────────────────────────

    /// <summary>
    /// Supply elapsed wall-clock time. Simulation state advances only in fixed-size
    /// updates so results do not depend on rendering frame rate or caller step size.
    /// </summary>
    public void Step(float dt)
    {
        if (dt <= 0f)
        {
            return;
        }

        _fixedStepAccum += dt;
        while (_fixedStepAccum + 0.000001f >= SimConstants.FixedTimeStep)
        {
            _fixedStepAccum -= SimConstants.FixedTimeStep;
            FixedStep(SimConstants.FixedTimeStep);
        }
    }

    private void FixedStep(float dt)
    {
        ElapsedTime += dt;

        // Food spawning
        _foodSpawnAccum += dt;
        var foodSpawnInterval = Interval(FoodSpawnInterval);
        while (_foodSpawnAccum >= foodSpawnInterval)
        {
            _foodSpawnAccum -= foodSpawnInterval;
            SpawnFood();
        }

        // Update each cell
        var toAdd = new List<CellState>();
        foreach (var cell in _cells)
        {
            if (!cell.Alive)
            {
                continue;
            }

            UpdateCell(cell, dt, toAdd);
        }

        _cells.AddRange(toAdd);
    }

    // ── cell physics & logic ──────────────────────────────────────────────────

    private void UpdateCell(CellState cell, float dt, List<CellState> newCells)
    {
        var env = GetEnvironment(cell.Position);

        // Drag
        var dragMult = env == EnvironmentType.Viscous
            ? _environment.ViscousDragMultiplier
            : 1f;
        if (cell.Blueprint.SlipperyMembraneCount > 0)
        {
            dragMult *= MathF.Pow(
                _environment.SlipperyMembraneDragMultiplier,
                cell.Blueprint.SlipperyMembraneCount * Strength(OrganelleType.SlipperyMembrane));
        }

        var drag = _dragBase * dragMult;
        cell.Velocity        = cell.Velocity.MoveToward(Vec2.Zero, drag * dt);
        cell.AngularVelocity = MathF.CopySign(
            Math.Max(0f, MathF.Abs(cell.AngularVelocity) - _environment.AngularDrag * dt),
            cell.AngularVelocity);

        // Passive random movement
        var turbMult = env == EnvironmentType.Turbulent
            ? _environment.TurbulentMovementMultiplier
            : 1f;
        var push = _randomPushMax * turbMult;
        cell.Velocity = new Vec2(
            cell.Velocity.X + RandF(-push, push) * dt,
            cell.Velocity.Y + RandF(-push, push) * dt);
        var angularPush = _environment.RandomRotationPower * turbMult;
        if (_rng.NextDouble() < _environment.RandomRotationChance)
        {
            cell.AngularVelocity += RandF(-angularPush, angularPush) * dt;
        }

        // Integrate
        cell.Position = new Vec2(
            Math.Clamp(cell.Position.X + cell.Velocity.X * dt,
                       _cellHalfSize, ArenaWidth  - _cellHalfSize),
            Math.Clamp(cell.Position.Y + cell.Velocity.Y * dt,
                       _cellHalfSize, ArenaHeight - _cellHalfSize));
        cell.Rotation = WrapAngle(cell.Rotation + cell.AngularVelocity * dt);

        // Food collection
        CollectFood(cell, env);

        // Tick accumulator (every T seconds)
        cell.TickAccum += dt;
        var metabolismInterval = Interval(_environment.MetabolismInterval);
        if (cell.TickAccum >= metabolismInterval)
        {
            cell.TickAccum -= metabolismInterval;
            RunTick(cell);
        }

        // Chloroplast income
        if (cell.Blueprint.ChloroplastCount > 0)
        {
            cell.ChloroAccum += dt;
            var chloroplastInterval = Interval(_environment.ChloroplastInterval);
            if (cell.ChloroAccum >= chloroplastInterval)
            {
                cell.ChloroAccum -= chloroplastInterval;
                var produced = cell.Blueprint.ChloroplastCount *
                               _environment.ChloroplastProduction *
                               Strength(OrganelleType.Chloroplast);
                cell.Food += produced;
            }
        }

        // Slippery Membrane upkeep
        if (cell.Blueprint.SlipperyMembraneCount > 0)
        {
            cell.SlipperyAccum += dt;
            var membraneInterval = Interval(_environment.SlipperyMembraneUpkeepInterval);
            if (cell.SlipperyAccum >= membraneInterval)
            {
                cell.SlipperyAccum -= membraneInterval;
                cell.Food -= cell.Blueprint.SlipperyMembraneCount * Upkeep(OrganelleType.SlipperyMembrane);
            }
        }

        // Toxin Producer upkeep
        if (cell.Blueprint.ToxinProducerCount > 0)
        {
            cell.ToxinAccum += dt;
            var toxinInterval = Interval(_environment.ToxinProducerUpkeepInterval);
            if (cell.ToxinAccum >= toxinInterval)
            {
                cell.ToxinAccum -= toxinInterval;
                cell.Food -= cell.Blueprint.ToxinProducerCount * Upkeep(OrganelleType.ToxinProducer);
            }
        }

        // Death check
        if (cell.Food <= cell.DeathFoodThreshold)
        {
            cell.Alive = false;
            return;
        }

        // Forward engines attempt activation on their own explicit interval.
        cell.EngineAccum += dt;
        var engineInterval = Interval(_environment.EngineActivationInterval);
        while (cell.EngineAccum >= engineInterval)
        {
            cell.EngineAccum -= engineInterval;
            ActivateEngines(cell);
        }

        // Duplication
        if (cell.Food >= cell.BiomassThreshold)
        {
            cell.DuplicationCount++;

            var offset = new Vec2(
                RandF(-_cellHalfSize * 3, _cellHalfSize * 3),
                RandF(-_cellHalfSize * 3, _cellHalfSize * 3));
            var daughter = new CellState
            {
                Name      = $"{cell.Name}'",
                Blueprint = cell.Blueprint,
                Position  = new Vec2(
                    Math.Clamp(cell.Position.X + offset.X, _cellHalfSize, ArenaWidth  - _cellHalfSize),
                    Math.Clamp(cell.Position.Y + offset.Y, _cellHalfSize, ArenaHeight - _cellHalfSize)),
                Rotation  = WrapAngle(cell.Rotation + MathF.PI),
                Food      = cell.Food * 0.5f,
                BiomassThreshold = cell.BiomassThreshold,
                DeathFoodThreshold = cell.DeathFoodThreshold
            };
            cell.Food *= 0.5f;
            newCells.Add(daughter);
        }
    }

    private void RunTick(CellState cell)
    {
        var env       = GetEnvironment(cell.Position);
        var drainMult = env == EnvironmentType.Toxic ? _environment.ToxicUpkeepMultiplier : 1f;
        cell.Food -= _environment.PassiveUpkeep * drainMult;
    }

    private void EvaluateSensorOutputs(CellState cell)
    {
        Array.Clear(cell.SensorOutputs);

        Vec2? foodGradient = null;
        Vec2? cellGradient = null;
        Vec2? toxicGradient = null;

        for (var index = 0; index < cell.Blueprint.Grid.Length; index++)
        {
            var sensor = cell.Blueprint.Grid[index];
            if (!sensor.IsSensor())
            {
                continue;
            }

            var facing = SensorFacingDirection(index, cell.Rotation);
            cell.SensorOutputs[index] = sensor switch
            {
                OrganelleType.FoodGradientDetector => IsAligned(
                    facing,
                    (foodGradient ??= GradientField.DirectionAt(
                        cell.Position, _foods, _cellHalfSize)),
                    sensor),
                OrganelleType.CellsGradientDetector => IsAligned(
                    facing,
                    (cellGradient ??= GradientField.DirectionAt(
                        cell.Position,
                        _cells.Where(other => other.Alive && !ReferenceEquals(other, cell))
                            .Select(other => other.Position),
                        _cellHalfSize)),
                    sensor),
                OrganelleType.ToxicGradientDetector => IsAligned(
                    facing,
                    (toxicGradient ??= GradientField.DirectionAt(
                        cell.Position,
                        _zones.Where(zone => zone.Type == EnvironmentType.Toxic)
                            .Select(zone => new Vec2(zone.X + zone.W * 0.5f,
                                                    zone.Y + zone.H * 0.5f)),
                        _cellHalfSize)),
                    sensor),
                OrganelleType.FoodVision => CanSeeFood(cell.Position, facing),
                _ => false
            };
        }
    }

    private static Vec2 SensorFacingDirection(int gridIndex, float cellRotation)
    {
        var column = gridIndex % 4;
        var row = gridIndex / 4;
        var outward = new Vec2(column - 1.5f, row - 1.5f).Normalized();
        return outward.Rotated(cellRotation);
    }

    private bool IsAligned(Vec2 facing, Vec2 gradient, OrganelleType sensor)
    {
        var halfAngle = Math.Clamp(
            _environment.SensorAlignmentDegrees * Strength(sensor), 0f, 180f);
        var threshold = MathF.Cos(halfAngle * MathF.PI / 180f);
        return gradient.LengthSq > 0f && Vec2.Dot(facing, gradient) >= threshold;
    }

    private bool CanSeeFood(Vec2 position, Vec2 facing)
    {
        var visionStrength = Strength(OrganelleType.FoodVision);
        var range = _foodVisionRange * visionStrength;
        var rangeSq = range * range;
        var halfAngle = Math.Clamp(
            _environment.FoodVisionHalfAngleDegrees * visionStrength, 0f, 180f);
        var alignmentThreshold = MathF.Cos(halfAngle * MathF.PI / 180f);
        foreach (var food in _foods)
        {
            var offset = food - position;
            if (offset.LengthSq <= rangeSq &&
                offset.LengthSq > 0f &&
                Vec2.Dot(facing, offset.Normalized()) >= alignmentThreshold)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>Evaluates sensor outputs once, then activates each engine independently.</summary>
    private void ActivateEngines(CellState cell)
    {
        EvaluateSensorOutputs(cell);
        var forward = new Vec2(0f, -1f).Rotated(cell.Rotation);
        for (var slot = 0; slot < cell.Blueprint.Grid.Length; slot++)
        {
            var engine = cell.Blueprint.Grid[slot];
            if (engine == OrganelleType.RandomEngine)
            {
                if (_rng.NextDouble() < 0.5)
                {
                    ApplyForwardEngine(cell, forward, engine, _randomEngineSpeed);
                }
                continue;
            }

            if (!engine.AcceptsSensorInput() || !ShouldActivateEngine(cell, slot))
            {
                continue;
            }

            switch (engine)
            {
                case OrganelleType.EffectiveEngine:
                    ApplyForwardEngine(cell, forward, engine, _effectiveEngineSpeed);
                    break;
                case OrganelleType.Engine:
                    ApplyForwardEngine(cell, forward, engine, _engineSpeed);
                    break;
                case OrganelleType.RotationEngine:
                    var torqueDirection = slot % 4 < 2 ? 1f : -1f;
                    cell.AngularVelocity += torqueDirection * _environment.RotationEnginePower *
                                            Strength(OrganelleType.RotationEngine);
                    cell.Food -= Upkeep(OrganelleType.RotationEngine);
                    break;
            }
        }
    }

    private bool ShouldActivateEngine(CellState cell, int engineSlot)
    {
        if (!cell.Blueprint.TryGetEngineInput(engineSlot, out var input))
        {
            return _rng.NextDouble() < 0.5;
        }

        return cell.SensorOutputs[input.SensorSlot] != input.Inverted;
    }

    private void ApplyForwardEngine(
        CellState cell,
        Vec2 forward,
        OrganelleType engine,
        float basePower)
    {
        cell.Velocity += forward * (basePower * Strength(engine));
        cell.Food -= Upkeep(engine);
    }

    private void CollectFood(CellState cell, EnvironmentType env)
    {
        var mult    = env == EnvironmentType.Nutritious ? _environment.NutritiousFoodMultiplier : 1f;
        var touchSq = (_cellHalfSize + _foodHalfSize) * (_cellHalfSize + _foodHalfSize);

        for (var i = _foods.Count - 1; i >= 0; i--)
        {
            if (cell.Position.DistanceSq(_foods[i]) <= touchSq)
            {
                _foods.RemoveAt(i);
                cell.Food += mult;
            }
        }
    }

    // ── food spawning ─────────────────────────────────────────────────────────

    private void SpawnFood()
    {
        if (_foods.Count >= MaxFood)
        {
            return;
        }

        Vec2 pos;
        // Prefer nutritious zone for spawning (30% of the time)
        var nutriZone = _zones.FirstOrDefault(z => z.Type == EnvironmentType.Nutritious);
        if (nutriZone is not null && _rng.NextDouble() < _environment.NutritiousFoodSpawnChance)
        {
            pos = new Vec2(
                (float)(nutriZone.X + _rng.NextDouble() * nutriZone.W),
                (float)(nutriZone.Y + _rng.NextDouble() * nutriZone.H));
        }
        else
        {
            pos = new Vec2(
                (float)(_rng.NextDouble() * ArenaWidth),
                (float)(_rng.NextDouble() * ArenaHeight));
        }

        _foods.Add(pos);
    }

    // ── environment helpers ───────────────────────────────────────────────────

    private EnvironmentType GetEnvironment(Vec2 pos)
    {
        foreach (var zone in _zones)
        {
            if (zone.Contains(pos))
            {
                return zone.Type;
            }
        }

        return EnvironmentType.Normal;
    }

    private float Strength(OrganelleType type) =>
        Math.Max(0f, _balance.For(type).StrengthCoefficient);

    private float Upkeep(OrganelleType type) =>
        Math.Max(0f, _balance.For(type).Upkeep);

    private static float Interval(float value) =>
        Math.Max(SimConstants.FixedTimeStep, value);

    private float GetBiomassThreshold(CellBlueprint blueprint) =>
        Math.Max(1f, blueprint.ElementCount -
            blueprint.RibosomeCount * _environment.RibosomeThresholdReduction *
            Strength(OrganelleType.Ribosome));

    private float GetDeathFoodThreshold(CellBlueprint blueprint) =>
        _environment.BaseDeathThreshold -
        blueprint.MitochondriaCount * _environment.MitochondriaSurvivalBonus *
        Strength(OrganelleType.Mitochondria);

    private static List<EnvironmentZone> BuildDefaultZones(float w, float h) =>
    [
        new EnvironmentZone { X = 0,        Y = 0,        W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Viscous    },
        new EnvironmentZone { X = w * 0.7f, Y = 0,        W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Toxic      },
        new EnvironmentZone { X = 0,        Y = h * 0.6f, W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Turbulent  },
        new EnvironmentZone { X = w * 0.7f, Y = h * 0.6f, W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Nutritious }
    ];

    // ── math helpers ──────────────────────────────────────────────────────────

    private float RandF(float min, float max) =>
        min + (float)_rng.NextDouble() * (max - min);

    private static float WrapAngle(float a)
    {
        while (a >  MathF.PI) a -= MathF.PI * 2;
        while (a < -MathF.PI) a += MathF.PI * 2;
        return a;
    }

    // ── result helpers ────────────────────────────────────────────────────────

    public CellState? GetWinner()
    {
        if (_cells.Count == 0)
        {
            return null;
        }

        return _cells
            .OrderByDescending(c => c.DuplicationCount)
            .ThenByDescending(c => c.Food)
            .First();
    }

    public int AliveCellCount => _cells.Count(c => c.Alive);
}
