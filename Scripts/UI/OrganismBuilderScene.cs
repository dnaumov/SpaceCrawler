using System.Collections.Generic;
using Godot;

public partial class OrganismBuilderScene : Control
{
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
				{ "component", ComponentName },
				{ "source", SourceList }
			};
		}
	}

	private sealed partial class ComponentDropList : VBoxContainer
	{
		[Signal]
		public delegate void ComponentDroppedEventHandler(string componentName, string sourceList);

		public string AcceptedSource { get; set; } = string.Empty;

		public override bool _CanDropData(Vector2 atPosition, Variant data)
		{
			return TryReadDropData(data, out _, out var sourceList) && sourceList == AcceptedSource;
		}

		public override void _DropData(Vector2 atPosition, Variant data)
		{
			if (!TryReadDropData(data, out var componentName, out var sourceList))
			{
				return;
			}

			EmitSignal(SignalName.ComponentDropped, componentName, sourceList);
		}

		private static bool TryReadDropData(Variant data, out string componentName, out string sourceList)
		{
			componentName = string.Empty;
			sourceList = string.Empty;
			if (data.VariantType != Variant.Type.Dictionary)
			{
				return false;
			}

			var dictionary = data.AsGodotDictionary();
			if (!dictionary.ContainsKey("component") || !dictionary.ContainsKey("source"))
			{
				return false;
			}

			componentName = dictionary["component"].AsString();
			sourceList = dictionary["source"].AsString();
			return !string.IsNullOrEmpty(componentName) && !string.IsNullOrEmpty(sourceList);
		}
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

	private readonly List<string> _selectedComponents = [];

	private ComponentDropList _availableList = default!;
	private ComponentDropList _selectedList = default!;
	private Label _statusLabel = default!;

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildUi();
		RefreshSelectedList();
	}

	private void BuildUi()
	{
		var margin = new MarginContainer();
		margin.SetAnchorsPreset(LayoutPreset.FullRect);
		margin.AddThemeConstantOverride("margin_left", 16);
		margin.AddThemeConstantOverride("margin_top", 16);
		margin.AddThemeConstantOverride("margin_right", 16);
		margin.AddThemeConstantOverride("margin_bottom", 16);
		AddChild(margin);

		var rootColumn = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		margin.AddChild(rootColumn);

		rootColumn.AddChild(new Label
		{
			Text = "Organism Builder",
			HorizontalAlignment = HorizontalAlignment.Center
		});

		rootColumn.AddChild(new Label
		{
			Text = "Drag components from Available to Loadout. Drag from Loadout back to Available to remove."
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

		_availableList = new ComponentDropList
		{
			AcceptedSource = "selected",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_availableList.ComponentDropped += OnComponentDroppedToAvailable;
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

		var selectedPanel = new PanelContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		split.AddChild(selectedPanel);

		var selectedColumn = new VBoxContainer();
		selectedPanel.AddChild(selectedColumn);
		selectedColumn.AddChild(new Label { Text = "Organism loadout" });

		_selectedList = new ComponentDropList
		{
			AcceptedSource = "available",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_selectedList.ComponentDropped += OnComponentDroppedToLoadout;
		selectedColumn.AddChild(_selectedList);

		_statusLabel = new Label();
		rootColumn.AddChild(_statusLabel);
	}

	private void RefreshSelectedList()
	{
		foreach (var child in _selectedList.GetChildren())
		{
			child.QueueFree();
		}

		if (_selectedComponents.Count == 0)
		{
			_selectedList.AddChild(new Label
			{
				Text = "Drop components here"
			});
		}
		else
		{
			foreach (var component in _selectedComponents)
			{
				_selectedList.AddChild(new ComponentItemLabel
				{
					Text = component,
					ComponentName = component,
					SourceList = "selected"
				});
			}
		}

		_statusLabel.Text = $"Selected components: {_selectedComponents.Count}";
	}

	private void OnComponentDroppedToLoadout(string componentName, string sourceList)
	{
		if (sourceList != "available")
		{
			return;
		}

		if (_selectedComponents.Contains(componentName))
		{
			_statusLabel.Text = $"{componentName} is already in the loadout.";
			return;
		}

		_selectedComponents.Add(componentName);
		RefreshSelectedList();
	}

	private void OnComponentDroppedToAvailable(string componentName, string sourceList)
	{
		if (sourceList != "selected")
		{
			return;
		}

		if (_selectedComponents.Remove(componentName))
		{
			RefreshSelectedList();
		}
	}
}
