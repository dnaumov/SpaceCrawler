using Godot;

public static class DragPayload
{
public const string ComponentKey = "component";
public const string SourceKey = "source";
public const string SourceNodeKey = "source_node";

public static bool TryRead(Variant data, out string componentName, out string sourceList, out int sourceNodeIndex)
{
componentName = string.Empty;
sourceList = string.Empty;
sourceNodeIndex = -1;
if (data.VariantType != Variant.Type.Dictionary)
{
return false;
}

var dictionary = data.AsGodotDictionary();
if (!dictionary.ContainsKey(ComponentKey) || !dictionary.ContainsKey(SourceKey))
{
return false;
}

componentName = dictionary[ComponentKey].AsString();
sourceList = dictionary[SourceKey].AsString();
if (dictionary.ContainsKey(SourceNodeKey))
{
sourceNodeIndex = dictionary[SourceNodeKey].AsInt32();
}

return !string.IsNullOrEmpty(componentName) && !string.IsNullOrEmpty(sourceList);
}
}
