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

	private ComponentDropList _availableList = new();
	private ComponentDropList _selectedList = new();
	private Label _statusLabel = new();
	private Label _emptyLoadoutLabel = new() { Text = "Drop components here" };

	public override void _Ready()
	{
		SetAnchorsPreset(LayoutPreset.FullRect);
		BuildUi();
		RefreshSelectedList();
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

		_availableList.AcceptedSource = "selected";
		_availableList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_availableList.SizeFlagsVertical = SizeFlags.ExpandFill;
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

		_selectedList.AcceptedSource = "available";
		_selectedList.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_selectedList.SizeFlagsVertical = SizeFlags.ExpandFill;
		_selectedList.ComponentDropped += OnComponentDroppedToLoadout;
		selectedColumn.AddChild(_selectedList);

		rootColumn.AddChild(_statusLabel);
	}

	private void RefreshSelectedList()
	{
		foreach (var child in _selectedList.GetChildren())
		{
			if (child != _emptyLoadoutLabel)
			{
				child.Free();
			}
		}

		if (_selectedComponents.Count == 0)
		{
			if (_emptyLoadoutLabel.GetParent() == null)
			{
				_selectedList.AddChild(_emptyLoadoutLabel);
			}

			_emptyLoadoutLabel.Visible = true;
		}
		else
		{
			_emptyLoadoutLabel.Visible = false;

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
