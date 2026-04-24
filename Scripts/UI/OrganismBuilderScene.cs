using System;
using System.Collections.Generic;
using Godot;

public partial class OrganismBuilderScene : Control
{
private const int GridSize = 4;
private const int GridNodeCount = GridSize * GridSize;
private const string OrganismConfigPath = "user://organism_config.json";

private readonly string[] _gridComponents = new string[GridNodeCount];
private readonly List<GridNodeSlot> _gridSlots = [];

private GridContainer _gridContainer = null!;
private RemoveDropZone _removeDropZone = null!;
private Label _statusLabel = null!;
private Button _startGameplayButton = null!;

public override void _Ready()
{
BindUiNodes();
BindGridSlots();
InitNucleusSlots();
_removeDropZone.ComponentRemoved += OnComponentRemovedFromGrid;
_startGameplayButton.Pressed += OnStartGameplayPressed;
RefreshGridState();
}

public override void _ExitTree()
{
SaveConfiguredOrganismToJson();
}

private void BindUiNodes()
{
_gridContainer = GetNode<GridContainer>("%BuilderGrid");
_removeDropZone = GetNode<RemoveDropZone>("%RemoveDropZone");
_statusLabel = GetNode<Label>("%StatusLabel");
_startGameplayButton = GetNode<Button>("%StartGameplayButton");
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

/// <summary>
/// Locks the four center nucleus slots (indices 5, 6, 9, 10 on the 4x4 grid)
/// and pre-fills them in the component array.
/// </summary>
private void InitNucleusSlots()
{
foreach (var idx in CellBlueprint.NucleusIndices)
{
_gridComponents[idx] = OrganelleType.Nucleus.SerializedName();
if (idx < _gridSlots.Count)
{
_gridSlots[idx].SetNucleus();
}
}
}

private static bool IsNucleusIndex(int nodeIndex)
{
foreach (var idx in CellBlueprint.NucleusIndices)
{
if (idx == nodeIndex)
{
return true;
}
}

return false;
}

private void RefreshGridState()
{
var placedCount = 0;
for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
{
var component = _gridComponents[nodeIndex];
if (!IsNucleusIndex(nodeIndex) && nodeIndex < _gridSlots.Count)
{
_gridSlots[nodeIndex].SetComponent(component);
}

if (!string.IsNullOrEmpty(component))
{
placedCount += 1;
}
}

var nonNucleusPlaced = placedCount - CellBlueprint.NucleusIndices.Length;

// Compute duplication threshold accounting for Ribosome organelles
var ribosomeCount = 0;
for (var i = 0; i < GridNodeCount; i++)
{
if (_gridComponents[i] == OrganelleType.Ribosome.SerializedName())
{
ribosomeCount++;
}
}

var dupThreshold = Math.Max(1, placedCount - ribosomeCount);
_statusLabel.Text =
$"Organelles placed: {nonNucleusPlaced}/{GridNodeCount - CellBlueprint.NucleusIndices.Length} " +
$"| Cell elements: {placedCount}/16 | Duplicates at {dupThreshold} food";
}

private int FindComponentNode(string componentName)
{
for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
{
if (IsNucleusIndex(nodeIndex))
{
continue;
}

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

if (IsNucleusIndex(targetNodeIndex))
{
_statusLabel.Text = "Cannot place organelles on nucleus slots.";
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

if (IsNucleusIndex(sourceNodeIndex))
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

if (IsNucleusIndex(sourceNodeIndex))
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

private void OnStartGameplayPressed()
{
var error = GetTree().ChangeSceneToFile(ScenePaths.Gameplay);
if (error != Error.Ok)
{
GD.PushError($"Failed to load gameplay scene: {error}");
}
}

private void SaveConfiguredOrganismToJson()
{
var serializedComponents = new Godot.Collections.Array<string>();
for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
{
serializedComponents.Add(_gridComponents[nodeIndex] ?? string.Empty);
}

var payload = new Godot.Collections.Dictionary
{
{ "grid_size", GridSize },
{ "components", serializedComponents }
};

var file = FileAccess.Open(OrganismConfigPath, FileAccess.ModeFlags.Write);
if (file is null)
{
GD.PushError($"Failed to open organism config file for writing: {OrganismConfigPath}. Error: {FileAccess.GetOpenError()}");
return;
}

using var openedFile = file;
openedFile.StoreString(Json.Stringify(payload, "\t"));
}
}
