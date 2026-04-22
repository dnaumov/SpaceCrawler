using Godot;

public partial class ComponentItemLabel : Label
{
[Export]
public string ComponentName { get; set; } = string.Empty;

[Export]
public string SourceList { get; set; } = "available";

public override void _Ready()
{
if (string.IsNullOrEmpty(ComponentName))
{
ComponentName = Text;
}
}

public override Variant _GetDragData(Vector2 atPosition)
{
var componentName = string.IsNullOrEmpty(ComponentName) ? Text : ComponentName;
if (string.IsNullOrEmpty(componentName) || string.IsNullOrEmpty(SourceList))
{
return default;
}

SetDragPreview(new Label { Text = componentName });
return new Godot.Collections.Dictionary
{
{ DragPayload.ComponentKey, componentName },
{ DragPayload.SourceKey, SourceList },
{ DragPayload.SourceNodeKey, -1 }
};
}
}
