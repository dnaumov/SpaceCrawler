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
