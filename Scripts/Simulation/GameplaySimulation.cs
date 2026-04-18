using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class GameplaySimulation : Node2D
{
	[Export] public float MatchDuration { get; set; } = 60.0f;
	[Export] public Vector2 ArenaSize { get; set; } = new(1152.0f, 648.0f);
	[Export] public int AiCompetitorCount { get; set; } = 3;
	[Export] public float FoodSpawnInterval { get; set; } = 0.6f;
	[Export] public int MaxFood { get; set; } = 80;
	[Export] public float FoodEnergyValue { get; set; } = 12.0f;
	[Export] public float CellRadius { get; set; } = 12.0f;
	[Export] public float FoodRadius { get; set; } = 5.0f;
	[Export] public float CollectionRadius { get; set; } = 18.0f;

	private sealed class Competitor
	{
		public string Name = string.Empty;
		public bool IsPlayer;
		public Vector2 Position;
		public float Speed;
		public float Energy;
		public float MaxEnergy;
		public float EnergyDrain;
		public float Health;
		public float MaxHealth;
		public int FoodCollected;
		public bool Alive;
		public Color Color;
		public Vector2 WanderDirection = Vector2.Right;
		public float WanderTimeLeft;
	}

	private readonly RandomNumberGenerator _rng = new();
	private readonly List<Competitor> _competitors = [];
	private readonly List<Vector2> _foods = [];

	private Timer _matchTimer = default!;
	private Label _hudTimer = default!;
	private Label _hudStatus = default!;
	private Label _hudScoreboard = default!;

	private float _foodSpawnCooldown;
	private bool _matchEnded;
	private Competitor? _finalWinner;

	public override void _Ready()
	{
		_matchTimer = GetNode<Timer>("MatchTimer");
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

		var dt = (float)delta;
		_foodSpawnCooldown -= dt;
		if (_foodSpawnCooldown <= 0.0f)
		{
			_foodSpawnCooldown += FoodSpawnInterval;
			SpawnFood();
		}

		UpdateCompetitors(dt);
		UpdateHud();
		QueueRedraw();

		if (AliveCompetitorsCount() == 0)
		{
			EndMatch("All organisms died.");
		}
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, ArenaSize), new Color(0.05f, 0.08f, 0.12f), true);

		foreach (var food in _foods)
		{
			DrawCircle(food, FoodRadius, new Color(0.4f, 1.0f, 0.4f));
		}

		foreach (var competitor in _competitors)
		{
			var color = competitor.Color;
			if (!competitor.Alive)
			{
				color.A = 0.35f;
			}

			DrawCircle(competitor.Position, CellRadius, color);
		}
	}

	private void StartMatch()
	{
		_matchEnded = false;
		_finalWinner = null;
		_foodSpawnCooldown = 0.0f;
		_foods.Clear();
		_competitors.Clear();
		_hudStatus.Text = "Collect more food than rivals before time runs out.";

		_competitors.Add(new Competitor
		{
			Name = "Player",
			IsPlayer = true,
			Position = ArenaSize * 0.5f,
			Speed = 220.0f,
			MaxEnergy = 100.0f,
			Energy = 100.0f,
			EnergyDrain = 4.5f,
			MaxHealth = 100.0f,
			Health = 100.0f,
			FoodCollected = 0,
			Alive = true,
			Color = new Color(0.35f, 0.75f, 1.0f),
			WanderDirection = Vector2.Right,
			WanderTimeLeft = 0.0f
		});

		for (var i = 0; i < AiCompetitorCount; i++)
		{
			_competitors.Add(new Competitor
			{
				Name = $"AI {i + 1}",
				IsPlayer = false,
				Position = new Vector2(
					_rng.RandfRange(CellRadius, ArenaSize.X - CellRadius),
					_rng.RandfRange(CellRadius, ArenaSize.Y - CellRadius)
				),
				Speed = _rng.RandfRange(160.0f, 205.0f),
				MaxEnergy = 115.0f,
				Energy = _rng.RandfRange(90.0f, 115.0f),
				EnergyDrain = _rng.RandfRange(3.2f, 5.1f),
				MaxHealth = 120.0f,
				Health = _rng.RandfRange(80.0f, 120.0f),
				FoodCollected = 0,
				Alive = true,
				Color = Color.FromHsv(_rng.Randf(), 0.65f, 0.95f),
				WanderDirection = Vector2.Right.Rotated(_rng.RandfRange(-Mathf.Pi, Mathf.Pi)),
				WanderTimeLeft = 0.0f
			});
		}

		_matchTimer.Stop();
		_matchTimer.WaitTime = MatchDuration;
		_matchTimer.OneShot = true;
		_matchTimer.Start();
		UpdateHud();
	}

	private void UpdateCompetitors(float delta)
	{
		foreach (var competitor in _competitors)
		{
			if (!competitor.Alive)
			{
				continue;
			}

			var direction = competitor.IsPlayer
				? Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down")
				: GetAiDirection(competitor, delta);

			if (direction.LengthSquared() > 1.0f)
			{
				direction = direction.Normalized();
			}

			if (direction != Vector2.Zero)
			{
				competitor.Position += direction * competitor.Speed * delta;
				competitor.Position = new Vector2(
					Mathf.Clamp(competitor.Position.X, CellRadius, ArenaSize.X - CellRadius),
					Mathf.Clamp(competitor.Position.Y, CellRadius, ArenaSize.Y - CellRadius)
				);
			}

			competitor.Energy = Mathf.Max(0.0f, competitor.Energy - competitor.EnergyDrain * delta);
			if (competitor.Energy <= 0.0f || competitor.Health <= 0.0f)
			{
				competitor.Alive = false;
			}
			else
			{
				CollectFood(competitor);
			}
		}
	}

	private Vector2 GetAiDirection(Competitor competitor, float delta)
	{
		var targetFood = FindNearestFood(competitor.Position);
		if (targetFood.HasValue)
		{
			return (targetFood.Value - competitor.Position).Normalized();
		}

		competitor.WanderTimeLeft -= delta;
		if (competitor.WanderTimeLeft <= 0.0f)
		{
			competitor.WanderDirection = Vector2.Right.Rotated(_rng.RandfRange(-Mathf.Pi, Mathf.Pi));
			competitor.WanderTimeLeft = _rng.RandfRange(0.4f, 1.2f);
		}

		return competitor.WanderDirection;
	}

	private void CollectFood(Competitor competitor)
	{
		var collectionRadiusSquared = CollectionRadius * CollectionRadius;
		for (var i = _foods.Count - 1; i >= 0; i--)
		{
			if (competitor.Position.DistanceSquaredTo(_foods[i]) <= collectionRadiusSquared)
			{
				_foods.RemoveAt(i);
				competitor.FoodCollected += 1;
				competitor.Energy = Mathf.Min(competitor.MaxEnergy, competitor.Energy + FoodEnergyValue);
			}
		}
	}

	private Vector2? FindNearestFood(Vector2 origin)
	{
		if (_foods.Count == 0)
		{
			return null;
		}

		var nearest = _foods[0];
		var nearestDistance = origin.DistanceSquaredTo(nearest);
		for (var i = 1; i < _foods.Count; i++)
		{
			var distance = origin.DistanceSquaredTo(_foods[i]);
			if (distance < nearestDistance)
			{
				nearestDistance = distance;
				nearest = _foods[i];
			}
		}

		return nearest;
	}

	private void SpawnFood()
	{
		if (_foods.Count >= MaxFood)
		{
			return;
		}

		_foods.Add(new Vector2(
			_rng.RandfRange(FoodRadius, ArenaSize.X - FoodRadius),
			_rng.RandfRange(FoodRadius, ArenaSize.Y - FoodRadius)
		));
	}

	private int AliveCompetitorsCount() => _competitors.Count(c => c.Alive);

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

		if (_finalWinner == null)
		{
			_hudStatus.Text = "Match ended. No winner.";
		}
		else
		{
			var message = $"Winner: {_finalWinner.Name} (food={_finalWinner.FoodCollected}, energy={_finalWinner.Energy:F1}, health={_finalWinner.Health:F1})";
			if (!string.IsNullOrEmpty(reason))
			{
				message += $" — {reason}";
			}

			_hudStatus.Text = message;
		}

		UpdateHud();
	}

	private Competitor? CalculateWinner()
	{
		if (_competitors.Count == 0)
		{
			return null;
		}

		var maxFood = _competitors.Max(c => c.FoodCollected);
		var foodTied = _competitors
			.Where(c => c.FoodCollected == maxFood)
			.ToList();
		if (foodTied.Count == 1)
		{
			return foodTied[0];
		}

		var maxEnergy = foodTied.Max(c => c.Energy);
		var energyTied = foodTied
			.Where(c => Mathf.IsEqualApprox(c.Energy, maxEnergy))
			.ToList();
		if (energyTied.Count == 1)
		{
			return energyTied[0];
		}

		var maxHealth = energyTied.Max(c => c.Health);
		var healthTied = energyTied
			.Where(c => Mathf.IsEqualApprox(c.Health, maxHealth))
			.ToList();
		if (healthTied.Count == 1)
		{
			return healthTied[0];
		}

		return healthTied[_rng.RandiRange(0, healthTied.Count - 1)];
	}

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
	}

	private void UpdateHud()
	{
		_hudTimer.Text = _matchEnded ? "Time left: 0.0s" : $"Time left: {_matchTimer.TimeLeft:F1}s";

		var standings = _competitors
			.OrderByDescending(c => c.FoodCollected)
			.ThenByDescending(c => c.Energy)
			.ThenByDescending(c => c.Health)
			.ToList();

		var lines = new List<string> { "Standings (food / energy / health):" };
		foreach (var competitor in standings)
		{
			var deadSuffix = competitor.Alive ? string.Empty : " [DEAD]";
			lines.Add($"- {competitor.Name}: {competitor.FoodCollected} / {competitor.Energy:F1} / {competitor.Health:F1}{deadSuffix}");
		}

		_hudScoreboard.Text = string.Join('\n', lines);
	}
}
