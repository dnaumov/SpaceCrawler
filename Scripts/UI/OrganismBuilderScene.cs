using System.Collections.Generic;
using Godot;

public partial class OrganismBuilderScene : Control
{
	private static class DragPayload
	{
		public const string ComponentKey = "component";
		public const string SourceKey = "source";
		public const string SourceNodeKey = "source_node";
	}

	private sealed partial class ComponentItemLabel : Label
	{
		public string ComponentName { get; set; } = string.Empty;
		public string SourceList { get; set; } = string.Empty;

		public override Variant _GetDragData(Vector2 atPosition)
		{
			if (string.IsNullOrEmpty(ComponentName) || string.IsNullOrEmpty(SourceList))
			{
				return default;
			}

			var preview = new Label
			{
				Text = ComponentName
			};
			SetDragPreview(preview);

			return new Godot.Collections.Dictionary
			{
				{ DragPayload.ComponentKey, ComponentName },
				{ DragPayload.SourceKey, SourceList },
				{ DragPayload.SourceNodeKey, -1 }
			};
		}
	}

	private sealed partial class RemoveDropZone : PanelContainer
	{
		[Signal]
		public delegate void ComponentRemovedEventHandler(string componentName, string sourceList, int sourceNodeIndex);

		public override bool _CanDropData(Vector2 atPosition, Variant data)
		{
			return TryReadDropData(data, out _, out var sourceList, out _) && sourceList == "grid";
		}

		public override void _DropData(Vector2 atPosition, Variant data)
		{
			if (!TryReadDropData(data, out var componentName, out var sourceList, out var sourceNodeIndex))
			{
				return;
			}

			EmitSignal(SignalName.ComponentRemoved, componentName, sourceList, sourceNodeIndex);
		}
	}

	private sealed partial class GridNodeSlot : PanelContainer
	{
		[Signal]
		public delegate void ComponentDroppedEventHandler(int targetNodeIndex, string componentName, string sourceList, int sourceNodeIndex);

		public int NodeIndex { get; set; } = -1;

		private string _componentName = string.Empty;
		private readonly Label _contentLabel = new()
		{
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};

		public override void _Ready()
		{
			CustomMinimumSize = new Vector2(120.0f, 84.0f);
			var center = new CenterContainer();
			center.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			center.SizeFlagsVertical = SizeFlags.ExpandFill;
			AddChild(center);

			_contentLabel.SizeFlagsHorizontal = SizeFlags.ExpandFill;
			_contentLabel.SizeFlagsVertical = SizeFlags.ExpandFill;
			center.AddChild(_contentLabel);
			UpdateLabel();
		}

		public void SetComponent(string? componentName)
		{
			_componentName = componentName ?? string.Empty;
			UpdateLabel();
		}

		public override Variant _GetDragData(Vector2 atPosition)
		{
			if (string.IsNullOrEmpty(_componentName))
			{
				return default;
			}

			var preview = new Label
			{
				Text = _componentName
			};
			SetDragPreview(preview);

			return new Godot.Collections.Dictionary
			{
				{ DragPayload.ComponentKey, _componentName },
				{ DragPayload.SourceKey, "grid" },
				{ DragPayload.SourceNodeKey, NodeIndex }
			};
		}

		public override bool _CanDropData(Vector2 atPosition, Variant data)
		{
			return TryReadDropData(data, out _, out var sourceList, out _)
				&& (sourceList == "available" || sourceList == "grid");
		}

		public override void _DropData(Vector2 atPosition, Variant data)
		{
			if (!TryReadDropData(data, out var componentName, out var sourceList, out var sourceNodeIndex))
			{
				return;
			}

			EmitSignal(SignalName.ComponentDropped, NodeIndex, componentName, sourceList, sourceNodeIndex);
		}

		private void UpdateLabel()
		{
			var nodeText = $"Node {NodeIndex + 1}";
			_contentLabel.Text = string.IsNullOrEmpty(_componentName)
				? $"{nodeText}\n(Empty)"
				: $"{nodeText}\n{_componentName}";
		}
	}

	private const int GridSize = 4;
	private const int GridNodeCount = GridSize * GridSize;

	private static bool TryReadDropData(Variant data, out string componentName, out string sourceList, out int sourceNodeIndex)
	{
		componentName = string.Empty;
		sourceList = string.Empty;
		sourceNodeIndex = -1;
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return false;
		}

		var dictionary = data.AsGodotDictionary();
		if (!dictionary.ContainsKey(DragPayload.ComponentKey) || !dictionary.ContainsKey(DragPayload.SourceKey))
		{
			return false;
		}

		componentName = dictionary[DragPayload.ComponentKey].AsString();
		sourceList = dictionary[DragPayload.SourceKey].AsString();
		if (dictionary.ContainsKey(DragPayload.SourceNodeKey))
		{
			sourceNodeIndex = dictionary[DragPayload.SourceNodeKey].AsInt32();
		}

		return !string.IsNullOrEmpty(componentName) && !string.IsNullOrEmpty(sourceList);
	}

	private readonly string[] _availableComponents =
	[
		"Core membrane",
		"Flagella engine",
		"Sensor cilia",
		"Mitochondria cluster",
		"Spike weapon",
		"Armor plate"
	];

	private readonly string?[] _gridComponents = new string?[GridNodeCount];
	private readonly Dictionary<string, int> _componentNodeLookup = [];
	private readonly List<GridNodeSlot> _gridSlots = [];

	private VBoxContainer _availableList = new();
	private RemoveDropZone _removeDropZone = new();
	private Label _statusLabel = new();

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildUi();
		RefreshGridState();
	}

	private void BuildUi()
	{
		var marginContainer = new MarginContainer();
		marginContainer.SetAnchorsPreset(LayoutPreset.FullRect);
		marginContainer.AddThemeConstantOverride("margin_left", 16);
		marginContainer.AddThemeConstantOverride("margin_top", 16);
		marginContainer.AddThemeConstantOverride("margin_right", 16);
		marginContainer.AddThemeConstantOverride("margin_bottom", 16);
		AddChild(marginContainer);

		var rootColumn = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		marginContainer.AddChild(rootColumn);

		rootColumn.AddChild(new Label
		{
			Text = "Organism Builder",
			HorizontalAlignment = HorizontalAlignment.Center
		});

		rootColumn.AddChild(new Label
		{
			Text = "Drag components from Available components into the 4x4 cell grid."
		});

		var split = new HSplitContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		rootColumn.AddChild(split);

		var availablePanel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		split.AddChild(availablePanel);

		var availableColumn = new VBoxContainer();
		availablePanel.AddChild(availableColumn);
		availableColumn.AddChild(new Label { Text = "Available components" });

		_availableList = new VBoxContainer();
		_availableList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_availableList.SizeFlagsVertical = SizeFlags.ExpandFill;
		availableColumn.AddChild(_availableList);

		foreach (var component in _availableComponents)
		{
			_availableList.AddChild(new ComponentItemLabel
			{
				Text = component,
				ComponentName = component,
				SourceList = "available"
			});
		}

		_removeDropZone.CustomMinimumSize = new Vector2(0.0f, 48.0f);
		_removeDropZone.ComponentRemoved += OnComponentRemovedFromGrid;
		availableColumn.AddChild(_removeDropZone);
		_removeDropZone.AddChild(new Label
		{
			Text = "Drop here to remove from grid",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center
		});

		var gridPanel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		split.AddChild(gridPanel);

		var gridColumn = new VBoxContainer();
		gridPanel.AddChild(gridColumn);
		gridColumn.AddChild(new Label { Text = "Cell layout grid (4x4)" });

		var gridContainer = new GridContainer
		{
			Columns = GridSize,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		gridColumn.AddChild(gridContainer);

		for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
		{
			var slot = new GridNodeSlot
			{
				NodeIndex = nodeIndex,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill
			};
			slot.ComponentDropped += OnComponentDroppedToGridNode;
			_gridSlots.Add(slot);
			gridContainer.AddChild(slot);
		}

		rootColumn.AddChild(_statusLabel);
	}

	private void RefreshGridState()
	{
		_componentNodeLookup.Clear();
		for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
		{
			var component = _gridComponents[nodeIndex];
			_gridSlots[nodeIndex].SetComponent(component);
			if (!string.IsNullOrEmpty(component))
			{
				_componentNodeLookup[component] = nodeIndex;
			}
		}

		_statusLabel.Text = $"Placed components: {_componentNodeLookup.Count}/{GridNodeCount}";
	}

	private void OnComponentDroppedToGridNode(int targetNodeIndex, string componentName, string sourceList, int sourceNodeIndex)
	{
		if (sourceList == "available")
		{
			if (_componentNodeLookup.TryGetValue(componentName, out var currentNodeIndex))
			{
				if (currentNodeIndex == targetNodeIndex)
				{
					_statusLabel.Text = $"{componentName} is already on node {targetNodeIndex + 1}.";
					return;
				}

				_gridComponents[currentNodeIndex] = null;
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

		_gridComponents[sourceNodeIndex] = null;
		RefreshGridState();
	}
}
