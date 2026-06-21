# AI Configuration Files

The Godot simulation looks for one optional JSON file per AI competitor:

```text
res://ai_configs/ai_1.json
res://ai_configs/ai_2.json
res://ai_configs/ai_3.json
...
```

In the repository, these paths correspond to `project/ai_configs/`. AI numbering is
one-based and matches the names shown in the simulation standings. Missing files use
a randomly generated blueprint. Invalid files produce a warning and also fall back to
random generation.

## Format

Each file uses the same 16-slot, row-major format as the player configuration:

```json
{
  "grid_size": 4,
  "components": [
    "FoodGradientDetector", "EffectiveEngine", "", "",
    "", "Nucleus", "Nucleus", "RotationEngine",
    "", "Nucleus", "Nucleus", "",
    "Mitochondria", "", "EffectiveEngine", ""
  ]
}
```

Slots 5, 6, 9, and 10 are always forced to `Nucleus` when loaded. Empty strings and
unknown names become empty slots. Canonical organelle names are listed in
[`CELL_ELEMENTS.md`](CELL_ELEMENTS.md).

## Random fallback

Random AI blueprints use 2-8 organelles selected from every available non-nucleus
organelle type. Placement uses randomly selected non-nucleus grid slots.

## Included configurations and benchmark

`project/ai_configs/ai_1.json` contains a tuned survival-and-collection blueprint:
one Slippery Membrane, five Chloroplasts, three Mitochondria, and three Ribosomes.
Before the fixed-step, biomass, and interval-based engine changes, a 30-seed benchmark
of 120-second matches against three full-pool random opponents gave this build 22 wins
and a surviving lineage in 28. These historical results require a new benchmark under
the current economy.

`project/ai_configs/ai_2.json` is an experimental pure-Chloroplast build. All 12
non-nucleus slots contain Chloroplasts, maximizing passive food production while
providing no specialized movement, rotation, sensing, or death-threshold bonuses.
In the historical benchmark, AI 2 won 15 matches, survived all 30, averaged 0.97
collected food, and retained an average reserve of 31.83. In 30 direct matches with
both configurations and two random opponents, AI 1 won 21, AI 2 won 6, and a random
opponent won 3.

Neither saved configuration produced a copy under the former pickup-only reproduction rules. Consequently,
the copy-first criterion did not distinguish them; AI 1's higher food collection won
more of the remaining comparisons, while AI 2 favored survival and accumulated reserve.
Chloroplast production now contributes biomass, so these results should not be treated
as representative of the current rules.
