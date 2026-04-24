using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>
/// Main gameplay simulation implementing the full cell-biology ruleset.
///
/// Key constants:
///   T  = 10 s   — tick interval
///   C  = 1 food — passive drain per T
///   S  = 16 px  — base size unit; cell = 2S×2S, food = 1S×1S
/// </summary>
public partial class GameplaySimulation : Node2D
{
    // ── tuneable exports ──────────────────────────────────────────────────────
    [Export] public float MatchDuration        { get; set; } = 120.0f;
    [Export] public Vector2 ArenaSize          { get; set; } = new(1152.0f, 648.0f);
    [Export] public int AiCompetitorCount      { get; set; } = 3;
    [Export] public float FoodSpawnInterval    { get; set; } = 0.8f;
    [Export] public int MaxFood                { get; set; } = 80;

    // ── physical constants ────────────────────────────────────────────────────
    private const float S                 = 16.0f;   // base size unit
    private const float CellHalfSize      = S;       // half of 2S
    private const float FoodHalfSize      = S * 0.5f;
    private const float TickInterval      = 10.0f;   // T
    private const float PassiveDrainPerT  = 1.0f;    // C
    private const float DragBase          = 2.5f;    // velocity lost per second (normal env)
    private const float AngularDrag       = 1.8f;    // angular velocity lost per second
    private const float RandomPush        = 18.0f;   // max random impulse per second
    private const float EngineSpeed       = 2.0f * S;
    private const float EffEngineSpeed    = 1.0f * S;
    private const float RandomEngineSpeed = 2.0f * S;
    private const float CellCollisionRestitution = 0.4f;
    private const float ChloroplastInterval = 40.0f; // produces 1 food every 40 s
    private const float SlipperyFoodInterval = 20.0f; // cost 1 food per 2T
    private const float ToxinFoodInterval    = 20.0f;

    // ── gradient grid ─────────────────────────────────────────────────────────
    private const int GradGridW = 24;
    private const int GradGridH = 14;
    private readonly float[] _foodGrad  = new float[GradGridW * GradGridH];
    private readonly float[] _cellGrad  = new float[GradGridW * GradGridH];
    private readonly float[] _toxicGrad = new float[GradGridW * GradGridH];
    private float _gradRecalcTimer;
    private const float GradRecalcInterval = 0.5f; // recalculate twice per second

    // ── environment zones ─────────────────────────────────────────────────────
    private enum EnvType { Normal, Viscous, Toxic, Turbulent, Nutritious }

    private sealed class EnvZone
    {
        public Rect2 Rect;
        public EnvType Type;
    }

    private readonly List<EnvZone> _envZones = [];

    // ── cell data ─────────────────────────────────────────────────────────────
    private sealed class Cell
    {
        public string Name = string.Empty;
        public bool IsPlayer;
        public Vector2 Position;
        public float Rotation;           // radians
        public Vector2 Velocity;
        public float AngularVelocity;
        public float Food;               // current food reserve
        public int FoodCollectedForDup;  // counter towards duplication
        public CellBlueprint Blueprint  = CellBlueprint.Default();
        public float TickAccum;          // time towards next T tick
        public float ChloroAccum;        // time towards chloroplast food
        public float SlipperyAccum;      // time towards slippery membrane cost
        public float ToxinAccum;         // time towards toxin producer cost
        public bool Alive = true;
        public Color Color;
        public bool SensorActive;        // true when any sensor fired this tick
    }

    private readonly RandomNumberGenerator _rng = new();
    private readonly List<Cell> _cells    = [];
    private readonly List<Vector2> _foods = [];

    private Timer _matchTimer = default!;
    private Label _hudTimer = default!;
    private Label _hudStatus = default!;
    private Label _hudScoreboard = default!;

    private float _foodSpawnCooldown;
    private bool _matchEnded;
    private Cell? _finalWinner;

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        _matchTimer = GetNode<Timer>("MatchTimer");
        _matchTimer.Timeout += OnMatchTimeout;

        _rng.Randomize();
        SetupEnvironmentZones();
        SetupHud();
        StartMatch();
    }

    public override void _Process(double delta)
    {
        if (_matchEnded)
        {
            return;
        }

        var dt = (float)delta;

        _foodSpawnCooldown -= dt;
        if (_foodSpawnCooldown <= 0.0f)
        {
            _foodSpawnCooldown += FoodSpawnInterval;
            SpawnFood();
        }

        // Recalculate gradient field periodically
        _gradRecalcTimer -= dt;
        if (_gradRecalcTimer <= 0.0f)
        {
            _gradRecalcTimer = GradRecalcInterval;
            RecalculateGradients();
        }

        UpdateCells(dt);
        ResolveCollisions();
        UpdateHud();
        QueueRedraw();

        if (AliveCellsCount() == 0)
        {
            EndMatch("All cells died.");
        }
    }

    public override void _Draw()
    {
        DrawRect(new Rect2(Vector2.Zero, ArenaSize), new Color(0.05f, 0.08f, 0.12f), true);

        // Environment zones
        foreach (var zone in _envZones)
        {
            var zoneColor = zone.Type switch
            {
                EnvType.Viscous    => new Color(0.2f, 0.2f, 0.6f, 0.25f),
                EnvType.Toxic      => new Color(0.3f, 0.6f, 0.2f, 0.25f),
                EnvType.Turbulent  => new Color(0.6f, 0.4f, 0.1f, 0.25f),
                EnvType.Nutritious => new Color(0.6f, 0.6f, 0.1f, 0.25f),
                _                  => new Color(0, 0, 0, 0)
            };

            DrawRect(zone.Rect, zoneColor, true);
            DrawRect(zone.Rect, zoneColor with { A = 0.5f }, false);
        }

        // Food items
        foreach (var food in _foods)
        {
            var foodRect = new Rect2(food - Vector2.One * FoodHalfSize, Vector2.One * (FoodHalfSize * 2.0f));
            DrawRect(foodRect, new Color(0.4f, 1.0f, 0.4f), true);
        }

        // Cells (drawn as rotated squares)
        foreach (var cell in _cells)
        {
            var color = cell.Color;
            if (!cell.Alive)
            {
                color.A = 0.3f;
            }
            else if (cell.Food < 0.0f)
            {
                color = color.Lerp(new Color(1.0f, 0.2f, 0.2f), Mathf.Clamp(-cell.Food / 4.0f, 0.0f, 1.0f));
            }

            DrawSetTransform(cell.Position, cell.Rotation);
            DrawRect(new Rect2(-Vector2.One * CellHalfSize, Vector2.One * (CellHalfSize * 2.0f)), color, true);
            // Orientation marker
            DrawLine(Vector2.Zero, new Vector2(0.0f, -CellHalfSize * 1.2f), Colors.White with { A = 0.6f }, 2.0f);
            DrawSetTransform(Vector2.Zero, 0.0f);
        }
    }

    // ── match setup ───────────────────────────────────────────────────────────

    private void StartMatch()
    {
        _matchEnded = false;
        _finalWinner = null;
        _foodSpawnCooldown = 0.0f;
        _foods.Clear();
        _cells.Clear();
        _gradRecalcTimer = 0.0f;

        var playerBlueprint = TryLoadPlayerBlueprint() ?? CellBlueprint.Default();
        _cells.Add(CreateCell("Player", true, ArenaSize * 0.5f, playerBlueprint, new Color(0.35f, 0.75f, 1.0f)));

        for (var i = 0; i < AiCompetitorCount; i++)
        {
            var pos = new Vector2(
                _rng.RandfRange(CellHalfSize * 2, ArenaSize.X - CellHalfSize * 2),
                _rng.RandfRange(CellHalfSize * 2, ArenaSize.Y - CellHalfSize * 2)
            );
            _cells.Add(CreateCell($"AI {i + 1}", false, pos, GenerateAiBlueprint(), Color.FromHsv(_rng.Randf(), 0.65f, 0.95f)));
        }

        _hudStatus.Text = "Collect food to duplicate. Survive the drain.";
        _matchTimer.Stop();
        _matchTimer.WaitTime = MatchDuration;
        _matchTimer.OneShot = true;
        _matchTimer.Start();
        UpdateHud();
    }

    private static Cell CreateCell(string name, bool isPlayer, Vector2 pos, CellBlueprint blueprint, Color color)
    {
        return new Cell
        {
            Name = name,
            IsPlayer = isPlayer,
            Position = pos,
            Rotation = 0.0f,
            Velocity = Vector2.Zero,
            AngularVelocity = 0.0f,
            Food = blueprint.ElementCount * 0.5f,  // start with half duplication food
            Blueprint = blueprint,
            Color = color
        };
    }

    private CellBlueprint GenerateAiBlueprint()
    {
        var grid = new OrganelleType[16];
        foreach (var idx in CellBlueprint.NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        // Place 2-8 random organelles in non-nucleus slots
        var freeSlots = Enumerable.Range(0, 16).Where(i => !CellBlueprint.NucleusIndices.Contains(i)).ToList();
        var count = _rng.RandiRange(2, 8);
        var organellePool = new[]
        {
            OrganelleType.RandomEngine,
            OrganelleType.EffectiveEngine,
            OrganelleType.Engine,
            OrganelleType.Mitochondria,
            OrganelleType.FoodGradientDetector,
            OrganelleType.SlipperyMembrane
        };

        for (var i = 0; i < count && freeSlots.Count > 0; i++)
        {
            var slotIdx = _rng.RandiRange(0, freeSlots.Count - 1);
            var organelle = organellePool[_rng.RandiRange(0, organellePool.Length - 1)];
            grid[freeSlots[slotIdx]] = organelle;
            freeSlots.RemoveAt(slotIdx);
        }

        return new CellBlueprint(grid);
    }

    // ── update loop ───────────────────────────────────────────────────────────

    private void UpdateCells(float dt)
    {
        var toAdd = new List<Cell>();

        foreach (var cell in _cells)
        {
            if (!cell.Alive)
            {
                continue;
            }

            var env = GetDominantEnvironment(cell.Position);

            // ── drag ──────────────────────────────────────────────────────────
            var dragMult = env == EnvType.Viscous ? 2.0f : 1.0f;
            if (cell.Blueprint.SlipperyMembraneCount > 0)
            {
                dragMult *= 0.5f;
            }

            var drag = DragBase * dragMult;
            cell.Velocity = cell.Velocity.MoveToward(Vector2.Zero, drag * dt);
            cell.AngularVelocity = Mathf.MoveToward(cell.AngularVelocity, 0.0f, AngularDrag * dt);

            // ── passive random movement ────────────────────────────────────────
            var turbMult = env == EnvType.Turbulent ? 2.0f : 1.0f;
            var randPush = RandomPush * turbMult;
            cell.Velocity += new Vector2(
                _rng.RandfRange(-randPush, randPush),
                _rng.RandfRange(-randPush, randPush)
            ) * dt;
            cell.AngularVelocity += _rng.RandfRange(-0.4f, 0.4f) * turbMult * dt;

            // ── player input ──────────────────────────────────────────────────
            if (cell.IsPlayer)
            {
                var input = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
                if (input != Vector2.Zero)
                {
                    cell.Velocity += input.Normalized() * EngineSpeed * dt * 10.0f;
                    cell.Rotation = Mathf.LerpAngle(cell.Rotation, Mathf.Atan2(input.Y, input.X) + Mathf.Pi * 0.5f, dt * 5.0f);
                }
            }
            else
            {
                ApplyAiEngines(cell, dt);
            }

            // ── integrate ─────────────────────────────────────────────────────
            cell.Position += cell.Velocity * dt;
            cell.Rotation  = Mathf.Wrap(cell.Rotation + cell.AngularVelocity * dt, -Mathf.Pi, Mathf.Pi);
            cell.Position  = new Vector2(
                Mathf.Clamp(cell.Position.X, CellHalfSize, ArenaSize.X - CellHalfSize),
                Mathf.Clamp(cell.Position.Y, CellHalfSize, ArenaSize.Y - CellHalfSize)
            );

            // ── food collection ───────────────────────────────────────────────
            CollectFood(cell, env);

            // ── per-tick processing ───────────────────────────────────────────
            cell.TickAccum += dt;
            if (cell.TickAccum >= TickInterval)
            {
                cell.TickAccum -= TickInterval;
                ProcessTick(cell);
            }

            // ── chloroplast ───────────────────────────────────────────────────
            if (cell.Blueprint.ChloroplastCount > 0)
            {
                cell.ChloroAccum += dt;
                if (cell.ChloroAccum >= ChloroplastInterval)
                {
                    cell.ChloroAccum -= ChloroplastInterval;
                    cell.Food += cell.Blueprint.ChloroplastCount;
                }
            }

            // ── slippery membrane upkeep ──────────────────────────────────────
            if (cell.Blueprint.SlipperyMembraneCount > 0)
            {
                cell.SlipperyAccum += dt;
                if (cell.SlipperyAccum >= SlipperyFoodInterval)
                {
                    cell.SlipperyAccum -= SlipperyFoodInterval;
                    cell.Food -= cell.Blueprint.SlipperyMembraneCount;
                }
            }

            // ── toxin producer upkeep ─────────────────────────────────────────
            if (cell.Blueprint.ToxinProducerCount > 0)
            {
                cell.ToxinAccum += dt;
                if (cell.ToxinAccum >= ToxinFoodInterval)
                {
                    cell.ToxinAccum -= ToxinFoodInterval;
                    cell.Food -= cell.Blueprint.ToxinProducerCount;
                }
            }

            // ── death check ───────────────────────────────────────────────────
            if (cell.Food <= cell.Blueprint.DeathFoodThreshold)
            {
                cell.Alive = false;
            }

            // ── duplication ───────────────────────────────────────────────────
            var dupThreshold = Math.Max(1, cell.Blueprint.FoodForDuplication);
            if (cell.Alive && cell.FoodCollectedForDup >= dupThreshold)
            {
                cell.FoodCollectedForDup = 0;
                var offset = new Vector2(_rng.RandfRange(-CellHalfSize * 3, CellHalfSize * 3),
                                         _rng.RandfRange(-CellHalfSize * 3, CellHalfSize * 3));
                var daughter = CreateCell(cell.Name + "'", cell.IsPlayer, cell.Position + offset, cell.Blueprint, cell.Color);
                daughter.Food = cell.Food * 0.5f;
                cell.Food    *= 0.5f;
                toAdd.Add(daughter);
            }
        }

        _cells.AddRange(toAdd);
    }

    private void ProcessTick(Cell cell)
    {
        var env = GetDominantEnvironment(cell.Position);
        var toxicMult = env == EnvType.Toxic ? 2.0f : 1.0f;
        cell.Food -= PassiveDrainPerT * toxicMult;

        // Re-evaluate sensor state for the coming tick
        cell.SensorActive = EvaluateSensors(cell);
    }

    private bool EvaluateSensors(Cell cell)
    {
        if (!cell.Blueprint.HasSensor)
        {
            return false;
        }

        if (cell.Blueprint.HasFoodSensor)
        {
            // Food gradient detector: active when food gradient is positive in cell direction
            var gx = (int)(cell.Position.X / ArenaSize.X * GradGridW);
            var gy = (int)(cell.Position.Y / ArenaSize.Y * GradGridH);
            gx = Mathf.Clamp(gx, 0, GradGridW - 1);
            gy = Mathf.Clamp(gy, 0, GradGridH - 1);

            if (_foods.Count > 0 && _foodGrad[gy * GradGridW + gx] > 0.001f)
            {
                return true;
            }

            // Food vision: check if any food is in front of the cell
            var ahead = cell.Position + new Vector2(0, -CellHalfSize * 4.0f).Rotated(cell.Rotation);
            foreach (var food in _foods)
            {
                if (food.DistanceSquaredTo(ahead) < (CellHalfSize * 3.0f) * (CellHalfSize * 3.0f))
                {
                    return true;
                }
            }
        }

        if (cell.Blueprint.HasCellSensor)
        {
            var gx = (int)(cell.Position.X / ArenaSize.X * GradGridW);
            var gy = (int)(cell.Position.Y / ArenaSize.Y * GradGridH);
            gx = Mathf.Clamp(gx, 0, GradGridW - 1);
            gy = Mathf.Clamp(gy, 0, GradGridH - 1);
            if (_cellGrad[gy * GradGridW + gx] > 0.001f)
            {
                return true;
            }
        }

        return false;
    }

    private void ApplyAiEngines(Cell cell, float dt)
    {
        // Determine target direction (toward nearest food if any, else wander)
        Vector2 targetDir;
        var nearest = FindNearestFood(cell.Position);
        if (nearest.HasValue)
        {
            targetDir = (nearest.Value - cell.Position).Normalized();
        }
        else
        {
            targetDir = new Vector2(Mathf.Cos(cell.Rotation - Mathf.Pi * 0.5f),
                                     Mathf.Sin(cell.Rotation - Mathf.Pi * 0.5f));
        }

        // Rotate toward target
        var targetAngle = Mathf.Atan2(targetDir.Y, targetDir.X) + Mathf.Pi * 0.5f;
        cell.Rotation = Mathf.LerpAngle(cell.Rotation, targetAngle, dt * 3.0f);

        var forward = new Vector2(0.0f, -1.0f).Rotated(cell.Rotation);

        // Random engine: 50% chance per T, activate ~once per T converted to per-frame probability
        if (cell.Blueprint.RandomEngineCount > 0)
        {
            if (_rng.Randf() < 0.5f * dt / TickInterval)
            {
                cell.Velocity += forward * RandomEngineSpeed * cell.Blueprint.RandomEngineCount;
                cell.Food -= 2.0f * cell.Blueprint.RandomEngineCount;
            }
        }

        // Effective engine: needs sensor, else 50% chance
        if (cell.Blueprint.EffectiveEngineCount > 0)
        {
            var activate = cell.SensorActive || _rng.Randf() < 0.5f * dt / TickInterval;
            if (activate)
            {
                cell.Velocity += forward * EffEngineSpeed * cell.Blueprint.EffectiveEngineCount;
                cell.Food -= 1.0f * cell.Blueprint.EffectiveEngineCount;
            }
        }

        // Engine: needs sensor, else 50% chance
        if (cell.Blueprint.EngineCount > 0)
        {
            var activate = cell.SensorActive || _rng.Randf() < 0.5f * dt / TickInterval;
            if (activate)
            {
                cell.Velocity += forward * EngineSpeed * cell.Blueprint.EngineCount;
                cell.Food -= 3.0f * cell.Blueprint.EngineCount;
            }
        }
    }

    private void CollectFood(Cell cell, EnvType env)
    {
        var nutritiousMult = env == EnvType.Nutritious ? 2.0f : 1.0f;
        var touchDistSq = (CellHalfSize + FoodHalfSize) * (CellHalfSize + FoodHalfSize);

        for (var i = _foods.Count - 1; i >= 0; i--)
        {
            if (cell.Position.DistanceSquaredTo(_foods[i]) <= touchDistSq)
            {
                _foods.RemoveAt(i);
                cell.Food += 1.0f * nutritiousMult;
                cell.FoodCollectedForDup += 1;
            }
        }
    }

    private void ResolveCollisions()
    {
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
                var minDist = CellHalfSize * 2.0f;
                var distSq = delta.LengthSquared();

                if (distSq < minDist * minDist && distSq > 0.001f)
                {
                    var dist = Mathf.Sqrt(distSq);
                    var normal = delta / dist;
                    var overlap = (minDist - dist) * 0.5f;

                    // Separate
                    _cells[i].Position -= normal * overlap;
                    _cells[j].Position += normal * overlap;

                    // Impulse
                    var relVel = _cells[j].Velocity - _cells[i].Velocity;
                    var impulse = Vector2.Dot(relVel, normal) * CellCollisionRestitution;
                    if (impulse < 0)
                    {
                        _cells[i].Velocity -= normal * impulse;
                        _cells[j].Velocity += normal * impulse;
                    }
                }
            }
        }
    }

    // ── environment ───────────────────────────────────────────────────────────

    private void SetupEnvironmentZones()
    {
        _envZones.Clear();
        // Fixed zone layout: corners/edges have different environments
        var w = ArenaSize.X;
        var h = ArenaSize.Y;

        _envZones.Add(new EnvZone { Rect = new Rect2(0, 0, w * 0.3f, h * 0.4f),              Type = EnvType.Viscous });
        _envZones.Add(new EnvZone { Rect = new Rect2(w * 0.7f, 0, w * 0.3f, h * 0.4f),       Type = EnvType.Toxic });
        _envZones.Add(new EnvZone { Rect = new Rect2(0, h * 0.6f, w * 0.3f, h * 0.4f),       Type = EnvType.Turbulent });
        _envZones.Add(new EnvZone { Rect = new Rect2(w * 0.7f, h * 0.6f, w * 0.3f, h * 0.4f), Type = EnvType.Nutritious });
    }

    private EnvType GetDominantEnvironment(Vector2 pos)
    {
        foreach (var zone in _envZones)
        {
            // "More than half of the cell" in the zone – approximate with center point
            if (zone.Rect.HasPoint(pos))
            {
                return zone.Type;
            }
        }

        return EnvType.Normal;
    }

    // ── gradient field ────────────────────────────────────────────────────────

    private void RecalculateGradients()
    {
        var cellW = ArenaSize.X / GradGridW;
        var cellH = ArenaSize.Y / GradGridH;

        for (var gy = 0; gy < GradGridH; gy++)
        {
            for (var gx = 0; gx < GradGridW; gx++)
            {
                var cx = (gx + 0.5f) * cellW;
                var cy = (gy + 0.5f) * cellH;

                float foodSum = 0.0f;
                foreach (var food in _foods)
                {
                    var dx = food.X - cx;
                    var dy = food.Y - cy;
                    var distSq = dx * dx + dy * dy + 1.0f;
                    foodSum += 1.0f / distSq;
                }

                float cellSum = 0.0f;
                foreach (var cell in _cells)
                {
                    if (!cell.Alive)
                    {
                        continue;
                    }

                    var dx = cell.Position.X - cx;
                    var dy = cell.Position.Y - cy;
                    var distSq = dx * dx + dy * dy + 1.0f;
                    cellSum += 1.0f / distSq;
                }

                // Toxic gradient: sum over toxic env zone presence
                var toxicVal = 0.0f;
                foreach (var zone in _envZones)
                {
                    if (zone.Type == EnvType.Toxic)
                    {
                        var zoneCenter = zone.Rect.Position + zone.Rect.Size * 0.5f;
                        var dx = zoneCenter.X - cx;
                        var dy = zoneCenter.Y - cy;
                        var distSq = dx * dx + dy * dy + 1.0f;
                        toxicVal += 1.0f / distSq;
                    }
                }

                var idx = gy * GradGridW + gx;
                _foodGrad[idx]  = foodSum;
                _cellGrad[idx]  = cellSum;
                _toxicGrad[idx] = toxicVal;
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

        // Prefer nutritious zone spawn (twice as often there)
        Vector2 pos;
        if (_envZones.Count > 0 && _rng.Randf() < 0.3f)
        {
            var nutriZone = _envZones.FirstOrDefault(z => z.Type == EnvType.Nutritious);
            if (nutriZone is not null)
            {
                pos = new Vector2(
                    _rng.RandfRange(nutriZone.Rect.Position.X, nutriZone.Rect.End.X),
                    _rng.RandfRange(nutriZone.Rect.Position.Y, nutriZone.Rect.End.Y)
                );
            }
            else
            {
                pos = RandomArenaPos();
            }
        }
        else
        {
            pos = RandomArenaPos();
        }

        _foods.Add(pos);
    }

    private Vector2 RandomArenaPos() => new(
        _rng.RandfRange(FoodHalfSize, ArenaSize.X - FoodHalfSize),
        _rng.RandfRange(FoodHalfSize, ArenaSize.Y - FoodHalfSize)
    );

    private Vector2? FindNearestFood(Vector2 origin)
    {
        if (_foods.Count == 0)
        {
            return null;
        }

        var nearest = _foods[0];
        var nearestDist = origin.DistanceSquaredTo(nearest);
        for (var i = 1; i < _foods.Count; i++)
        {
            var d = origin.DistanceSquaredTo(_foods[i]);
            if (d < nearestDist)
            {
                nearestDist = d;
                nearest = _foods[i];
            }
        }

        return nearest;
    }

    // ── match end ─────────────────────────────────────────────────────────────

    private int AliveCellsCount() => _cells.Count(c => c.Alive);

    private void OnMatchTimeout() => EndMatch();

    private void EndMatch(string reason = "")
    {
        if (_matchEnded)
        {
            return;
        }

        _matchEnded = true;
        _matchTimer.Stop();
        _finalWinner = CalculateWinner();

        if (_finalWinner is null)
        {
            _hudStatus.Text = "Match ended. No winner.";
        }
        else
        {
            var msg = $"Winner: {_finalWinner.Name} (food={_finalWinner.Food:F1}, dups={_finalWinner.FoodCollectedForDup})";
            if (!string.IsNullOrEmpty(reason))
            {
                msg += $" — {reason}";
            }

            _hudStatus.Text = msg;
        }

        UpdateHud();
    }

    private Cell? CalculateWinner()
    {
        if (_cells.Count == 0)
        {
            return null;
        }

        // Primary: most food collected for duplication (proxy for fitness)
        var maxDup = _cells.Max(c => c.FoodCollectedForDup);
        var dupTied = _cells.Where(c => c.FoodCollectedForDup == maxDup).ToList();
        if (dupTied.Count == 1)
        {
            return dupTied[0];
        }

        // Tie-break: highest current food
        var maxFood = dupTied.Max(c => c.Food);
        var foodTied = dupTied.Where(c => Mathf.IsEqualApprox(c.Food, maxFood)).ToList();
        if (foodTied.Count == 1)
        {
            return foodTied[0];
        }

        // Random tie-break
        return foodTied[_rng.RandiRange(0, foodTied.Count - 1)];
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void SetupHud()
    {
        var canvas = new CanvasLayer();
        AddChild(canvas);

        _hudTimer = new Label { Position = new Vector2(12.0f, 8.0f) };
        canvas.AddChild(_hudTimer);

        _hudStatus = new Label { Position = new Vector2(12.0f, 32.0f) };
        canvas.AddChild(_hudStatus);

        _hudScoreboard = new Label { Position = new Vector2(12.0f, 56.0f) };
        canvas.AddChild(_hudScoreboard);

        // Environment legend
        var legend = new Label
        {
            Position = new Vector2(ArenaSize.X - 220.0f, 8.0f),
            Text = "Zones: [Viscous] [Toxic] [Turbulent] [Nutritious]"
        };
        canvas.AddChild(legend);
    }

    private void UpdateHud()
    {
        _hudTimer.Text = _matchEnded
            ? "Time left: 0.0s"
            : $"Time left: {_matchTimer.TimeLeft:F1}s  |  Cells: {AliveCellsCount()}/{_cells.Count}  |  Food: {_foods.Count}";

        var standings = _cells
            .OrderByDescending(c => c.FoodCollectedForDup)
            .ThenByDescending(c => c.Food)
            .ToList();

        var lines = new List<string> { "Standings (dup-food / food-reserve):" };
        foreach (var cell in standings)
        {
            var suffix = cell.Alive ? string.Empty : " [DEAD]";
            lines.Add($"- {cell.Name}: {cell.FoodCollectedForDup} / {cell.Food:F1} [{cell.Blueprint.ElementCount} elements]{suffix}");
        }

        _hudScoreboard.Text = string.Join('\n', lines);
    }

    // ── player blueprint loading ──────────────────────────────────────────────

    private static CellBlueprint? TryLoadPlayerBlueprint()
    {
        const string path = "user://organism_config.json";
        if (!FileAccess.FileExists(path))
        {
            return null;
        }

        using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
        if (file is null)
        {
            return null;
        }

        var json = new Json();
        if (json.Parse(file.GetAsText()) != Error.Ok)
        {
            return null;
        }

        if (json.Data.VariantType != Variant.Type.Dictionary)
        {
            return null;
        }

        var dict = json.Data.AsGodotDictionary();
        if (!dict.ContainsKey("components"))
        {
            return null;
        }

        var arr = dict["components"].AsGodotArray();
        if (arr.Count < 16)
        {
            return null;
        }

        var grid = new OrganelleType[16];
        for (var i = 0; i < 16; i++)
        {
            grid[i] = OrganelleTypeExtensions.FromSerializedName(arr[i].AsString());
        }

        // Ensure nucleus slots are always nucleus
        foreach (var idx in CellBlueprint.NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        return new CellBlueprint(grid);
    }
}
