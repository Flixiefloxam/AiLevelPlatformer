using Godot;
using System;

public partial class MainMenu : Control
{
    private SceneChanger SceneChanger; // Link to the global SceneChanger node
    private CommandLine commandLine; // Reference to the CommandLine node for logging purposes

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");
        commandLine = GetNode<CommandLine>("/root/CommandLine"); // Get the CommandLine node for logging
    }
    public void StartButtonPressed()
    {
        commandLine.Log("Start button pressed");
        SceneChanger.ChangeScene("res://Levels/Menus/LevelSelect.tscn");
    }

    public void QuitButtonPressed()
    {
        commandLine.Log("Quit button pressed");
        GetTree().Quit();
    }
}
