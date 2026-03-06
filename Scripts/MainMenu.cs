using Godot;
using System;

public partial class MainMenu : Control
{
    private SceneChanger SceneChanger; // Link to the global SceneChanger node

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");
    }
    public void StartButtonPressed()
    {
        GD.Print("Start button pressed");
        SceneChanger.ChangeScene("res://Levels/Menus/LevelSelect.tscn");
    }

    public void QuitButtonPressed()
    {
        GD.Print("Quit button pressed");
        GetTree().Quit();
    }
}
