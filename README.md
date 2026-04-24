# SpaceCrawler

## 1. General description

SpaceCrawler is a 2D Godot game prototype where the player designs a microorganism (cell/bacteria) from modular parts and runs automated matches against other species.

Core loop:
- Build a creature from available components
- Run a simulation match
- Compete for resources
- Earn progression points to unlock stronger parts
- Move to more challenging levels

Current repository status:
- Godot project configuration is present (`project.godot`)
- Main gameplay-related scenes currently include:
  - `Gameplay.tscn`
  - `OrganismBuilder.tscn`

## 2. Gameplay rules

Prototype match rules:
- Match duration: 60 seconds (initial target for prototype balancing; configurable later)
- Competitors: 1 player build + AI-generated rivals
- Main objective: collect the most food by the end of the timer
- Tie-breakers (in order):
  1. Higher remaining energy
  2. Higher remaining health
  3. Random winner

Core design principles:
- Every component must have a trade-off (power vs. cost)
- Builds should be diverse (no single dominant setup)
- Current prototype scope is non-combat until combat systems are explicitly approved

Initial non-combat component set (Stage 1 baseline):
- **Core membrane**: required base; gives base HP and energy capacity
- **Flagella engine**: increases movement speed, increases energy drain
- **Sensor cilia**: improves food targeting/collection range, lowers max health
- **Mitochondria cluster**: increases max energy and food-to-energy efficiency, lowers speed
- **Armor plate**: increases health, decreases speed

Detailed categorized element list:
- See [`CELL_ELEMENTS.md`](CELL_ELEMENTS.md) (Movement, Sensors, Energy, Auxiliary)

Initial stat list (Stage 1 baseline):
- **Speed**
- **Max Energy**
- **Energy Drain**
- **Health**
- **Food Collected** (match score)
- **Collection Radius**

## 3. Recommended scenes and code structure

Recommended scene layout:
- `Main.tscn` (entry point and flow)
- `Simulation.tscn` (match arena)
- `OrganismBuilder.tscn` (build/edit creature)
- `Cell.tscn` (single organism)
- `Food.tscn` (resource pickup)
- `Results.tscn` (post-match summary)

Recommended C# structure:
- `Scripts/Core/`
  - `GameManager.cs`
  - `SimulationManager.cs`
- `Scripts/Cells/`
  - `Cell.cs`
  - `CellStats.cs`
  - `SpeciesBlueprint.cs`
- `Scripts/Parts/`
  - `CellPart.cs`
  - `SensorPart.cs`
  - `EnginePart.cs`
  - `MitochondriaPart.cs`
  - `WeaponPart.cs`
  - `ArmorPart.cs`
- `Scripts/UI/`
  - `BuildScreen.cs`
  - `ResultsScreen.cs`

## 4. Project stages and TODOs

### Stage 1 — Rules and scope
- [x] Finalize first playable ruleset documented above
- [x] Finalize initial components and stat list
- [x] Finalize match objective and tie-breakers

### Stage 2 — Playable simulation core
- [x] Implement cell movement
- [x] Implement food spawn/collection
- [x] Implement energy drain and death conditions
- [x] Implement match timer and winner calculation

### Stage 3 — Modular component system
- [ ] Implement `CellPart` base system
- [ ] Add stat and behavior modifiers per part
- [ ] Build and instantiate cells from blueprints

### Stage 4 — Builder and opponents
- [ ] Complete organism builder flow
- [ ] Add AI species archetypes
- [ ] Generate rivals from same component rules

### Stage 5 — Progression and polish
- [ ] Add points and unlock system
- [ ] Add level/biome progression
- [ ] Add results screen and balancing tools
- [ ] Improve visuals, effects, and sound after mechanics stabilize
