# Cell Elements

This document describes all organelle types available in the cell builder.

A cell occupies a **4×4 grid** with **4–16 elements**. The four center slots are always **Nucleus** and cannot be changed. The remaining 12 outer slots accept any of the organelles below.

Organelle **orientation** is always *away from the nucleus* (pointing toward the cell's outer edge from the center).

---

## Movement organelles

Movement organelles have an orientation away from the nucleus and fire in that direction.

| Organelle          | Serialised name     | Speed    | Food cost | Activation                                                |
|--------------------|---------------------|----------|-----------|-----------------------------------------------------------|
| **Random Engine**  | `RandomEngine`      | 2 speed  | 2 food    | Once per [T]. Activates with **50% chance** each [T].    |
| **Eff. Engine**    | `EffectiveEngine`   | 1 speed  | 1 food    | Once per [T]. Needs a connected sensor; without one: 50% chance. |
| **Engine**         | `Engine`            | 2 speed  | 3 food    | Once per [T]. Needs a connected sensor; without one: 50% chance. |

> **Trade-off summary**: Random Engine is cheap (2 food) but wastes impulse in random directions. Effective Engine gives the best food-efficiency ratio when paired with a sensor. Engine gives maximum speed when guided by a sensor but is costly to run unguided.

---

## Storage & energy organelles

| Organelle       | Serialised name  | Effect                                                              |
|-----------------|------------------|---------------------------------------------------------------------|
| **Mitochondria**| `Mitochondria`   | Allows the cell to survive 1 extra unit of negative food.          |
| **Chloroplast** | `Chloroplast`    | Produces **1 food every 40 seconds** passively.                    |
| **Ribosome**    | `Ribosome`       | Reduces the food-collection requirement for duplication by **1**.  |

> **Tip**: Multiple Mitochondria stack — a cell with 2 Mitochondria can survive down to −6 food before dying.

---

## Sensory organelles

Sensory organelles have an orientation away from the nucleus. They can be **connected to one or more movement organelles**; connections can also be *inverse* (sensor off → engine activates). When connected, the engine checks the sensor instead of rolling the 50% chance.

| Organelle                     | Serialised name          | Activates when …                                                            |
|-------------------------------|--------------------------|-----------------------------------------------------------------------------|
| **Food Sensor** (gradient)    | `FoodGradientDetector`   | Sensor direction is aligned with the food concentration gradient.           |
| **Cell Sensor** (gradient)    | `CellsGradientDetector`  | Sensor direction is aligned with the cell-concentration gradient.           |
| **Toxic Sensor** (gradient)   | `ToxicGradientDetector`  | Sensor direction is aligned with the toxic-environment gradient.            |
| **Food Vision**               | `FoodVision`             | A food item is directly in front of the organelle (line-of-sight check).   |

> **Note on connections**: In the current builder, sensors are automatically connected to all movement organelles on the same cell. Support for selecting individual connections and inverse links is planned for a future stage.

---

## Other organelles

| Organelle            | Serialised name   | Effect                                                              | Cost                         |
|----------------------|-------------------|---------------------------------------------------------------------|------------------------------|
| **Slip. Membrane**   | `SlipperyMembrane`| Reduces drag by **2×** (stacks multiplicatively with environment).  | **1 food per 2 [T]** (20 s). |
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

> For full game rules and constants see [`README.md`](README.md).
