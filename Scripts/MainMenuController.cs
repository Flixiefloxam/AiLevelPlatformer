using Godot;
using System;

public partial class MainMenuController : Control
{
    private Node SceneChanger; // Link to the global SceneChanger node
    public override void _Ready()
    {
        SceneChanger = GetNode("/root/SceneChanger");
    }
    public void StartButtonPressed()
    {
        GD.Print("Start button pressed");
        SceneChanger.Call("ChangeScene", "res://Levels/Menus/LevelSelect.tscn");
    }
}
