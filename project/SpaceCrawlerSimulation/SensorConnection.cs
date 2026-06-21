/// <summary>
/// A directed sensor output. One sensor may own many outputs, while validation
/// guarantees that each engine slot has at most one input.
/// </summary>
public readonly record struct SensorConnection(
    int SensorSlot,
    int EngineSlot,
    bool Inverted = false);
