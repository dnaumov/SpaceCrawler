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
	private bool _isNucleus;
	private Label _contentLabel = null!;

	public override void _Ready()
	{
		CustomMinimumSize = new Vector2(SlotMinWidth, SlotMinHeight);
		_contentLabel = GetNode<Label>("ContentLabel");
		UpdateLabel();
	}

	public void SetComponent(string componentName)
	{
		_componentName = componentName;
		UpdateLabel();
	}

	/// <summary>
	/// Marks this slot as a permanent nucleus cell that cannot be dragged from or dropped onto.
	/// </summary>
	public void SetNucleus()
	{
		_isNucleus = true;
		_componentName = OrganelleType.Nucleus.SerializedName();
		UpdateLabel();
	}

	public override Variant _GetDragData(Vector2 atPosition)
	{
		if (_isNucleus || string.IsNullOrEmpty(_componentName))
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
		if (_isNucleus)
		{
			return false;
		}

		return DragPayload.TryRead(data, out _, out var sourceList, out _)
			&& (sourceList == "available" || sourceList == "grid");
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (_isNucleus)
		{
			return;
		}

		if (!DragPayload.TryRead(data, out var componentName, out var sourceList, out var sourceNodeIndex))
		{
			return;
		}

		EmitSignal(SignalName.ComponentDropped, NodeIndex, componentName, sourceList, sourceNodeIndex);
	}

	private void UpdateLabel()
	{
		if (_isNucleus)
		{
			_contentLabel.Text = "Nucleus\n[locked]";
			return;
		}

		var nodeText = $"Node {NodeIndex + 1}";
		_contentLabel.Text = string.IsNullOrEmpty(_componentName)
			? $"{nodeText}\n(Empty)"
			: $"{nodeText}\n{_componentName}";
	}
}
