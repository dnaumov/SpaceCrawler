using Godot;

public partial class MenuScene : Control
{
	private Button _openBuilderButton = null!;

	public override void _Ready()
	{
		_openBuilderButton = GetNode<Button>("%OpenBuilderButton");
		_openBuilderButton.Pressed += OnOpenBuilderPressed;
	}

	private void OnOpenBuilderPressed()
	{
		GetTree().ChangeSceneToFile("res://OrganismBuilder.tscn");
	}
}
