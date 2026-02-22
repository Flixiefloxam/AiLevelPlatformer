using Godot;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public partial class SceneChanger : CanvasLayer
{
    private const string levelLoadScenePath = "res://Levels/LevelLoader.tscn"; // The path to the LevelLoader scene
    private const string GeneratedLevelPath = "res://Levels/GeneratedLevels/GeneratedLevel.txt"; // The path to the generated level file
    private const int MaxLevelGenerationAttempts = 10; // Maximum number of attempts to generate a valid level

    private string newScenePath;
	private AnimationPlayer animationPlayer;
	private Stack<string> sceneHistory = new Stack<string>(); // Stack to keep track of scene history. Does not include current scene.
    private TaskCompletionSource<bool> sceneChanged;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public async void LoadLevelFromFile(string path)
    {
        if (Godot.FileAccess.FileExists(path)&& path.EndsWith(".txt"))
        {
            ChangeScene(levelLoadScenePath);
            await sceneChanged.Task; // Wait for the level to be loaded
            var levelLoader = GetNode<LevelLoader>("/root/LevelLoader");
            levelLoader.LoadLevelFromFile(path);
        }
        else
        {
            GD.PrintErr("Level file does not exist or is not a valid .txt file: " + path);
        }
    }

    public async void GenerateLevelAndLoad(string path)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{path}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        string stdout;
        string stderr;
        bool validLevel = false;
        int failedAttempts = 0;

        do
        {
            process.Start();

            // Read the output and error streams asynchronously for debugging
            stdout = await process.StandardOutput.ReadToEndAsync();
            stderr = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                GD.PrintErr($"Error generating level with {path}: {stderr}");
                return;
            }

            if (ValidLevelStructure(GeneratedLevelPath))
            {
                validLevel = true;
            }
            failedAttempts++;
        }
        while (!validLevel && failedAttempts < MaxLevelGenerationAttempts);

        

        GD.Print($"Level generated successfully with {path}. Output: {stdout}");

        LoadLevelFromFile(GeneratedLevelPath);
    }

    private static bool ValidLevelStructure(string path)
    {
        using var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        if (file == null)
        {
            GD.PrintErr("Failed to open level file: " + path);
            return false;
        }

        var lines = new List<string>();
        while (!file.EofReached())
            lines.Add(file.GetLine());

        // Structure Validation
        if (!lines.Contains("a"))
        {
            GD.PrintErr("No spawn point found");
        }
        else
        {
            for (int y = 0; y < lines.Count; y++)
            {
                for (int x = 0; x < lines[0].Length; x++)
                {
                    if (lines[y][x] == 'a')
                    {
                        for (int i = 0; i < lines.Count - y; i++)
                        {
                            if (lines[i].Length >= x)
                            {
                                if (lines[i][x] != '-')
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }
            GD.PrintErr("No valid spawn point found (must have a solid tile below it)");
        }
            

        return false;
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

        sceneChanged = new TaskCompletionSource<bool>();

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
	private async void ChangeSceneToFile(string path)
	{
		GetTree().ChangeSceneToFile(path);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame); // Wait for the next frame so the scene is fully loaded
        sceneChanged?.SetResult(true); // Set the result of the TaskCompletionSource to true
    }
}
