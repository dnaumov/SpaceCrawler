extends Node2D

@export var match_duration: float = 60.0
@export var arena_size: Vector2 = Vector2(1152.0, 648.0)
@export var ai_competitor_count: int = 3
@export var food_spawn_interval: float = 0.6
@export var max_food: int = 80
@export var food_energy_value: float = 12.0
@export var cell_radius: float = 12.0
@export var food_radius: float = 5.0
@export var collection_radius: float = 18.0

@onready var _match_timer: Timer = $MatchTimer

var _rng := RandomNumberGenerator.new()
var _food_spawn_cooldown: float = 0.0
var _match_ended: bool = false
var _competitors: Array[Dictionary] = []
var _foods: Array[Vector2] = []

var _hud_timer: Label
var _hud_status: Label
var _hud_scoreboard: Label


func _ready() -> void:
	_rng.randomize()
	_setup_hud()
	_start_match()


func _process(delta: float) -> void:
	if _match_ended:
		return

	_food_spawn_cooldown -= delta
	if _food_spawn_cooldown <= 0.0:
		_food_spawn_cooldown += food_spawn_interval
		_spawn_food()

	_update_competitors(delta)
	_update_hud()
	queue_redraw()

	if _alive_competitors_count() == 0:
		_end_match("All organisms died.")


func _draw() -> void:
	draw_rect(Rect2(Vector2.ZERO, arena_size), Color(0.05, 0.08, 0.12), true)

	for food_position in _foods:
		draw_circle(food_position, food_radius, Color(0.4, 1.0, 0.4))

	for competitor in _competitors:
		var color: Color = competitor["color"]
		if not competitor["alive"]:
			color.a = 0.35
		draw_circle(competitor["position"], cell_radius, color)


func _start_match() -> void:
	_match_ended = false
	_foods.clear()
	_competitors.clear()
	_food_spawn_cooldown = 0.0
	_hud_status.text = "Collect more food than rivals before time runs out."

	_competitors.append({
		"name": "Player",
		"is_player": true,
		"position": arena_size * 0.5,
		"speed": 220.0,
		"energy": 100.0,
		"max_energy": 100.0,
		"energy_drain": 4.5,
		"health": 100.0,
		"max_health": 100.0,
		"food_collected": 0,
		"alive": true,
		"color": Color(0.35, 0.75, 1.0),
		"wander_direction": Vector2.RIGHT,
		"wander_time_left": 0.0
	})

	for i in ai_competitor_count:
		_competitors.append({
			"name": "AI %d" % (i + 1),
			"is_player": false,
			"position": Vector2(
				_rng.randf_range(cell_radius, arena_size.x - cell_radius),
				_rng.randf_range(cell_radius, arena_size.y - cell_radius)
			),
			"speed": _rng.randf_range(160.0, 205.0),
			"energy": _rng.randf_range(90.0, 115.0),
			"max_energy": 115.0,
			"energy_drain": _rng.randf_range(3.2, 5.1),
			"health": _rng.randf_range(80.0, 120.0),
			"max_health": 120.0,
			"food_collected": 0,
			"alive": true,
			"color": Color.from_hsv(_rng.randf(), 0.65, 0.95),
			"wander_direction": Vector2.RIGHT.rotated(_rng.randf_range(-PI, PI)),
			"wander_time_left": 0.0
		})

	if not _match_timer.timeout.is_connected(_on_match_timeout):
		_match_timer.timeout.connect(_on_match_timeout)
	_match_timer.stop()
	_match_timer.wait_time = match_duration
	_match_timer.one_shot = true
	_match_timer.start()
	_update_hud()


func _update_competitors(delta: float) -> void:
	for i in _competitors.size():
		var competitor := _competitors[i]
		if not competitor["alive"]:
			continue

		var direction := Vector2.ZERO
		if competitor["is_player"]:
			direction = Input.get_vector("ui_left", "ui_right", "ui_up", "ui_down")
		else:
			direction = _get_ai_direction(competitor, delta)

		if direction.length_squared() > 1.0:
			direction = direction.normalized()

		if direction != Vector2.ZERO:
			competitor["position"] += direction * competitor["speed"] * delta
			competitor["position"] = Vector2(
				clampf(competitor["position"].x, cell_radius, arena_size.x - cell_radius),
				clampf(competitor["position"].y, cell_radius, arena_size.y - cell_radius)
			)

		competitor["energy"] = max(0.0, competitor["energy"] - competitor["energy_drain"] * delta)
		if competitor["energy"] <= 0.0 or competitor["health"] <= 0.0:
			competitor["alive"] = false
		else:
			_collect_food(competitor)

		_competitors[i] = competitor


func _get_ai_direction(competitor: Dictionary, delta: float) -> Vector2:
	var target_food := _find_nearest_food(competitor["position"])
	if target_food != Vector2.INF:
		return (target_food - competitor["position"]).normalized()

	competitor["wander_time_left"] -= delta
	if competitor["wander_time_left"] <= 0.0:
		var random_angle := _rng.randf_range(-PI, PI)
		competitor["wander_direction"] = Vector2.RIGHT.rotated(random_angle)
		competitor["wander_time_left"] = _rng.randf_range(0.4, 1.2)

	return competitor["wander_direction"]


func _collect_food(competitor: Dictionary) -> void:
	for i in range(_foods.size() - 1, -1, -1):
		if competitor["position"].distance_to(_foods[i]) <= collection_radius:
			_foods.remove_at(i)
			competitor["food_collected"] += 1
			competitor["energy"] = min(competitor["max_energy"], competitor["energy"] + food_energy_value)


func _find_nearest_food(origin: Vector2) -> Vector2:
	var nearest := Vector2.INF
	var nearest_distance := INF
	for food_position in _foods:
		var distance := origin.distance_squared_to(food_position)
		if distance < nearest_distance:
			nearest_distance = distance
			nearest = food_position
	return nearest


func _spawn_food() -> void:
	if _foods.size() >= max_food:
		return

	_foods.append(Vector2(
		_rng.randf_range(food_radius, arena_size.x - food_radius),
		_rng.randf_range(food_radius, arena_size.y - food_radius)
	))


func _alive_competitors_count() -> int:
	var count := 0
	for competitor in _competitors:
		if competitor["alive"]:
			count += 1
	return count


func _on_match_timeout() -> void:
	_end_match()


func _end_match(reason: String = "") -> void:
	if _match_ended:
		return
	_match_ended = true
	_match_timer.stop()

	var winner := _calculate_winner()
	if winner.is_empty():
		_hud_status.text = "Match ended. No winner."
	else:
		var message := "Winner: %s (food=%d, energy=%.1f, health=%.1f)" % [
			winner["name"],
			winner["food_collected"],
			winner["energy"],
			winner["health"]
		]
		if reason != "":
			message += " — %s" % reason
		_hud_status.text = message
	_update_hud()


func _calculate_winner() -> Dictionary:
	if _competitors.is_empty():
		return {}

	var best_food := -1
	for competitor in _competitors:
		best_food = max(best_food, competitor["food_collected"])

	var food_tied: Array[Dictionary] = []
	for competitor in _competitors:
		if competitor["food_collected"] == best_food:
			food_tied.append(competitor)
	if food_tied.size() == 1:
		return food_tied[0]

	var best_energy := -INF
	for competitor in food_tied:
		best_energy = max(best_energy, competitor["energy"])

	var energy_tied: Array[Dictionary] = []
	for competitor in food_tied:
		if is_equal_approx(competitor["energy"], best_energy):
			energy_tied.append(competitor)
	if energy_tied.size() == 1:
		return energy_tied[0]

	var best_health := -INF
	for competitor in energy_tied:
		best_health = max(best_health, competitor["health"])

	var health_tied: Array[Dictionary] = []
	for competitor in energy_tied:
		if is_equal_approx(competitor["health"], best_health):
			health_tied.append(competitor)
	if health_tied.size() == 1:
		return health_tied[0]

	return health_tied[_rng.randi_range(0, health_tied.size() - 1)]


func _setup_hud() -> void:
	var canvas := CanvasLayer.new()
	add_child(canvas)

	_hud_timer = Label.new()
	_hud_timer.position = Vector2(12.0, 8.0)
	canvas.add_child(_hud_timer)

	_hud_status = Label.new()
	_hud_status.position = Vector2(12.0, 32.0)
	canvas.add_child(_hud_status)

	_hud_scoreboard = Label.new()
	_hud_scoreboard.position = Vector2(12.0, 56.0)
	canvas.add_child(_hud_scoreboard)


func _update_hud() -> void:
	_hud_timer.text = "Time left: %.1fs" % _match_timer.time_left

	var standings := _competitors.duplicate()
	standings.sort_custom(func(a: Dictionary, b: Dictionary) -> bool:
		if a["food_collected"] != b["food_collected"]:
			return a["food_collected"] > b["food_collected"]
		if not is_equal_approx(a["energy"], b["energy"]):
			return a["energy"] > b["energy"]
		return a["health"] > b["health"]
	)

	var lines: Array[String] = []
	lines.append("Standings (food / energy / health):")
	for competitor in standings:
		var dead_suffix := ""
		if not competitor["alive"]:
			dead_suffix = " [DEAD]"
		lines.append("- %s: %d / %.1f / %.1f%s" % [
			competitor["name"],
			competitor["food_collected"],
			competitor["energy"],
			competitor["health"],
			dead_suffix
		])
	_hud_scoreboard.text = "\n".join(lines)
