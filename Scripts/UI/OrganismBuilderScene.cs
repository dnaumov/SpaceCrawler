using System.Collections.Generic;
using Godot;

public partial class OrganismBuilderScene : Control
{
	private const int GridSize = 4;
	private const int GridNodeCount = GridSize * GridSize;

	private readonly string[] _gridComponents = new string[GridNodeCount];
	private readonly List<GridNodeSlot> _gridSlots = [];

	private GridContainer _gridContainer = null!;
	private RemoveDropZone _removeDropZone = null!;
	private Label _statusLabel = null!;

	public override void _Ready()
	{
		BindUiNodes();
		BindGridSlots();
		_removeDropZone.ComponentRemoved += OnComponentRemovedFromGrid;
		RefreshGridState();
	}

	private void BindUiNodes()
	{
		_gridContainer = GetNode<GridContainer>("%BuilderGrid");
		_removeDropZone = GetNode<RemoveDropZone>("%RemoveDropZone");
		_statusLabel = GetNode<Label>("%StatusLabel");
	}

	private void BindGridSlots()
	{
		_gridSlots.Clear();
		var indexedSlots = new GridNodeSlot[GridNodeCount];
		foreach (var child in _gridContainer.GetChildren())
		{
			if (child is not GridNodeSlot slot)
			{
				continue;
			}

			if (slot.NodeIndex < 0 || slot.NodeIndex >= GridNodeCount)
			{
				GD.PushWarning($"GridNodeSlot '{slot.Name}' has invalid NodeIndex {slot.NodeIndex}.");
				continue;
			}

			if (indexedSlots[slot.NodeIndex] is not null)
			{
				GD.PushWarning($"Duplicate GridNodeSlot for NodeIndex {slot.NodeIndex}: '{slot.Name}'.");
				continue;
			}

			slot.ComponentDropped += OnComponentDroppedToGridNode;
			indexedSlots[slot.NodeIndex] = slot;
		}

		for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
		{
			var slot = indexedSlots[nodeIndex];
			if (slot is null)
			{
				GD.PushWarning($"Missing GridNodeSlot for NodeIndex {nodeIndex}.");
				continue;
			}

			_gridSlots.Add(slot);
		}
	}

	private void RefreshGridState()
	{
		var placedCount = 0;
		for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
		{
			var component = _gridComponents[nodeIndex];
			if (nodeIndex < _gridSlots.Count)
			{
				_gridSlots[nodeIndex].SetComponent(component);
			}

			if (!string.IsNullOrEmpty(component))
			{
				placedCount += 1;
			}
		}

		_statusLabel.Text = $"Placed components: {placedCount}/{GridNodeCount}";
	}

	private int FindComponentNode(string componentName)
	{
		for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
		{
			if (_gridComponents[nodeIndex] == componentName)
			{
				return nodeIndex;
			}
		}

		return -1;
	}

	private void OnComponentDroppedToGridNode(int targetNodeIndex, string componentName, string sourceList, int sourceNodeIndex)
	{
		if (targetNodeIndex < 0 || targetNodeIndex >= GridNodeCount)
		{
			return;
		}

		if (sourceList == "available")
		{
			var currentNodeIndex = FindComponentNode(componentName);
			if (currentNodeIndex >= 0)
			{
				if (currentNodeIndex == targetNodeIndex)
				{
					_statusLabel.Text = $"{componentName} is already on node {targetNodeIndex + 1}.";
					return;
				}

				_gridComponents[currentNodeIndex] = string.Empty;
			}

			_gridComponents[targetNodeIndex] = componentName;
			RefreshGridState();
			return;
		}

		if (sourceList != "grid" || sourceNodeIndex < 0 || sourceNodeIndex >= GridNodeCount)
		{
			return;
		}

		if (sourceNodeIndex == targetNodeIndex)
		{
			_statusLabel.Text = $"{componentName} is already on node {targetNodeIndex + 1}.";
			return;
		}

		if (_gridComponents[sourceNodeIndex] != componentName)
		{
			return;
		}

		var targetComponent = _gridComponents[targetNodeIndex];
		_gridComponents[targetNodeIndex] = componentName;
		_gridComponents[sourceNodeIndex] = targetComponent;
		RefreshGridState();
	}

	private void OnComponentRemovedFromGrid(string componentName, string sourceList, int sourceNodeIndex)
	{
		if (sourceList != "grid" || sourceNodeIndex < 0 || sourceNodeIndex >= GridNodeCount)
		{
			return;
		}

		if (_gridComponents[sourceNodeIndex] != componentName)
		{
			return;
		}

		_gridComponents[sourceNodeIndex] = string.Empty;
		RefreshGridState();
	}
}
