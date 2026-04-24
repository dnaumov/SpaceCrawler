using System.Linq;
using Godot;

/// <summary>
/// Godot rendering and input layer for the cell-biology simulation.
/// All game rules and state live in <see cref="SimulationEngine"/>;
/// this class is responsible only for:
///   - reading player input and forwarding it to the engine
///   - drawing the arena, environment zones, food items, and cells
///   - managing the match timer and HUD
/// </summary>
public partial class GameplaySimulation : Node2D
{
    // ── Godot exports ─────────────────────────────────────────────────────────
    [Export] public float   MatchDuration     { get; set; } = 120.0f;
    [Export] public Vector2 ArenaSize         { get; set; } = new(1152.0f, 648.0f);
    [Export] public int     AiCompetitorCount { get; set; } = 3;
    [Export] public float   FoodSpawnInterval { get; set; } = 0.8f;
    [Export] public int     MaxFood           { get; set; } = 80;

    // S = 16 pixels per simulation unit (console uses S = 1)
    private const float UnitScale = 16.0f;

    private SimulationEngine _engine = null!;
    private CellState?       _playerCell;

    // Cell display colours indexed by cell instance (assigned at creation)
    private readonly System.Collections.Generic.Dictionary<CellState, Color> _cellColors = [];

    private Timer  _matchTimer  = default!;
    private Label  _hudTimer    = default!;
    private Label  _hudStatus   = default!;
    private Label  _hudScoreboard = default!;
    private bool   _matchEnded;
    private readonly RandomNumberGenerator _rng = new();

    // ── Godot lifecycle ───────────────────────────────────────────────────────

    public override void _Ready()
    {
        _matchTimer          = GetNode<Timer>("MatchTimer");
        _matchTimer.Timeout += OnMatchTimeout;

        _rng.Randomize();
        SetupHud();
        StartMatch();
    }

    public override void _Process(double delta)
    {
        if (_matchEnded)
        {
            return;
        }

        // Forward player input to the engine before stepping
        var rawInput = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        _engine.PlayerInputDirection = new Vec2(rawInput.X, rawInput.Y);

        _engine.Step((float)delta);
        UpdateHud();
        QueueRedraw();

        if (_engine.AliveCellCount == 0)
        {
            EndMatch("All cells died.");
        }
    }

    public override void _Draw()
    {
        // Background
        DrawRect(new Rect2(Vector2.Zero, ArenaSize), new Color(0.05f, 0.08f, 0.12f), true);

        // Environment zones
        foreach (var zone in _engine.Zones)
        {
            var zoneColor = zone.Type switch
            {
                EnvironmentType.Viscous    => new Color(0.2f, 0.2f, 0.6f, 0.25f),
                EnvironmentType.Toxic      => new Color(0.3f, 0.6f, 0.2f, 0.25f),
                EnvironmentType.Turbulent  => new Color(0.6f, 0.4f, 0.1f, 0.25f),
                EnvironmentType.Nutritious => new Color(0.6f, 0.6f, 0.1f, 0.25f),
                _                          => new Color(0, 0, 0, 0)
            };

            var rect = new Rect2(zone.X, zone.Y, zone.W, zone.H);
            DrawRect(rect, zoneColor, true);
            DrawRect(rect, zoneColor with { A = 0.5f }, false);
        }

        // Food items
        var foodHalf = SimConstants.FoodHalfSize * UnitScale;
        foreach (var food in _engine.Foods)
        {
            var pos = V(food);
            DrawRect(new Rect2(pos - Vector2.One * foodHalf, Vector2.One * (foodHalf * 2f)),
                     new Color(0.4f, 1.0f, 0.4f), true);
        }

        // Cells
        var cellHalf = SimConstants.CellHalfSize * UnitScale;
        foreach (var cell in _engine.Cells)
        {
            var color = GetCellColor(cell);
            if (!cell.Alive)
            {
                color.A = 0.3f;
            }
            else if (cell.Food < 0f)
            {
                color = color.Lerp(new Color(1f, 0.2f, 0.2f),
                                   Mathf.Clamp(-cell.Food / 4f, 0f, 1f));
            }

            DrawSetTransform(V(cell.Position), cell.Rotation);
            DrawRect(new Rect2(-Vector2.One * cellHalf, Vector2.One * (cellHalf * 2f)), color, true);
            // Orientation marker (forward direction)
            DrawLine(Vector2.Zero, new Vector2(0f, -cellHalf * 1.2f),
                     Colors.White with { A = 0.6f }, 2f);
            DrawSetTransform(Vector2.Zero, 0f);
        }
    }

    // ── match management ──────────────────────────────────────────────────────

    private void StartMatch()
    {
        _matchEnded = false;
        _cellColors.Clear();

        // Create engine in pixel-space coordinates (unitScale=16 maps S-units to pixels)
        _engine = new SimulationEngine(
            arenaW:    ArenaSize.X,
            arenaH:    ArenaSize.Y,
            unitScale: UnitScale,
            seed:      (int)_rng.Randi());

        _engine.FoodSpawnInterval = FoodSpawnInterval;
        _engine.MaxFood           = MaxFood;

        // Player cell
        var playerBlueprint = TryLoadPlayerBlueprint() ?? CellBlueprint.Default();
        var playerPos = new Vec2(ArenaSize.X * 0.5f, ArenaSize.Y * 0.5f);
        _playerCell   = _engine.CreateCell("Player", playerPos, playerBlueprint, isPlayer: true);
        _cellColors[_playerCell] = new Color(0.35f, 0.75f, 1.0f);

        // AI competitors
        for (var i = 0; i < AiCompetitorCount; i++)
        {
            var pos = new Vec2(
                _rng.RandfRange(SimConstants.CellHalfSize * UnitScale * 2,
                                ArenaSize.X - SimConstants.CellHalfSize * UnitScale * 2),
                _rng.RandfRange(SimConstants.CellHalfSize * UnitScale * 2,
                                ArenaSize.Y - SimConstants.CellHalfSize * UnitScale * 2));
            var cell = _engine.CreateCell($"AI {i + 1}", pos, GenerateAiBlueprint());
            _cellColors[cell] = Color.FromHsv(_rng.Randf(), 0.65f, 0.95f);
        }

        _hudStatus.Text = "Collect food to duplicate. Survive the drain.";
        _matchTimer.Stop();
        _matchTimer.WaitTime = MatchDuration;
        _matchTimer.OneShot  = true;
        _matchTimer.Start();
        UpdateHud();
    }

    private CellBlueprint GenerateAiBlueprint()
    {
        var grid = new OrganelleType[16];
        foreach (var idx in CellBlueprint.NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        var freeSlots = System.Linq.Enumerable.Range(0, 16)
            .Where(i => !CellBlueprint.NucleusIndices.Contains(i))
            .ToList();

        var pool = new[]
        {
            OrganelleType.RandomEngine, OrganelleType.EffectiveEngine,
            OrganelleType.Engine, OrganelleType.Mitochondria,
            OrganelleType.FoodGradientDetector, OrganelleType.SlipperyMembrane
        };

        var count = (int)_rng.RandiRange(2, 8);
        for (var i = 0; i < count && freeSlots.Count > 0; i++)
        {
            var slotIdx   = (int)_rng.RandiRange(0, freeSlots.Count - 1);
            var organelle = pool[(int)_rng.RandiRange(0, pool.Length - 1)];
            grid[freeSlots[slotIdx]] = organelle;
            freeSlots.RemoveAt(slotIdx);
        }

        return new CellBlueprint(grid);
    }

    private void OnMatchTimeout() => EndMatch();

    private void EndMatch(string reason = "")
    {
        if (_matchEnded)
        {
            return;
        }

        _matchEnded = true;
        _matchTimer.Stop();

        var winner = _engine.GetWinner();
        if (winner is null)
        {
            _hudStatus.Text = "Match ended. No winner.";
        }
        else
        {
            var msg = $"Winner: {winner.Name}  (food={winner.Food:F1}, dups={winner.DuplicationCount})";
            if (!string.IsNullOrEmpty(reason))
            {
                msg += $" — {reason}";
            }

            _hudStatus.Text = msg;
        }

        UpdateHud();
    }

    // ── HUD ───────────────────────────────────────────────────────────────────

    private void SetupHud()
    {
        var canvas = new CanvasLayer();
        AddChild(canvas);

        _hudTimer       = new Label { Position = new Vector2(12f, 8f) };
        _hudStatus      = new Label { Position = new Vector2(12f, 32f) };
        _hudScoreboard  = new Label { Position = new Vector2(12f, 56f) };

        canvas.AddChild(_hudTimer);
        canvas.AddChild(_hudStatus);
        canvas.AddChild(_hudScoreboard);

        var legend = new Label
        {
            Position = new Vector2(ArenaSize.X - 220f, 8f),
            Text = "Zones: [Viscous] [Toxic] [Turbulent] [Nutritious]"
        };
        canvas.AddChild(legend);
    }

    private void UpdateHud()
    {
        if (_matchEnded)
        {
            _hudTimer.Text = "Time left: 0.0s";
        }
        else
        {
            _hudTimer.Text = $"Time left: {_matchTimer.TimeLeft:F1}s  |  " +
                             $"Cells: {_engine.AliveCellCount}/{_engine.Cells.Count}  |  " +
                             $"Food: {_engine.Foods.Count}";
        }

        var standings = _engine.Cells
            .OrderByDescending(c => c.FoodCollectedForDup)
            .ThenByDescending(c => c.Food)
            .ToList();

        var lines = new System.Collections.Generic.List<string>
        {
            "Standings (dup-food / food-reserve):"
        };

        foreach (var cell in standings)
        {
            var suffix = cell.Alive ? string.Empty : " [DEAD]";
            lines.Add($"- {cell.Name}: {cell.FoodCollectedForDup} / {cell.Food:F1} " +
                      $"[{cell.Blueprint.ElementCount} elements]{suffix}");
        }

        _hudScoreboard.Text = string.Join('\n', lines);
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    /// <summary>Convert simulation Vec2 to Godot Vector2.</summary>
    private static Vector2 V(Vec2 v) => new(v.X, v.Y);

    private Color GetCellColor(CellState cell)
    {
        if (_cellColors.TryGetValue(cell, out var c))
        {
            return c;
        }

        // Daughter cells get a tinted version of their parent's color
        var newColor = Color.FromHsv(_rng.Randf(), 0.65f, 0.95f);
        _cellColors[cell] = newColor;
        return newColor;
    }

    // ── blueprint loading ─────────────────────────────────────────────────────

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

        foreach (var idx in CellBlueprint.NucleusIndices)
        {
            grid[idx] = OrganelleType.Nucleus;
        }

        return new CellBlueprint(grid);
    }
}
