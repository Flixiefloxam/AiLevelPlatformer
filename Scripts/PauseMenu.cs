using Godot;
using System;

public partial class PauseMenu : CanvasLayer
{
	private SceneChanger SceneChanger; // Link to the global SceneChanger node
	private CommandLine commandLine; // Reference to the CommandLine node for logging purposes

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Visible = false;
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");
		commandLine = GetNode<CommandLine>("/root/CommandLine");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        if (Input.IsActionJustPressed("PauseGame"))
        {
            Visible = !Visible;
        }

    }

	public void ResumeButtonPressed()
	{
		Visible = false;
    }

	public void ExitButtonPressed()
	{
		commandLine.Log("Exit button pressed");
		SceneChanger.ChangeScene("res://Levels/Menus/MainMenu.tscn");
    }
}
