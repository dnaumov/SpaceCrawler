# Architecture

## Technology

- Godot 4.5 with C# for the game client and user interface.
- .NET 8 for the shared simulation library and console runner.

## Components

### Godot client

Located in `project/`. It owns scenes, rendering, navigation, and HUD behavior. The simulation screen does not provide direct cell steering input.

- `Menu.tscn` and `Scripts/UI/MenuScene.cs`: entry point and navigation.
- `OrganismBuilder.tscn` and `Scripts/UI/`: organism builder and drag-and-drop controls.
- `Gameplay.tscn` and `Scripts/Simulation/GameplaySimulation.cs`: match presentation and Godot integration.

### Simulation library

Located in `project/SpaceCrawlerSimulation/`. This is a pure C# class library with no Godot dependency. It contains the game rules, state models, environment calculations, collisions, gradients, and organelle behavior.

`SimulationEngine` is the authoritative implementation of match rules. Both the Godot client and console runner reference this library so they execute the same simulation.

Runtime tuning is represented by `SimulationBalance`. Both front ends load the same
`project/balance/environment.json` and per-organelle JSON files. Godot reads them from
`res://balance`; the console project copies them beside its executable.

### Console runner

Located in `project/SpaceCrawlerSimulation.Runner/`. It provides a thin command-line entry point for deterministic simulations and balancing runs.

```powershell
cd project/SpaceCrawlerSimulation.Runner
dotnet run -- [durationSeconds] [aiCount] [seed]
```

Example: `dotnet run -- 120 3 42`

The runner also supports evolutionary balancing runs:

```powershell
dotnet run -- genetic [generations] [population] [durationSeconds] [seed]
```

With no additional arguments, genetic mode evaluates 20 competing genomes over
60-second matches for 100 generations. It preserves the two strongest genomes,
selects parents by tournament, performs uniform crossover, and mutates organelle
slots before the next match.

Godot AI competitors may load individual blueprints from
`res://ai_configs/ai_N.json`. Missing or invalid configurations fall back to random
blueprints generated from the complete organelle pool. See
[`notes/AI_CONFIGS.md`](notes/AI_CONFIGS.md).

## Design boundaries

- Simulation rules belong in `SpaceCrawlerSimulation`, not in Godot scene scripts.
- Godot scripts adapt simulation state for presentation; movement and rotation remain simulation-driven.
- Structural constants remain in `SimConstants.cs`; tunable gameplay values belong in `project/balance/`.
- The console runner should remain thin and must not implement separate rules.
