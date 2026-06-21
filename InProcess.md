# Current Work

## Milestone

Builder usability, simulation feedback, and evolutionary balancing.

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
- Limited forward engines to one activation attempt per 10-second interval, with food
  charged only when an activation succeeds.
- Replaced the arena-wide scalar gradient grid with on-demand direction calculations.
- Made each gradient sensor compare its outward grid orientation with the gradient
  direction, excluded self-detection from cell sensors, and separated Food Vision into
  an 8 S, 30-degree forward-cone check.
- Increased all linear engine impulses by four times while leaving Rotation Engine torque unchanged.
- Made multiple Slippery Membranes halve drag multiplicatively instead of applying only one reduction.
- Added shared runtime balance loading with one upkeep/strength JSON file per organelle
  and an environment JSON for spawning, drag, timing, movement, sensors, and zone values.
- Wired both Godot and the console runner to the same balance files with safe defaults
  and warnings for missing or invalid configuration.

### Console runner

- Added a `genetic` mode that starts with random organisms and evolves them through selection, elitism, uniform crossover, and mutation.
- The default evolutionary run uses 100 generations, 20 genomes, and 60-second matches.
- Added progress reports, final-generation rankings, and output of the best observed organelle grid.
- Preserved compatibility with the original single-match command.

## Verification

- The complete Godot and .NET solution builds successfully with no errors.
- The original console simulation mode runs successfully.
- A full default 100-generation genetic run completes successfully.
- Two existing nullable-annotation compiler warnings remain in `GameplaySimulation.cs`.

## Session checkpoint

This checkpoint includes the genetic runner, builder restart flow, saved-grid restoration, documentation updates, and pass-through cell behavior.

## Next tasks

- [ ] Test the restart-to-builder flow interactively in Godot.
- [ ] Review and tune genetic fitness weights, mutation rate, population size, and match duration.
- [ ] Add a visual interface for selecting sensor-to-engine connections.
- [ ] Add inverse sensor links.
- [ ] Define distinct AI species archetypes and representative blueprints.
- [ ] Resolve the remaining nullable-annotation warnings.

## Next decision

Decide whether genetic-mode winners should remain a balancing tool only or become a source of AI opponent blueprints used by the game.
