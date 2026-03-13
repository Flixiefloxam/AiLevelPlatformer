using Godot;
using System;

public partial class VictoryScreen : CanvasLayer
{
    private SceneChanger SceneChanger; // Link to the global SceneChanger node
    private AnimationPlayer animationPlayer;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Visible = false;
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public void ShowVictoryScreen()
    {
        animationPlayer.Play("Show");
    }

    public void ExitButtonPressed()
    {
        GD.Print("Exit button pressed");
        SceneChanger.ChangeScene("res://Levels/Menus/MainMenu.tscn");
    }
}
