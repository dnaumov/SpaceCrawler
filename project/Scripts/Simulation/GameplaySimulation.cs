using System.Linq;
using Godot;

#nullable enable

/// <summary>
/// Godot rendering and input layer for the cell-biology simulation.
/// All game rules and state live in <see cref="SimulationEngine"/>;
/// this class is responsible only for:
///   - drawing the arena, environment zones, food items, and cells
///   - managing the match timer and HUD
/// </summary>
public partial class GameplaySimulation : Node2D
{
	// ── Godot exports ─────────────────────────────────────────────────────────
	[Export] public float   MatchDuration     { get; set; } = 120.0f;
	[Export] public Vector2 ArenaSize         { get; set; } = new(1152.0f, 648.0f);
	[Export] public int     AiCompetitorCount { get; set; } = 3;

	// S = 16 pixels per simulation unit (console uses S = 1)
	private const float UnitScale = 16.0f;
	private const string PlayerConfigPath = "user://organism_config.json";
	private const string AiConfigDirectory = "res://ai_configs";
	private const string BalanceDirectory = "res://balance";

	private static readonly OrganelleType[] AiOrganellePool = System.Enum
		.GetValues<OrganelleType>()
		.Where(type => type is not OrganelleType.Empty and not OrganelleType.Nucleus)
		.ToArray();

	private SimulationEngine _engine = null!;
	private CellState?       _playerCell;

	// Cell display colours indexed by cell instance (assigned at creation)
	private readonly System.Collections.Generic.Dictionary<CellState, Color> _cellColors = [];

	private Timer  _matchTimer  = default!;
	private Label  _hudTimer    = default!;
	private Label  _hudStatus   = default!;
	private RichTextLabel _hudScoreboard = default!;
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
			var color = GetRenderedCellColor(cell);

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
		var balance = LoadSimulationBalance();

		// Create engine in pixel-space coordinates (unitScale=16 maps S-units to pixels)
		_engine = new SimulationEngine(
			arenaW:    ArenaSize.X,
			arenaH:    ArenaSize.Y,
			unitScale: UnitScale,
			seed:      (int)_rng.Randi(),
			balance:   balance);

		// Player cell
		var playerBlueprint = TryLoadBlueprint(PlayerConfigPath) ?? CellBlueprint.Default();
		var playerPos = new Vec2(ArenaSize.X * 0.5f, ArenaSize.Y * 0.5f);
		_playerCell   = _engine.CreateCell("Player", playerPos, playerBlueprint);
		_cellColors[_playerCell] = new Color(0.35f, 0.75f, 1.0f);

		// AI competitors
		for (var i = 0; i < AiCompetitorCount; i++)
		{
			var pos = new Vec2(
				_rng.RandfRange(SimConstants.CellHalfSize * UnitScale * 2,
								ArenaSize.X - SimConstants.CellHalfSize * UnitScale * 2),
				_rng.RandfRange(SimConstants.CellHalfSize * UnitScale * 2,
								ArenaSize.Y - SimConstants.CellHalfSize * UnitScale * 2));
			var configPath = $"{AiConfigDirectory}/ai_{i + 1}.json";
			var blueprint = TryLoadBlueprint(configPath, warnOnInvalid: true)
				?? GenerateRandomAiBlueprint();
			var cell = _engine.CreateCell($"AI {i + 1}", pos, blueprint);
			_cellColors[cell] = Color.FromHsv(_rng.Randf(), 0.65f, 0.95f);
		}

		_hudStatus.Text = "Accumulate biomass to duplicate. Survive the drain.";
		_matchTimer.Stop();
		_matchTimer.WaitTime = MatchDuration;
		_matchTimer.OneShot  = true;
		_matchTimer.Start();
		UpdateHud();
	}

	private CellBlueprint GenerateRandomAiBlueprint()
	{
		var grid = new OrganelleType[16];
		foreach (var idx in CellBlueprint.NucleusIndices)
		{
			grid[idx] = OrganelleType.Nucleus;
		}

		var freeSlots = System.Linq.Enumerable.Range(0, 16)
			.Where(i => !CellBlueprint.NucleusIndices.Contains(i))
			.ToList();

		var count = (int)_rng.RandiRange(2, 8);
		for (var i = 0; i < count && freeSlots.Count > 0; i++)
		{
			var slotIdx   = (int)_rng.RandiRange(0, freeSlots.Count - 1);
			var organelle = AiOrganellePool[(int)_rng.RandiRange(0, AiOrganellePool.Length - 1)];
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
			var msg = $"Winner: {winner.Name}  (copies={winner.DuplicationCount}, " +
				$"biomass={winner.Food:F1})";
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
		_hudScoreboard = new RichTextLabel
		{
			Position = new Vector2(12f, 56f),
			Size = new Vector2(520f, 360f),
			BbcodeEnabled = true,
			FitContent = true,
			ScrollActive = false
		};

		canvas.AddChild(_hudTimer);
		canvas.AddChild(_hudStatus);
		canvas.AddChild(_hudScoreboard);

		var restartButton = new Button
		{
			Position = new Vector2(ArenaSize.X - 180f, 40f),
			Size = new Vector2(168f, 40f),
			Text = "Restart in Builder"
		};
		restartButton.Pressed += OnRestartInBuilderPressed;
		canvas.AddChild(restartButton);

		var legend = new Label
		{
			Position = new Vector2(ArenaSize.X - 220f, 8f),
			Text = "Zones: [Viscous] [Toxic] [Turbulent] [Nutritious]"
		};
		canvas.AddChild(legend);
	}

	private void OnRestartInBuilderPressed()
	{
		_matchTimer.Stop();
		var error = GetTree().ChangeSceneToFile(ScenePaths.OrganismBuilder);
		if (error != Error.Ok)
		{
			GD.PushError($"Failed to return to builder scene: {error}");
		}
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
			.OrderByDescending(c => c.DuplicationCount)
			.ThenByDescending(c => c.Food)
			.ToList();

		var lines = new System.Collections.Generic.List<string>
		{
			"Standings (copies / biomass):"
		};

		foreach (var cell in standings)
		{
			var suffix = cell.Alive ? string.Empty : " [DEAD]";
			var color = GetRenderedCellColor(cell).ToHtml();
			lines.Add($"[color=#{color}]- {cell.Name}: {cell.DuplicationCount} / " +
					  $"{cell.Food:F1} " +
					  $"[{cell.Blueprint.ElementCount} elements]{suffix}[/color]");
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

	private Color GetRenderedCellColor(CellState cell)
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

		return color;
	}

	// ── blueprint loading ─────────────────────────────────────────────────────

	private static SimulationBalance LoadSimulationBalance()
	{
		string? Read(string path)
		{
			if (!FileAccess.FileExists(path))
			{
				return null;
			}

			using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
			return file?.GetAsText();
		}

		return SimulationBalance.Load(
			Read($"{BalanceDirectory}/environment.json"),
			type => Read($"{BalanceDirectory}/organelles/{type.SerializedName()}.json"),
			message => GD.PushWarning(message));
	}

	private static CellBlueprint? TryLoadBlueprint(string path, bool warnOnInvalid = false)
	{
		if (!FileAccess.FileExists(path))
		{
			return null;
		}

		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file is null)
		{
			if (warnOnInvalid)
			{
				GD.PushWarning($"Could not open AI configuration '{path}'. Using a random blueprint.");
			}
			return null;
		}

		try
		{
			return CellBlueprintJson.Deserialize(
				file.GetAsText(),
				message => GD.PushWarning($"Configuration '{path}': {message}"));
		}
		catch (System.Exception exception)
		{
			if (warnOnInvalid)
			{
				GD.PushWarning($"Invalid configuration '{path}': {exception.Message}. Using a random blueprint.");
			}
			return null;
		}
	}
}
