using Godot;

public partial class RemoveDropZone : PanelContainer
{
[Signal]
public delegate void ComponentRemovedEventHandler(string componentName, string sourceList, int sourceNodeIndex);

public override bool _CanDropData(Vector2 atPosition, Variant data)
{
return DragPayload.TryRead(data, out _, out var sourceList, out _) && sourceList == "grid";
}

public override void _DropData(Vector2 atPosition, Variant data)
{
if (!DragPayload.TryRead(data, out var componentName, out var sourceList, out var sourceNodeIndex))
{
return;
}

EmitSignal(SignalName.ComponentRemoved, componentName, sourceList, sourceNodeIndex);
}
}
