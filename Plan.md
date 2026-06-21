# Plan

## Completed

- [x] **P0** Define the core rules, constants, organelles, match objective, and win conditions.
- [x] **P0** Rank competitors by copies produced, food collected, then food reserve.
- [x] **P0** Implement movement, drag, food collection, passive drain, death, and duplication.
- [x] **P0** Implement pass-through cell interactions and passive random movement and rotation.
- [x] **P0** Remove direct player/AI steering and add organelle-driven rotational control.
- [x] **P0** Implement viscous, toxic, turbulent, and nutritious environments.
- [x] **P0** Implement gradient fields and organelle activation.
- [x] **P1** Extract the rules into a shared pure C# simulation library.
- [x] **P1** Add a configurable console simulation runner.
- [x] **P1** Add genetic-algorithm balancing runs with selection, crossover, and mutation.
- [x] **P1** Support per-AI JSON blueprints with complete-pool random fallback.
- [x] **P0** Add sensor fan-out, single engine inputs, and independently inverted outputs.
- [x] **P0** Add builder controls for sensor-to-engine connections.
- [x] **P1** Add unit tests for simulation wiring and edge cases.

## Current milestone: UI and signal logic

- [ ] **P0** Improve the Godot menu, builder, simulation HUD, and results UI.
- [ ] **P0** Improve organelle placement and connection-wiring feedback in the builder.
- [ ] **P0** Define and implement a Neuron organelle that combines multiple signal inputs.
- [ ] **P0** Add Neuron wiring validation, persistence, runtime evaluation, builder controls,
  and unit tests for inversion, invalid graphs, and chained signals.
- [ ] **P1** Add validation and feedback for organism builds.

## Next milestone: Player meta progression

- [ ] **P0** Define the reward loop and persistent player-progression data.
- [ ] **P0** Add points or currency and organelle unlocks.
- [ ] **P1** Surface rewards, unlocks, and progression choices in the Godot UI.
- [ ] **P1** Add save migration and safe fallback for progression data.

## Future milestone: Content and polish

- [ ] **P1** Create distinct AI species archetypes and blueprints.
- [ ] **P1** Add level and biome progression with different environments.
- [ ] **P1** Add a results screen and balancing tools.
- [ ] **P2** Improve organelle and environment visuals and effects.
- [ ] **P2** Add sound after the mechanics stabilize.
