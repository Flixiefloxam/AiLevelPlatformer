using Godot;
using System;

public partial class VictoryScreen : CanvasLayer
{
    private SceneChanger SceneChanger; // Link to the global SceneChanger node
    private AnimationPlayer animationPlayer;
    private CommandLine commandLine; // Reference to the CommandLine node for logging purposes

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Visible = false;
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        commandLine = GetNode<CommandLine>("/root/CommandLine"); // Get the CommandLine node for logging
    }

    public void ShowVictoryScreen()
    {
        animationPlayer.Play("Show");
    }

    public void ExitButtonPressed()
    {
        commandLine.Log("Exit button pressed");
        SceneChanger.ChangeScene("res://Levels/Menus/MainMenu.tscn");
    }
}
