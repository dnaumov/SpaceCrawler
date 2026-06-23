# Current Work

## Milestone

UI polish, player meta progression, and multi-input organelle logic.

## Session results

### Project maintenance

- Reorganized the repository documentation to match the standard project structure.
- Fixed the Godot project configuration so nested projects and generated files are not compiled twice.
- Added repository-wide UTF-8 and line-ending configuration and ignored Visual Studio's local cache.

### Organism builder

- Fixed drag-and-drop input for available organelles, grid slots, and the removal zone.
- Allowed multiple instances of the same organelle type in one cell.
- Added restoration of the saved 4 x 4 organism configuration when returning to the builder.
- Kept the four nucleus positions locked when loading saved configurations.

### Simulation screen

- Matched each standings row to its cell's display color, including starving and dead states.
- Increased passive random linear and rotational impulses so movement is visibly noticeable.
- Added a **Restart in Builder** button that returns to the design screen without losing the current organism.
- Removed cell-to-cell collision response; cells now overlap and pass through each other freely.
- Removed keyboard steering and automatic nearest-food steering.
- Doubled passive random rotation force while applying it on half of simulation updates.
- Added the Rotation Engine with placement-based clockwise/counterclockwise torque.
- Expanded random AI generation to every available organelle type.
- Added separate `ai_N.json` blueprint loading for each AI with random fallback.
- Added a benchmarked AI 1 baseline (22/30 wins and 28/30 survival across full-length seeded matches).
- Added a pure-Chloroplast AI 2 configuration for comparison testing.
- Updated winner selection and standings to prioritize copies, then collected food, then reserve.
- Re-benchmarked both saved configurations under the copy-first ranking: AI 1 won 22/30
  seeded matches versus random opponents and 21/30 direct comparison matches; AI 2 won
  15/30 versus random opponents and 6/30 direct comparison matches.
- Unified biomass and fuel into one resource. Food pickups and Chloroplasts increase it,
  metabolism and active organelles consume it, and division splits it between cells.
- Changed Ribosomes to reduce the duplication threshold by two, giving a net benefit
  even when a Ribosome is added to an empty slot.
- Made the simulation advance at a deterministic 60 fixed updates per second.
- Changed sensor-controlled and ordinary engines to activate once per second at one tenth
  force and fuel cost, while Random Engines retain ten-second full-strength activations.
- Replaced the arena-wide scalar gradient grid with on-demand direction calculations.
- Made each gradient sensor compare its outward grid orientation with the gradient
  direction, excluded self-detection from cell sensors, and separated Food Vision into
  an 8 S, 30-degree forward-cone check.
- Increased all linear engine impulses by four times while leaving Rotation Engine torque unchanged.
- Made multiple Slippery Membranes halve drag multiplicatively instead of applying only one reduction.
- Removed Slippery Membrane upkeep and benchmarked active builds with up to three stacked membranes.
- Added shared runtime balance loading with one upkeep/strength JSON file per organelle
  and an environment JSON for spawning, drag, timing, movement, sensors, and zone values.
- Wired both Godot and the console runner to the same balance files with safe defaults
  and warnings for missing or invalid configuration.
- Added explicit sensor-to-engine edge wiring with sensor fan-out, one input per engine,
  independent inversion, backward-compatible JSON, and builder persistence controls.
- Added an xUnit simulation test project covering validation, serialization, inversion,
  fan-out, and runtime activation edge cases.
- Enabled nullable analysis for the Godot simulation screen and corrected optional
  balance-file loading so the complete solution builds without warnings.

### Console runner

- Added a `genetic` mode that starts with random organisms and evolves them through selection, elitism, uniform crossover, and mutation.
- The default evolutionary run uses 100 generations, 20 genomes, and 60-second matches.
- Added progress reports, final-generation rankings, and output of the best observed organelle grid.
- Preserved compatibility with the original single-match command.

## Verification

- The complete Godot and .NET solution builds successfully with no errors.
- The original console simulation mode runs successfully.
- A full default 100-generation genetic run completes successfully.
- All 12 sensor-wiring unit tests pass.
- The complete solution builds with zero warnings and zero errors.

## Session checkpoint

This checkpoint includes explicit sensor-to-engine wiring, builder connection editing,
backward-compatible blueprint persistence, edge-case tests, and warning-free builds.

## Next tasks

- [ ] Test the restart-to-builder flow interactively in Godot.
- [ ] Improve the Godot UI across the menu, builder, simulation HUD, and results flow.
- [ ] Make organelle placement and connection wiring easier to understand through
  clearer visual feedback, layout, and interaction states.
- [ ] Design player meta progression: rewards, persistent currency/progress, organelle
  unlocks, and how progression feeds back into organism building.
- [ ] Add a Neuron organelle that accepts multiple signal inputs, combines them using
  a clearly defined rule, and exposes an output that can drive organelles downstream.
- [ ] Define Neuron graph validation, inversion behavior, serialization, builder UX,
  runtime evaluation order, and tests before implementing chained signal networks.
- [ ] Review and tune genetic fitness weights, mutation rate, population size, and match duration.
- [ ] Define distinct AI species archetypes and representative blueprints.

## Next decision

Choose the first meta-progression loop and define the Neuron's initial signal-combination
rule so UI flows and data structures can be designed around concrete behavior.
