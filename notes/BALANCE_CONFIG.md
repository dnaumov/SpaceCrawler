# Balance Configuration

Runtime balance data lives under `project/balance/` and is shared by the Godot game
and console runner.

## Organelle files

Every organelle has a JSON file in `project/balance/organelles/`:

```json
{
  "upkeep": 1.0,
  "strength_coefficient": 1.0
}
```

- `upkeep` is food consumed per upkeep event. For linear and Rotation Engines, the
  event is a successful activation. For Slippery Membranes and Toxin Producers, the
  event uses the corresponding interval from `environment.json`. Other organelles
  currently have zero upkeep.
- `strength_coefficient` multiplies the organelle's effect. It scales engine impulse,
  Rotation Engine torque, Chloroplast production, Ribosome threshold reduction,
  Mitochondria survival bonus, Slippery Membrane drag reduction, gradient-sensor cone
  width, and Food Vision range/cone as applicable.

Values below zero are treated as zero. Missing or invalid files fall back to built-in
defaults and produce a warning.

## Environment file

`project/balance/environment.json` contains world-level values, including food spawn
interval and cap, drag, passive movement, metabolism, organelle activation/upkeep
intervals, base organelle power, sensor angles, zone multipliers, and passive upkeep.

The simulation rate remains fixed at 60 updates per second in code because it is a
determinism setting rather than a balance parameter. Interval values are clamped to at
least one fixed simulation update.

Godot loads these files from `res://balance`. The console runner copies the same files
beside its executable and loads them from its `balance` directory.
