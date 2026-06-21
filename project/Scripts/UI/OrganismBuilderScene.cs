using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class OrganismBuilderScene : Control
{
private const int GridSize = 4;
private const int GridNodeCount = GridSize * GridSize;
private const string OrganismConfigPath = "user://organism_config.json";

private readonly string[] _gridComponents = new string[GridNodeCount];
private readonly List<GridNodeSlot> _gridSlots = [];
private readonly List<SensorConnection> _connections = [];
private readonly List<int> _sensorOptionSlots = [];
private readonly List<int> _engineOptionSlots = [];

private GridContainer _gridContainer = null!;
private RemoveDropZone _removeDropZone = null!;
private Label _statusLabel = null!;
private Button _startGameplayButton = null!;
private OptionButton _sensorOption = null!;
private OptionButton _engineOption = null!;
private CheckButton _invertedCheck = null!;
private Button _connectButton = null!;
private ItemList _connectionList = null!;
private Button _removeConnectionButton = null!;

public override void _Ready()
{
BindUiNodes();
BindGridSlots();
InitNucleusSlots();
LoadConfiguredOrganismFromJson();
_removeDropZone.ComponentRemoved += OnComponentRemovedFromGrid;
_startGameplayButton.Pressed += OnStartGameplayPressed;
_connectButton.Pressed += OnConnectPressed;
_removeConnectionButton.Pressed += OnRemoveConnectionPressed;
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
_sensorOption = GetNode<OptionButton>("%SensorOption");
_engineOption = GetNode<OptionButton>("%EngineOption");
_invertedCheck = GetNode<CheckButton>("%InvertedCheck");
_connectButton = GetNode<Button>("%ConnectButton");
_connectionList = GetNode<ItemList>("%ConnectionList");
_removeConnectionButton = GetNode<Button>("%RemoveConnectionButton");
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
PruneInvalidConnections();
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

var dupThreshold = Math.Max(1, placedCount - ribosomeCount * 2);
_statusLabel.Text =
$"Organelles placed: {nonNucleusPlaced}/{GridNodeCount - CellBlueprint.NucleusIndices.Length} " +
$"| Cell elements: {placedCount}/16 | Duplicates at {dupThreshold} food";
RefreshConnectionUi();
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
RemoveConnectionsAtSlot(targetNodeIndex);
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
RemapConnectionsForSwap(sourceNodeIndex, targetNodeIndex);
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
RemoveConnectionsAtSlot(sourceNodeIndex);
RefreshGridState();
}

private void OnConnectPressed()
{
if (_sensorOption.Selected < 0 || _engineOption.Selected < 0)
{
return;
}

var sensorSlot = _sensorOptionSlots[_sensorOption.Selected];
var engineSlot = _engineOptionSlots[_engineOption.Selected];
_connections.RemoveAll(connection => connection.EngineSlot == engineSlot);
_connections.Add(new SensorConnection(sensorSlot, engineSlot, _invertedCheck.ButtonPressed));
RefreshGridState();
}

private void OnRemoveConnectionPressed()
{
var selected = _connectionList.GetSelectedItems();
if (selected.Length == 0)
{
return;
}

var index = selected[0];
if (index >= 0 && index < _connections.Count)
{
_connections.RemoveAt(index);
RefreshGridState();
}
}

private void RefreshConnectionUi()
{
_sensorOption.Clear();
_engineOption.Clear();
_sensorOptionSlots.Clear();
_engineOptionSlots.Clear();

for (var slot = 0; slot < GridNodeCount; slot++)
{
var type = OrganelleTypeExtensions.FromSerializedName(_gridComponents[slot] ?? string.Empty);
if (type.IsSensor())
{
_sensorOptionSlots.Add(slot);
_sensorOption.AddItem($"{slot + 1}: {type.DisplayName()}");
}
if (type.AcceptsSensorInput())
{
_engineOptionSlots.Add(slot);
_engineOption.AddItem($"{slot + 1}: {type.DisplayName()}");
}
}

_connectionList.Clear();
foreach (var connection in _connections)
{
var sensor = OrganelleTypeExtensions.FromSerializedName(_gridComponents[connection.SensorSlot]);
var engine = OrganelleTypeExtensions.FromSerializedName(_gridComponents[connection.EngineSlot]);
var inversion = connection.Inverted ? " [inverted]" : string.Empty;
_connectionList.AddItem($"{connection.SensorSlot + 1}: {sensor.DisplayName()} -> " +
$"{connection.EngineSlot + 1}: {engine.DisplayName()}{inversion}");
}

_connectButton.Disabled = _sensorOptionSlots.Count == 0 || _engineOptionSlots.Count == 0;
_removeConnectionButton.Disabled = _connections.Count == 0;
}

private void PruneInvalidConnections()
{
var grid = CurrentGrid();
var occupiedEngines = new HashSet<int>();
_connections.RemoveAll(connection =>
{
if (!CellBlueprint.TryValidateConnection(grid, connection, occupiedEngines, out _))
{
return true;
}
occupiedEngines.Add(connection.EngineSlot);
return false;
});
}

private void RemoveConnectionsAtSlot(int slot) =>
_connections.RemoveAll(connection => connection.SensorSlot == slot || connection.EngineSlot == slot);

private void RemapConnectionsForSwap(int first, int second)
{
for (var index = 0; index < _connections.Count; index++)
{
var connection = _connections[index];
_connections[index] = connection with
{
SensorSlot = SwapSlot(connection.SensorSlot, first, second),
EngineSlot = SwapSlot(connection.EngineSlot, first, second)
};
}
}

private static int SwapSlot(int value, int first, int second) =>
value == first ? second : value == second ? first : value;

private OrganelleType[] CurrentGrid()
{
var grid = new OrganelleType[GridNodeCount];
for (var index = 0; index < GridNodeCount; index++)
{
grid[index] = OrganelleTypeExtensions.FromSerializedName(_gridComponents[index] ?? string.Empty);
}
return grid;
}

private void OnStartGameplayPressed()
{
var error = GetTree().ChangeSceneToFile(ScenePaths.Gameplay);
if (error != Error.Ok)
{
GD.PushError($"Failed to load gameplay scene: {error}");
}
}

private void LoadConfiguredOrganismFromJson()
{
if (!FileAccess.FileExists(OrganismConfigPath))
{
return;
}

using var file = FileAccess.Open(OrganismConfigPath, FileAccess.ModeFlags.Read);
if (file is null)
{
GD.PushWarning($"Failed to open saved organism config: {OrganismConfigPath}.");
return;
}

CellBlueprint blueprint;
try
{
blueprint = CellBlueprintJson.Deserialize(file.GetAsText(), message => GD.PushWarning(message));
}
catch (Exception exception)
{
GD.PushWarning($"Saved organism config is invalid: {exception.Message}");
return;
}

for (var nodeIndex = 0; nodeIndex < GridNodeCount; nodeIndex++)
{
if (IsNucleusIndex(nodeIndex))
{
continue;
}

var organelle = blueprint.Grid[nodeIndex];
_gridComponents[nodeIndex] = organelle == OrganelleType.Empty
? string.Empty
: organelle.SerializedName();
}
_connections.Clear();
_connections.AddRange(blueprint.Connections);
}

private void SaveConfiguredOrganismToJson()
{
PruneInvalidConnections();
var blueprint = new CellBlueprint(CurrentGrid(), _connections);

var file = FileAccess.Open(OrganismConfigPath, FileAccess.ModeFlags.Write);
if (file is null)
{
GD.PushError($"Failed to open organism config file for writing: {OrganismConfigPath}. Error: {FileAccess.GetOpenError()}");
return;
}

using var openedFile = file;
openedFile.StoreString(CellBlueprintJson.Serialize(blueprint));
}
}
