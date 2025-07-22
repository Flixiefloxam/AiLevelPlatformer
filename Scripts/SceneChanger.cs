using Godot;
using System;
using System.Collections.Generic;

public partial class SceneChanger : CanvasLayer
{
	private string newScenePath;
	private AnimationPlayer animationPlayer;
	private Stack<string> sceneHistory = new Stack<string>(); // Stack to keep track of scene history. Does not include current scene.

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    // Wrapper function to change the scene with history tracking enabled.
    public void ChangeScene(string scenePath)
	{
        ChangeScene(scenePath, true);
    }

    // Called to change the scene
    private void ChangeScene(string scenePath, bool addToHistory = false)
    {
        if (addToHistory)
        {
            sceneHistory.Push(GetTree().CurrentScene.SceneFilePath); // Push the current scene onto the stack
        }
        newScenePath = scenePath;
        GD.Print("Changing scene to: " + newScenePath);

        if (animationPlayer.IsPlaying())
        {
            return; // If an animation is already playing, do not change scene
        }
        animationPlayer.Play("FadeInOut");
    }

    // Called to go back to the previous scene in the stack.
    public void Back()
    {
        if (sceneHistory.Count > 0)
        {
            ChangeScene(sceneHistory.Pop(), false);
        }
        else
        {
            GD.Print("No previous scene in history. Returning to main menu.");
            ChangeScene("res://Levels/Menus/MainMenu.tscn", false); // Default back to main menu if no history
        }
    }

    // Called when the fade animation is finished to change scene.
    private void NewScene()
	{
		CallDeferred(nameof(ChangeSceneToFile), newScenePath);
    }

    // Called only by new scene so that the scene change can be deffered
	private void ChangeSceneToFile(string path)
	{
		GetTree().ChangeSceneToFile(path);
    }
}
