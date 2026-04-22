using Godot;

public partial class GridNodeSlot : PanelContainer
{
private const float SlotMinWidth = 120.0f;
private const float SlotMinHeight = 84.0f;

[Signal]
public delegate void ComponentDroppedEventHandler(int targetNodeIndex, string componentName, string sourceList, int sourceNodeIndex);

[Export]
public int NodeIndex { get; set; } = -1;

private string _componentName = string.Empty;
private Label _contentLabel = null!;

public override void _Ready()
{
CustomMinimumSize = new Vector2(SlotMinWidth, SlotMinHeight);
_contentLabel = GetNodeOrNull<Label>("ContentLabel");
if (_contentLabel is null)
{
var center = new CenterContainer
{
SizeFlagsHorizontal = SizeFlags.ExpandFill,
SizeFlagsVertical = SizeFlags.ExpandFill
};
AddChild(center);

_contentLabel = new Label
{
Name = "ContentLabel",
HorizontalAlignment = HorizontalAlignment.Center,
VerticalAlignment = VerticalAlignment.Center,
AutowrapMode = TextServer.AutowrapMode.WordSmart,
SizeFlagsHorizontal = SizeFlags.ExpandFill,
SizeFlagsVertical = SizeFlags.ExpandFill
};
center.AddChild(_contentLabel);
}
UpdateLabel();
}

public void SetComponent(string componentName)
{
_componentName = componentName;
UpdateLabel();
}

public override Variant _GetDragData(Vector2 atPosition)
{
if (string.IsNullOrEmpty(_componentName))
{
return default;
}

SetDragPreview(new Label { Text = _componentName });
return new Godot.Collections.Dictionary
{
{ DragPayload.ComponentKey, _componentName },
{ DragPayload.SourceKey, "grid" },
{ DragPayload.SourceNodeKey, NodeIndex }
};
}

public override bool _CanDropData(Vector2 atPosition, Variant data)
{
return DragPayload.TryRead(data, out _, out var sourceList, out _)
&& (sourceList == "available" || sourceList == "grid");
}

public override void _DropData(Vector2 atPosition, Variant data)
{
if (!DragPayload.TryRead(data, out var componentName, out var sourceList, out var sourceNodeIndex))
{
return;
}

EmitSignal(SignalName.ComponentDropped, NodeIndex, componentName, sourceList, sourceNodeIndex);
}

private void UpdateLabel()
{
if (_contentLabel is null)
{
return;
}

var nodeText = $"Node {NodeIndex + 1}";
_contentLabel.Text = string.IsNullOrEmpty(_componentName)
? $"{nodeText}\n(Empty)"
: $"{nodeText}\n{_componentName}";
}
}
