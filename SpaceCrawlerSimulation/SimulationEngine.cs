namespace SpaceCrawlerSimulation;

/// <summary>
/// Pure-C# simulation engine implementing all game rules.
/// Advances the simulation by discrete time steps; no rendering dependency.
/// </summary>
public sealed class SimulationEngine
{
    private readonly Random _rng;
    private readonly List<CellState> _cells = [];
    private readonly List<Vec2> _foods = [];
    private readonly List<EnvironmentZone> _zones;
    private readonly GradientField _gradients;

    public float ArenaWidth { get; }
    public float ArenaHeight { get; }
    public float ElapsedTime { get; private set; }
    public float FoodSpawnInterval { get; set; } = 0.8f;
    public int MaxFood { get; set; } = 80;

    private float _foodSpawnAccum;
    private float _gradRecalcAccum;
    private const float GradRecalcInterval = 0.5f;

    public IReadOnlyList<CellState> Cells => _cells;
    public IReadOnlyList<Vec2> Foods => _foods;
    public IReadOnlyList<EnvironmentZone> Zones => _zones;

    public SimulationEngine(float arenaW = SimConstants.ArenaWidth,
                            float arenaH = SimConstants.ArenaHeight,
                            int seed = 42)
    {
        ArenaWidth  = arenaW;
        ArenaHeight = arenaH;
        _rng        = new Random(seed);
        _zones      = BuildDefaultZones(arenaW, arenaH);
        _gradients  = new GradientField(arenaW, arenaH);
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
            Food      = blueprint.ElementCount * 0.5f
        };
        _cells.Add(cell);
        return cell;
    }

    // ── main update ───────────────────────────────────────────────────────────

    /// <summary>Advance the simulation by <paramref name="dt"/> seconds.</summary>
    public void Step(float dt)
    {
        ElapsedTime += dt;

        // Food spawning
        _foodSpawnAccum += dt;
        while (_foodSpawnAccum >= FoodSpawnInterval)
        {
            _foodSpawnAccum -= FoodSpawnInterval;
            SpawnFood();
        }

        // Gradient recalculation
        _gradRecalcAccum += dt;
        if (_gradRecalcAccum >= GradRecalcInterval)
        {
            _gradRecalcAccum -= GradRecalcInterval;
            var cellPositions = _cells.Select(c => (c.Position, c.Alive)).ToList();
            _gradients.Recalculate(_foods, cellPositions, _zones);
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

        // Resolve cell-cell collisions
        ResolveCollisions();
    }

    // ── cell physics & logic ──────────────────────────────────────────────────

    private void UpdateCell(CellState cell, float dt, List<CellState> newCells)
    {
        var env = GetEnvironment(cell.Position);

        // Drag
        var dragMult = env == EnvironmentType.Viscous
            ? SimConstants.ViscousDragMultiplier
            : 1f;
        if (cell.Blueprint.SlipperyMembraneCount > 0)
        {
            dragMult *= SimConstants.SlipperyMembraneMultiplier;
        }

        var drag = SimConstants.DragBase * dragMult;
        cell.Velocity     = cell.Velocity.MoveToward(Vec2.Zero, drag * dt);
        cell.AngularVelocity = MathF.CopySign(
            Math.Max(0f, MathF.Abs(cell.AngularVelocity) - SimConstants.AngularDrag * dt),
            cell.AngularVelocity);

        // Passive random movement
        var turbMult = env == EnvironmentType.Turbulent
            ? SimConstants.TurbulentMovementMultiplier
            : 1f;
        var push = SimConstants.RandomPushMax * turbMult;
        cell.Velocity = new Vec2(
            cell.Velocity.X + RandF(-push, push) * dt,
            cell.Velocity.Y + RandF(-push, push) * dt);
        cell.AngularVelocity += RandF(-0.4f, 0.4f) * turbMult * dt;

        // Engine activation (autonomous / sensor-guided)
        ActivateEngines(cell, dt);

        // Integrate
        cell.Position = new Vec2(
            Math.Clamp(cell.Position.X + cell.Velocity.X * dt,
                       SimConstants.CellHalfSize, ArenaWidth  - SimConstants.CellHalfSize),
            Math.Clamp(cell.Position.Y + cell.Velocity.Y * dt,
                       SimConstants.CellHalfSize, ArenaHeight - SimConstants.CellHalfSize));
        cell.Rotation = WrapAngle(cell.Rotation + cell.AngularVelocity * dt);

        // Food collection
        CollectFood(cell, env);

        // Tick accumulator (every T seconds)
        cell.TickAccum += dt;
        if (cell.TickAccum >= SimConstants.TickInterval)
        {
            cell.TickAccum -= SimConstants.TickInterval;
            RunTick(cell);
        }

        // Chloroplast income
        if (cell.Blueprint.ChloroplastCount > 0)
        {
            cell.ChloroAccum += dt;
            if (cell.ChloroAccum >= SimConstants.ChloroplastInterval)
            {
                cell.ChloroAccum -= SimConstants.ChloroplastInterval;
                cell.Food += cell.Blueprint.ChloroplastCount;
            }
        }

        // Slippery Membrane upkeep
        if (cell.Blueprint.SlipperyMembraneCount > 0)
        {
            cell.SlipperyAccum += dt;
            if (cell.SlipperyAccum >= SimConstants.SlipperyMembraneCostInterval)
            {
                cell.SlipperyAccum -= SimConstants.SlipperyMembraneCostInterval;
                cell.Food -= cell.Blueprint.SlipperyMembraneCount;
            }
        }

        // Toxin Producer upkeep
        if (cell.Blueprint.ToxinProducerCount > 0)
        {
            cell.ToxinAccum += dt;
            if (cell.ToxinAccum >= SimConstants.ToxinProducerCostInterval)
            {
                cell.ToxinAccum -= SimConstants.ToxinProducerCostInterval;
                cell.Food -= cell.Blueprint.ToxinProducerCount;
            }
        }

        // Death check
        if (cell.Food <= cell.Blueprint.DeathFoodThreshold)
        {
            cell.Alive = false;
            return;
        }

        // Duplication
        if (cell.FoodCollectedForDup >= cell.Blueprint.FoodForDuplication)
        {
            cell.FoodCollectedForDup = 0;
            cell.DuplicationCount++;

            var offset = new Vec2(RandF(-SimConstants.CellHalfSize * 3, SimConstants.CellHalfSize * 3),
                                  RandF(-SimConstants.CellHalfSize * 3, SimConstants.CellHalfSize * 3));
            var daughter = new CellState
            {
                Name       = $"{cell.Name}'",
                Blueprint  = cell.Blueprint,
                Position   = new Vec2(
                    Math.Clamp(cell.Position.X + offset.X, SimConstants.CellHalfSize, ArenaWidth  - SimConstants.CellHalfSize),
                    Math.Clamp(cell.Position.Y + offset.Y, SimConstants.CellHalfSize, ArenaHeight - SimConstants.CellHalfSize)),
                Rotation   = WrapAngle(cell.Rotation + MathF.PI),
                Food       = cell.Food * 0.5f
            };
            cell.Food *= 0.5f;
            newCells.Add(daughter);
        }
    }

    private void RunTick(CellState cell)
    {
        var env = GetEnvironment(cell.Position);
        var drainMult = env == EnvironmentType.Toxic ? SimConstants.ToxicDrainMultiplier : 1f;
        cell.Food -= SimConstants.PassiveFoodDrain * drainMult;

        // Re-evaluate sensors once per tick
        cell.SensorActive = EvaluateSensors(cell);
    }

    private bool EvaluateSensors(CellState cell)
    {
        if (!cell.Blueprint.HasSensor)
        {
            return false;
        }

        if (cell.Blueprint.HasFoodSensor && _foods.Count > 0)
        {
            // Food gradient sensor: active when food gradient is non-negligible
            var grad = _gradients.FoodGradAt(cell.Position, ArenaWidth, ArenaHeight);
            if (grad > 0.001f)
            {
                return true;
            }

            // Food vision: food directly in forward arc
            var ahead = new Vec2(0f, -SimConstants.CellHalfSize * 4f).Rotated(cell.Rotation);
            var aheadPos = cell.Position + ahead;
            foreach (var food in _foods)
            {
                if (food.DistanceSq(aheadPos) < (SimConstants.CellHalfSize * 3f) * (SimConstants.CellHalfSize * 3f))
                {
                    return true;
                }
            }
        }

        if (cell.Blueprint.HasCellSensor)
        {
            var grad = _gradients.CellGradAt(cell.Position, ArenaWidth, ArenaHeight);
            if (grad > 0.001f)
            {
                return true;
            }
        }

        if (cell.Blueprint.HasToxicSensor)
        {
            var grad = _gradients.ToxicGradAt(cell.Position, ArenaWidth, ArenaHeight);
            if (grad > 0.001f)
            {
                return true; // consider moving away — engine steering not implemented for inverse links
            }
        }

        return false;
    }

    private void ActivateEngines(CellState cell, float dt)
    {
        // Determine forward direction (toward nearest food or current heading)
        var forward = new Vec2(0f, -1f).Rotated(cell.Rotation);
        var nearest = FindNearestFood(cell.Position);
        if (nearest.HasValue)
        {
            var dir = (nearest.Value - cell.Position).Normalized();
            // Steer toward food
            var targetAngle = MathF.Atan2(dir.Y, dir.X) + MathF.PI * 0.5f;
            cell.Rotation = LerpAngle(cell.Rotation, targetAngle, dt * 3f);
            forward = new Vec2(0f, -1f).Rotated(cell.Rotation);
        }

        // Random Engine: 50% chance each T → probability per frame
        if (cell.Blueprint.RandomEngineCount > 0 &&
            _rng.NextDouble() < 0.5 * dt / SimConstants.TickInterval)
        {
            cell.Velocity = cell.Velocity + forward * (SimConstants.RandomEngineSpeed * cell.Blueprint.RandomEngineCount);
            cell.Food    -= SimConstants.RandomEngineFoodCost * cell.Blueprint.RandomEngineCount;
        }

        // Effective Engine: needs sensor or 50% chance
        if (cell.Blueprint.EffectiveEngineCount > 0)
        {
            var activate = cell.SensorActive ||
                           _rng.NextDouble() < 0.5 * dt / SimConstants.TickInterval;
            if (activate)
            {
                cell.Velocity = cell.Velocity + forward * (SimConstants.EffectiveEngineSpeed * cell.Blueprint.EffectiveEngineCount);
                cell.Food    -= SimConstants.EffectiveEngineFoodCost * cell.Blueprint.EffectiveEngineCount;
            }
        }

        // Engine: needs sensor or 50% chance
        if (cell.Blueprint.EngineCount > 0)
        {
            var activate = cell.SensorActive ||
                           _rng.NextDouble() < 0.5 * dt / SimConstants.TickInterval;
            if (activate)
            {
                cell.Velocity = cell.Velocity + forward * (SimConstants.EngineSpeed * cell.Blueprint.EngineCount);
                cell.Food    -= SimConstants.EngineFoodCost * cell.Blueprint.EngineCount;
            }
        }
    }

    private void CollectFood(CellState cell, EnvironmentType env)
    {
        var mult = env == EnvironmentType.Nutritious ? SimConstants.NutritiousFoodMultiplier : 1f;
        var touchSq = (SimConstants.CellHalfSize + SimConstants.FoodHalfSize) *
                      (SimConstants.CellHalfSize + SimConstants.FoodHalfSize);

        for (var i = _foods.Count - 1; i >= 0; i--)
        {
            if (cell.Position.DistanceSq(_foods[i]) <= touchSq)
            {
                _foods.RemoveAt(i);
                cell.Food += 1f * mult;
                cell.FoodCollectedForDup += 1;
            }
        }
    }

    private void ResolveCollisions()
    {
        var minDist = SimConstants.CellHalfSize * 2f;

        for (var i = 0; i < _cells.Count; i++)
        {
            if (!_cells[i].Alive)
            {
                continue;
            }

            for (var j = i + 1; j < _cells.Count; j++)
            {
                if (!_cells[j].Alive)
                {
                    continue;
                }

                var delta = _cells[j].Position - _cells[i].Position;
                var distSq = delta.LengthSq;

                if (distSq < minDist * minDist && distSq > 1e-6f)
                {
                    var dist   = MathF.Sqrt(distSq);
                    var normal = delta / dist;
                    var overlap = (minDist - dist) * 0.5f;

                    _cells[i].Position = _cells[i].Position - normal * overlap;
                    _cells[j].Position = _cells[j].Position + normal * overlap;

                    var relVel  = _cells[j].Velocity - _cells[i].Velocity;
                    var impulse = Vec2.Dot(relVel, normal) * SimConstants.CollisionRestitution;
                    if (impulse < 0)
                    {
                        _cells[i].Velocity = _cells[i].Velocity - normal * impulse;
                        _cells[j].Velocity = _cells[j].Velocity + normal * impulse;
                    }
                }
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
        // Spawn twice as often in nutritious zone
        var nutriZone = _zones.FirstOrDefault(z => z.Type == EnvironmentType.Nutritious);
        if (nutriZone is not null && _rng.NextDouble() < 0.3)
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

    private Vec2? FindNearestFood(Vec2 origin)
    {
        if (_foods.Count == 0)
        {
            return null;
        }

        var best     = _foods[0];
        var bestDist = origin.DistanceSq(best);
        for (var i = 1; i < _foods.Count; i++)
        {
            var d = origin.DistanceSq(_foods[i]);
            if (d < bestDist)
            {
                bestDist = d;
                best     = _foods[i];
            }
        }

        return best;
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

    private static List<EnvironmentZone> BuildDefaultZones(float w, float h) =>
    [
        new EnvironmentZone { X = 0,           Y = 0,           W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Viscous    },
        new EnvironmentZone { X = w * 0.7f,    Y = 0,           W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Toxic      },
        new EnvironmentZone { X = 0,           Y = h * 0.6f,    W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Turbulent  },
        new EnvironmentZone { X = w * 0.7f,    Y = h * 0.6f,    W = w * 0.3f, H = h * 0.4f, Type = EnvironmentType.Nutritious }
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

    private static float LerpAngle(float from, float to, float t)
    {
        var diff = WrapAngle(to - from);
        return from + diff * Math.Clamp(t, 0f, 1f);
    }

    // ── result helpers ────────────────────────────────────────────────────────

    public CellState? GetWinner()
    {
        if (_cells.Count == 0)
        {
            return null;
        }

        var ordered = _cells
            .OrderByDescending(c => c.FoodCollectedForDup)
            .ThenByDescending(c => c.Food)
            .ToList();
        return ordered[0];
    }

    public int AliveCellCount => _cells.Count(c => c.Alive);
}
