# Architecture

## Technology

- Godot 4.5 with C# for the game client and user interface.
- .NET 8 for the shared simulation library and console runner.

## Components

### Godot client

Located in `project/`. It owns scenes, input, rendering, navigation, and HUD behavior.

- `Menu.tscn` and `Scripts/UI/MenuScene.cs`: entry point and navigation.
- `OrganismBuilder.tscn` and `Scripts/UI/`: organism builder and drag-and-drop controls.
- `Gameplay.tscn` and `Scripts/Simulation/GameplaySimulation.cs`: match presentation and Godot integration.

### Simulation library

Located in `project/SpaceCrawlerSimulation/`. This is a pure C# class library with no Godot dependency. It contains the game rules, state models, environment calculations, collisions, gradients, and organelle behavior.

`SimulationEngine` is the authoritative implementation of match rules. Both the Godot client and console runner reference this library so they execute the same simulation.

### Console runner

Located in `project/SpaceCrawlerSimulation.Runner/`. It provides a thin command-line entry point for deterministic simulations and balancing runs.

```powershell
cd project/SpaceCrawlerSimulation.Runner
dotnet run -- [durationSeconds] [aiCount] [seed]
```

Example: `dotnet run -- 120 3 42`

## Design boundaries

- Simulation rules belong in `SpaceCrawlerSimulation`, not in Godot scene scripts.
- Godot scripts adapt simulation state for presentation and player input.
- Numeric gameplay constants should remain centralized in `SimConstants.cs`.
- The console runner should remain thin and must not implement separate rules.

