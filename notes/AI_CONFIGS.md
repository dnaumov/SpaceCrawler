# AI Configuration Files

The Godot simulation looks for one optional JSON file per AI competitor:

```text
res://ai_configs/ai_1.json
res://ai_configs/ai_2.json
res://ai_configs/ai_3.json
...
```

In the repository, these paths correspond to `project/ai_configs/`. AI numbering is
one-based and matches the names shown in the simulation standings. Missing or invalid
files fall back to a randomly generated blueprint.

## Format

Each file uses the same 16-slot, row-major format as the player configuration. Slots
5, 6, 9, and 10 are always forced to `Nucleus` when loaded. Empty strings and unknown
names become empty slots. Canonical names are listed in
[`CELL_ELEMENTS.md`](CELL_ELEMENTS.md).

Optional wiring is stored as an edge list:

```json
"connections": [
  { "sensor_slot": 1, "engine_slot": 2, "inverted": false },
  { "sensor_slot": 1, "engine_slot": 14, "inverted": true }
]
```

Sensor slots may appear many times. Engine slots may appear only once. Invalid edges
are ignored with warnings, and older files without `connections` remain valid.

## Random fallback

Random AI blueprints use 2-8 organelles selected from every available non-nucleus
organelle type. Placement uses randomly selected non-nucleus grid slots.

## Included configurations

`project/ai_configs/ai_1.json` is an active, low-drag build with three Chloroplasts,
four Ribosomes, three Slippery Membranes, one forward Food Gradient Sensor, and one
Effective Engine. Its sensor and engine occupy the two upper inner slots so sensed food
gradients are close to the engine's forward thrust direction. With zero membrane upkeep,
the build survived all 12 candidate-selection matches. In a subsequent 20-match direct
comparison with AI 2 and two random opponents, it won once, averaged 1.2 births, and
AI 2 won the other 19 matches.

`project/ai_configs/ai_2.json` is a pure-Chloroplast build. All 12 non-nucleus slots
contain Chloroplasts, maximizing passive biomass production while providing no
specialized movement, rotation, sensing, or survival bonuses. Under the current balance
it remains the stronger reproduction-focused baseline.
