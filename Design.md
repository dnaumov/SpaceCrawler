# Product Design

## Overview

The player constructs a microorganism from modular organelles on a 4 x 4 grid and tests it in automated ecosystem matches. An organism's shape, energy economy, movement, sensing, and environmental adaptations determine its performance.

## Cell construction

- A cell contains 4 to 16 elements on a 4 x 4 grid.
- Grid positions 5, 6, 9, and 10 form the locked 2 x 2 nucleus.
- The remaining positions may contain organelles or remain empty.
- Directional organelles face outward from the nucleus.
- Sensors may control movement organelles; selectable and inverse connections are planned.

The full organelle catalogue is in [`notes/CELL_ELEMENTS.md`](notes/CELL_ELEMENTS.md).

## Match rules

- Cells collect food by touching it.
- A cell duplicates after collecting food equal to its element count; each Ribosome lowers the requirement by one.
- Every 10 seconds, a cell passively consumes one food.
- A cell dies when its food reaches its negative survival limit. The base limit is -4, and each Mitochondria extends it by one.
- Movement loses speed over time through drag.
- Cell collisions produce an elastic bounce.
- Cells receive small random movement and rotation impulses.
- Movement organelles apply force outward from the nucleus.

## Scale and constants

| Symbol | Value | Meaning |
|---|---:|---|
| T | 10 seconds | Simulation tick interval |
| C | 1 food | Passive consumption per tick |
| S | 16 pixels | Base visual size unit |

## Gradients

At every simulation tick, the game recalculates food, cell-concentration, and toxic-environment gradients for each grid position. A contribution is inversely proportional to squared distance, with a small epsilon to prevent division by zero.

Sensors compare their orientation with these gradients to decide whether connected movement organelles should activate.

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

