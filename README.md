# SpaceCrawler

## 1. General description

SpaceCrawler is a 2D Godot game prototype where the player designs a microorganism (cell/bacteria) from modular organelle parts and runs automated matches against other species.

Core loop:
- Build a cell from available organelles in the 4×4 grid builder
- Run a simulation match
- Survive by collecting food and duplicating
- Compete against AI-designed rivals
- Unlock stronger organelle parts for progression

## 2. Project structure

```
SpaceCrawler/                          ← Godot game project
  Scripts/
    Core/
      ScenePaths.cs                    ← Shared scene path constants
    Simulation/
      GameplaySimulation.cs            ← Thin Godot wrapper: input, rendering, HUD
                                          (all rules live in SimulationEngine)
    UI/
      OrganismBuilderScene.cs          ← Builder scene controller
      GridNodeSlot.cs                  ← Drag-and-drop grid slot widget
      ComponentItemLabel.cs            ← Draggable organelle label
      DragPayload.cs                   ← Drag-and-drop data model
      RemoveDropZone.cs                ← Drop zone that removes organelles
      MenuScene.cs                     ← Main menu controller
  Gameplay.tscn                        ← Gameplay scene
  OrganismBuilder.tscn                 ← Builder scene
  Menu.tscn                            ← Main menu scene

SpaceCrawlerSimulation/                ← Shared pure-C# simulation library (no Godot)
  SimulationEngine.cs                  ← All game rules (food, drag, collisions, …)
  SimConstants.cs                      ← All numeric constants
  OrganelleType.cs                     ← Organelle enum + serialization helpers
  CellBlueprint.cs                     ← Cell blueprint data model
  CellState.cs                         ← Mutable runtime cell state
  Vec2.cs                              ← 2D vector math
  EnvironmentZone.cs                   ← Environment zone types
  GradientField.cs                     ← Grid-based gradient calculations

SpaceCrawlerSimulation.Runner/         ← Thin console entry point
  Program.cs                           ← CLI args (duration aiCount seed), prints results

SpaceCrawler.sln                       ← Solution file (all three projects)
```

> **Design principle**: `SpaceCrawlerSimulation` is a class library referenced by both the
> Godot game (`SpaceCrawler.csproj`) and the console runner
> (`SpaceCrawlerSimulation.Runner.csproj`).  Game rules live in exactly one place —
> `SimulationEngine` — and are exercised identically in both contexts.

## 3. Gameplay rules

### 3.1 General constants

| Symbol | Value   | Meaning                                     |
|--------|---------|---------------------------------------------|
| [T]    | 10 s    | Time interval (game tick)                   |
| [C]    | 1 food  | Passive food consumption per [T]            |
| [S]    | 16 px   | Base size unit                              |

### 3.2 Cell rules

- A cell consists of **4–16 elements** placed on a **4×4 grid**.
- The **center 4 slots** (grid indices 5, 6, 9, 10) are always **Nucleus** and cannot be removed or modified.
- A cell **collects food** by touching it (cell = 2[S]×2[S], food = 1[S]×1[S]).
- Upon collecting **[number of elements]** food items, the cell **duplicates** (Ribosome reduces this threshold by 1 each).
- A cell **consumes [C] = 1 food per [T]** passively.
- A cell can survive down to **−4 food** (each Mitochondria extends this by 1).  At or below the threshold the cell **dies**.
- Cell movement has **drag** — speed decreases over time.
- **Collision** between cells causes elastic bounce.
- Cells have passive **random small movements and rotation** every frame.
- Cells have a **rotation** property; movement organelles fire in the direction away from the nucleus.

### 3.3 Gradient calculations (per tick)

Gradients are recalculated for each grid position every game tick:

| Gradient type       | Formula                                |
|---------------------|----------------------------------------|
| Food gradient       | `SUM[ Food_x / (dist_x² + ε) ]`        |
| Cell concentration  | `SUM[ Cell_x / (dist_x² + ε) ]`        |
| Toxic environment   | `SUM[ ToxicZone_x / (dist_x² + ε) ]`  |

Sensory organelles check these gradient values to decide whether to activate the connected movement organelles.

### 3.4 Environments

| Environment   | Effect (active when > half the cell is inside)    |
|---------------|---------------------------------------------------|
| Normal        | No modifier                                       |
| Viscous       | 2× increased drag                                 |
| Toxic         | 2× increased passive food drain                   |
| Turbulent     | 2× random cell movement/rotation                  |
| Nutritious    | 2× food generation from collection                |

### 3.5 Organelles — see [`CELL_ELEMENTS.md`](CELL_ELEMENTS.md)

## 4. Running the console simulation

```
cd SpaceCrawlerSimulation.Runner
dotnet run -- [durationSeconds] [aiCount] [seed]

# Example: 120-second match, 3 AI cells, seed 42
dotnet run -- 120 3 42
```

The runner delegates entirely to `SimulationEngine` in `SpaceCrawlerSimulation` — the same
engine the Godot game uses — and prints per-tick standings plus a final result.

## 5. Recommended scene layout

| Scene                   | Purpose                       |
|-------------------------|-------------------------------|
| `Menu.tscn`             | Entry point and navigation    |
| `OrganismBuilder.tscn`  | Build/edit cell organelle grid |
| `Gameplay.tscn`         | Simulation match arena        |

## 6. Project stages and TODOs

### Stage 1 — Rules and scope ✅
- [x] Finalize game rules and constants
- [x] Define all organelle types and their effects
- [x] Define match objective and win conditions

### Stage 2 — Gameplay simulation ✅
- [x] Implement cell movement with drag
- [x] Implement food spawn/collection
- [x] Implement passive food drain and death
- [x] Implement cell duplication
- [x] Implement cell-cell collision
- [x] Implement passive random movement/rotation
- [x] Implement environment zones (Viscous, Toxic, Turbulent, Nutritious)
- [x] Implement gradient field (food, cell concentration, toxic)
- [x] Implement organelle activation (engines, sensors, chloroplast, etc.)

### Stage 3 — Console simulation ✅
- [x] Standalone C# console app with full rule-set
- [x] Configurable match parameters (duration, AI count, seed)

### Stage 4 — Builder and opponents
- [ ] Full sensor-to-engine visual connection UI in builder
- [ ] "Inverse" sensor link type
- [ ] Complete AI species archetypes with varied blueprints

### Stage 5 — Progression and polish
- [ ] Points and unlock system for new organelles
- [ ] Level/biome progression with different default environments
- [ ] Results screen and balancing tools
- [ ] Visual improvements (organelle art, environment art, effects)
- [ ] Sound after mechanics stabilize
