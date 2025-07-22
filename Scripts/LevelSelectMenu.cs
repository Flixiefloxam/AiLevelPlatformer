using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.Linq;

public partial class LevelSelectMenu : Control
{
    private const string TrainingLevelsDir = "res://Levels/TrainingLevels/"; // The directory where the training levels are stored
    private const string AiGeneratorsDir = "res://Scripts/LevelGenerators/"; // The directory where the AI generators are stored

    private Control startMenu; // The menu that shows when you first navigate to the level select menu that lets you select whether you want to load a level or generate one with ai
    private Control levelLoadMenu; //The menu that allows you to select which training/test/saved level you want to load
    private Control levelGenerateMenu; // The menu that allows you to select which ai you want to use to generate a level
    private ItemList levelList; // The list of levels that you can select from to load
    private ItemList aiList; // The list of ai's that you can select from to generate a level
    private AnimationPlayer animationPlayer; // The animation player for the level select menu
    private SceneChanger SceneChanger; // The scene changer node to change scenes when a level is selected

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        startMenu = GetNode<Control>("StartMenu");
        levelLoadMenu = GetNode<Control>("LevelLoadMenu");
        levelGenerateMenu = GetNode<Control>("LevelGenerateMenu");
        levelList = levelLoadMenu.GetNode<ItemList>("LevelList");
        aiList = levelGenerateMenu.GetNode<ItemList>("AIList");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");

        startMenu.Visible = true;
        startMenu.Position = new Vector2(0, 0);
        levelLoadMenu.Visible = false;
        levelGenerateMenu.Visible = false;
    }

    private void LoadLevelButtonPressed()
    {
        QueueAnimation("CloseStartMenu");
        levelList.Clear();
        PopulateList(levelList, TrainingLevelsDir);
        QueueAnimation("OpenLevelLoadMenu");
    }
    private void GenerateLevelButtonPressed()
    {
        QueueAnimation("CloseStartMenu");
        aiList.Clear();
        PopulateList(aiList, AiGeneratorsDir);
        QueueAnimation("OpenLevelGenerateMenu");
    }

    private void BackButtonPressed()
    {
        if (levelLoadMenu.Visible)
        {
            QueueAnimation("CloseLevelLoadMenu");
            QueueAnimation("OpenStartMenu");
        }
        else if (levelGenerateMenu.Visible)
        {
            QueueAnimation("CloseLevelGenerateMenu");
            QueueAnimation("OpenStartMenu");
        }
        else if (startMenu.Visible)
        {
            SceneChanger.Back(); // Go back to the previous scene
        }
    }

    private string CleanFileName(string fileName)
    {
        fileName = Path.GetFileNameWithoutExtension(fileName); // Remove the file extension
        return Regex.Replace(fileName, @"(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " "); // Add a space before each uppercase letter that is not at the start of the string or part of an acronym
    }

    private void PopulateList(ItemList list, string dir)
    {
        List<string> files = GetFilesInDir(dir);

        foreach (string fileName in files)
        {
            list.AddItem(CleanFileName(fileName));
        }
    }

    private static List<string> GetFilesInDir(string dirPath)
    {
        DirAccess dir = DirAccess.Open(dirPath);

        // Check if the directory exists and can be opened
        if (dir == null)
        {
            GD.PrintErr($"Level directory not found or cannot be opened at: {dirPath}");
            return new List<string>();
        }

        dir.ListDirBegin(); // Start listing files in the directory
        List<string> files = new List<string>();

        string fileName;
        do
        {
            fileName = dir.GetNext(); // Get the next file in the directory
            if (fileName != "" && !fileName.StartsWith(".") && !fileName.EndsWith(".import")) // Ignore hidden files and import files
            {
                files.Add(fileName);
            }
        }
        while (fileName != "");

        return files;
    }

    private async void QueueAnimation(string animName)
    {
        if (animationPlayer.IsPlaying())
        {
            if (animationPlayer.CurrentAnimation == animName)
                return;

            await ToSignal(animationPlayer, AnimationPlayer.SignalName.AnimationFinished);
        }
        animationPlayer.Play(animName);
    }
}
