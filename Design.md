# Product Design

## Overview

The player constructs a microorganism from modular organelles on a 4 x 4 grid and tests it in automated ecosystem matches. An organism's shape, energy economy, movement, sensing, and environmental adaptations determine its performance.

## Cell construction

- A cell contains 4 to 16 elements on a 4 x 4 grid.
- Grid positions 5, 6, 9, and 10 form the locked 2 x 2 nucleus.
- The remaining positions may contain organelles or remain empty.
- Directional organelles face outward from the nucleus.
- Sensors connect to engines through explicit grid-slot wiring. A sensor may drive many
  engines, each engine accepts one sensor input, and inversion is configured per output.

The full organelle catalogue is in [`notes/CELL_ELEMENTS.md`](notes/CELL_ELEMENTS.md).
Runtime tuning is documented in [`notes/BALANCE_CONFIG.md`](notes/BALANCE_CONFIG.md).

## Match rules

- Cells collect food by touching it.
- A cell duplicates after accumulating biomass equal to its element count minus twice its Ribosome count (minimum 1).
- Food and biomass are the same resource: pickups and Chloroplasts increase it, while metabolism and organelle activation consume it.
- On duplication, the cell's biomass is split equally between parent and daughter.
- Every 10 seconds, a cell passively consumes one food.
- A cell dies when its food reaches its negative survival limit. The base limit is -4, and each Mitochondria extends it by one.
- Movement loses speed over time through drag.
- Cells overlap and pass through each other without collision response.
- Cells receive passive random movement impulses.
- Passive random rotation has double-strength angular force and occurs on 50% of simulation updates.
- Cells receive no direct player input and no automatic AI steering toward food.
- Movement organelles apply force outward from the nucleus.
- Effective, standard, and Rotation Engines use explicit sensor inputs when connected;
  unconnected engines retain their 50% fallback. Random Engines cannot be connected.
- Rotation Engines turn clockwise from the left half of the grid and counterclockwise from the right half.
- Each AI competitor may use its own JSON blueprint; absent or invalid configurations fall back to full-pool random generation.
- The simulation advances at a fixed 60 updates per second, independent of rendering frame rate.
- Forward engines attempt activation at most once every 10 seconds and pay food only when they activate.
- Engines in sensor-equipped cells activate only when a sensor is aligned; the 50% random fallback applies only to cells without sensors.
- Linear engine impulses use the strengthened 8/4/8 power values for Random, Effective, and standard Engines respectively; Rotation Engine torque is unchanged.
- Each Slippery Membrane halves remaining drag, so multiple membranes stack multiplicatively.
- Match ranking first compares copies produced, then remaining biomass.

## Scale and constants

Balance values are loaded from `project/balance/environment.json` and the per-organelle
files under `project/balance/organelles/`. The table below describes structural units;
see the balance files for current tunable values.

| Symbol | Value | Meaning |
|---|---:|---|
| T | 10 seconds | Simulation tick interval |
| C | 1 food | Passive consumption per tick |
| S | 16 pixels | Base visual size unit |
| Simulation rate | 60 updates/second | Fixed deterministic update rate |

## Gradients

Gradient directions are calculated on demand only when a cell's sensor is evaluated; the simulation does not maintain an arena-wide gradient grid. Each source contributes an inverse-distance-squared concentration, and the resulting spatial derivative points toward increasing concentration.

Each sensor faces outward according to its grid position, rotated with the cell. Gradient sensors activate when their facing direction is within 45 degrees of the increasing-concentration direction. Cell sensors exclude the sensing cell itself. Food Vision is evaluated separately and detects food within 8 S and a 30-degree forward cone.

## Environments

An environment affects a cell when more than half of the cell is inside its zone.

| Environment | Effect |
|---|---|
| Normal | No modifier |
| Viscous | Doubles drag |
| Toxic | Doubles passive food drain |
| Turbulent | Doubles random movement and rotation |
| Nutritious | Doubles food gained from collection |

## Screens

| Screen | Purpose |
|---|---|
| Main menu | Entry point and navigation |
| Organism builder | Assemble and edit an organelle grid |
| Gameplay arena | Run and display a simulation match |
| Results | Explain match outcomes and progression; planned |

## Progression

Matches should award resources or points used to unlock organelles and new environments. Progression should broaden viable strategies rather than only increase raw power. Exact economy and unlock pacing remain to be designed.
