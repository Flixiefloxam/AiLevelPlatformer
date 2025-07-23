using Godot;
using System;
using System.Collections.Generic;
using System.IO;

public partial class SceneChanger : CanvasLayer
{
	private string newScenePath;
	private AnimationPlayer animationPlayer;
	private Stack<string> sceneHistory = new Stack<string>(); // Stack to keep track of scene history. Does not include current scene.
    private string levelLoadScenePath = "res://Levels/LevelLoader.tscn"; // The path to the LevelLoader scene

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public bool LoadLevelFromFile(string path)
    {
        if (Godot.FileAccess.FileExists(path)&& path.EndsWith(".txt"))
        {
            ChangeScene(levelLoadScenePath);
            var levelLoader = GetNode<LevelLoader>("/root/LevelLoader");
            levelLoader.LoadLevelFromFile(path);
            return true; // Return true to indicate the level was loaded successfully
        }
        else return false; // Return false if the file does not exist
    }

    // Wrapper function to change the scene with history tracking enabled.
    public bool ChangeScene(string scenePath)
	{
        return ChangeScene(scenePath, true);
    }

    // Called to change the scene
    private bool ChangeScene(string scenePath, bool addToHistory = false)
    {
        if (!ResourceLoader.Exists(scenePath) && scenePath.EndsWith(".tscn"))
        {
            GD.PrintErr("Scene does not exist: " + scenePath);
            return false;
        }

        if (addToHistory)
        {
            sceneHistory.Push(GetTree().CurrentScene.SceneFilePath); // Push the current scene onto the stack
        }
        newScenePath = scenePath;
        GD.Print("Changing scene to: " + Path.GetFileNameWithoutExtension(newScenePath));

        if (animationPlayer.IsPlaying())
        {
            return false; // If an animation is already playing, do not change scene
        }
        animationPlayer.Play("FadeInOut");
        return true; // Return true to indicate the scene change was initiated
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
