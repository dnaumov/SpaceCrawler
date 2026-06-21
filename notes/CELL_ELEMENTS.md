# Cell Elements

This document describes all organelle types available in the cell builder.

A cell occupies a **4×4 grid** with **4–16 elements**. The four center slots are always **Nucleus** and cannot be changed. The remaining 12 outer slots accept any of the organelles below.

Organelle **orientation** is always *away from the nucleus* (pointing toward the cell's outer edge from the center).

---

## Movement organelles

Forward engines fire along the cell's current orientation. Rotation Engines apply torque based on which half of the grid contains them.

| Organelle           | Serialised name      | Output        | Food cost | Activation                                                |
|---------------------|----------------------|---------------|-----------|-----------------------------------------------------------|
| **Random Engine**   | `RandomEngine`       | 8 speed       | 2 food    | One attempt per [T], with a **50% chance** to activate.  |
| **Eff. Engine**     | `EffectiveEngine`    | 4 speed       | 1 food    | One attempt per [T]; sensor-controlled, or 50% without one. |
| **Engine**          | `Engine`             | 8 speed       | 3 food    | One attempt per [T]; sensor-controlled, or 50% without one. |
| **Rotation Engine** | `RotationEngine`     | 2 angular impulse | 1 food | Once per [T]. Sensor-active, otherwise 50% chance.       |

> **Rotation direction**: Rotation Engines in grid columns 0-1 apply clockwise torque; columns 2-3 apply counterclockwise torque. Opposing engines cancel, but every activated engine still costs food.

> **Trade-off summary**: Random Engine is cheap (2 food) but fires unpredictably. Effective Engine gives the best food-efficiency ratio when paired with a sensor. Engine gives maximum forward speed but is costly. Rotation Engine enables deliberate turning without direct player or AI steering.

> Forward engines consume food only when an interval-based activation succeeds; they never charge per rendering or simulation update. If the cell has a sensor, connected engines obey its result. The 50% random fallback is used only when the cell has no sensor.

---

## Storage & energy organelles

| Organelle       | Serialised name  | Effect                                                              |
|-----------------|------------------|---------------------------------------------------------------------|
| **Mitochondria**| `Mitochondria`   | Allows the cell to survive 1 extra unit of negative food.          |
| **Chloroplast** | `Chloroplast`    | Produces **1 food every 40 seconds** passively.                    |
| **Ribosome**    | `Ribosome`       | Reduces the biomass requirement for duplication by **2**.          |

> **Tip**: Multiple Mitochondria stack — a cell with 2 Mitochondria can survive down to −6 food before dying.

---

> **Biomass and fuel are the same resource**: Physical food and Chloroplast production increase it; metabolism and organelle activation reduce it. When a cell reaches its duplication threshold, its biomass is split equally between parent and daughter. Because a Ribosome occupies one grid slot but reduces the threshold by two, adding one to an empty slot still lowers the final requirement by one.

---

## Sensory organelles

Sensory organelles have an orientation away from the nucleus. They can be **connected to one or more movement organelles**; connections can also be *inverse* (sensor off → engine activates). When connected, the engine checks the sensor instead of rolling the 50% chance.

| Organelle                     | Serialised name          | Activates when …                                                            |
|-------------------------------|--------------------------|-----------------------------------------------------------------------------|
| **Food Sensor** (gradient)    | `FoodGradientDetector`   | Sensor direction is aligned with the food concentration gradient.           |
| **Cell Sensor** (gradient)    | `CellsGradientDetector`  | Sensor direction is aligned with the cell-concentration gradient.           |
| **Toxic Sensor** (gradient)   | `ToxicGradientDetector`  | Sensor direction is aligned with the toxic-environment gradient.            |
| **Food Vision**               | `FoodVision`             | Food is within 8 S and the organelle's 30-degree forward cone.              |

Gradient directions are calculated only when sensors are evaluated. A gradient sensor
activates when its outward-facing direction is within 45 degrees of increasing
concentration. Food Vision is evaluated separately and does not use concentration.

> **Note on connections**: In the current builder, sensors are automatically connected to all movement organelles on the same cell. Support for selecting individual connections and inverse links is planned for a future stage.

---

## Other organelles

| Organelle            | Serialised name   | Effect                                                              | Cost                         |
|----------------------|-------------------|---------------------------------------------------------------------|------------------------------|
| **Slip. Membrane**   | `SlipperyMembrane`| Halves remaining drag; multiple membranes stack multiplicatively.   | **1 food per 2 [T]** (20 s). |
| **Toxin Prod.**      | `ToxinProducer`   | Makes one surrounding grid position toxic each [T].                 | **1 food per 2 [T]** (20 s). |

---

## Nucleus (locked)

| Organelle  | Serialised name | Grid positions (4×4) |
|------------|-----------------|----------------------|
| **Nucleus**| `Nucleus`       | Slots 5, 6, 9, 10 (the center 2×2 block) — always present, cannot be removed. |

---

## Grid layout reference

```
Col →    0    1    2    3
Row 0:   0    1    2    3
Row 1:   4   [5]  [6]   7
Row 2:   8   [9] [10]  11
Row 3:  12   13   14   15
```

`[n]` = Nucleus (locked). All other slots accept any organelle or remain empty.

---

## Environment compatibility

| Environment   | Recommended organelles                                |
|---------------|-------------------------------------------------------|
| Viscous       | Slip. Membrane (counter drag); avoid heavy Engine use |
| Toxic         | Mitochondria (extend survival); Chloroplast (income)  |
| Turbulent     | Any; random movement is amplified anyway              |
| Nutritious    | Any; food yield is doubled — prioritise duplication builds |

---

> For full game rules and constants, see [`Design.md`](../Design.md).
