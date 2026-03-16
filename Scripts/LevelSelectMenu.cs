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
    private Label LoadingLabel; // The "Loading Level..." label that shows when a level is being loaded or generated
    private AnimationPlayer animationPlayer; // The animation player for the level select menu
    private SceneChanger SceneChanger; // The scene changer node to change scenes when a level is selected
    private bool isLoadingLevel; // Flag to prevent multiple level loads at the same time
    private List<string> levelFiles; // List to store the level files in the training levels directory
    private List<string> aiFiles; // List to store the AI generator files in the AI generators directory
    private CommandLine commandLine; // Reference to the CommandLine node for logging purposes

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        startMenu = GetNode<Control>("StartMenu");
        levelLoadMenu = GetNode<Control>("LevelLoadMenu");
        levelGenerateMenu = GetNode<Control>("LevelGenerateMenu");
        levelList = levelLoadMenu.GetNode<ItemList>("LevelList");
        aiList = levelGenerateMenu.GetNode<ItemList>("AIList");
        LoadingLabel = GetNode<Label>("LoadingLabel");
        animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        SceneChanger = GetNode<SceneChanger>("/root/SceneChanger");
        levelFiles = new List<string>(); // Initialize the list to store level files
        aiFiles = new List<string>(); // Initialize the list to store AI generator files
        commandLine = GetNode<CommandLine>("/root/CommandLine"); // Get the CommandLine node for logging

        // Initialize menu visibility and positions
        startMenu.Visible = true;
        startMenu.Position = new Vector2(0, 0);
        levelLoadMenu.Visible = false;
        levelGenerateMenu.Visible = false;
        LoadingLabel.Visible = false;

        isLoadingLevel = false; // Initialize the flag to false
    }

    private void LoadLevelButtonPressed()
    {
        QueueAnimation("CloseStartMenu");
        PopulateList(levelList, TrainingLevelsDir, levelFiles);
        QueueAnimation("OpenLevelLoadMenu");
    }

    private void GenerateLevelButtonPressed()
    {
        QueueAnimation("CloseStartMenu");
        PopulateList(aiList, AiGeneratorsDir, aiFiles);
        QueueAnimation("OpenLevelGenerateMenu");
    }

    //This runs when a level is selected from the list of levels
    private void LevelSelected(int index, Vector2 atPosition, int mouseButtonIndex)
    {
        if (mouseButtonIndex == 1)
        {
            if (isLoadingLevel == false)
            {
                isLoadingLevel = true; // Set the flag to prevent multiple loads
                SceneChanger.LoadLevelFromFile(TrainingLevelsDir + levelFiles[index]);
            }
        }
    }

    //This runs when an ai is selected from the list of ais
    private void AiSelected(int index, Vector2 atPosition, int mouseButtonIndex)
    {
        if (mouseButtonIndex == 1)
        {
            if (isLoadingLevel == false)
            {
                isLoadingLevel = true; // Set the flag to prevent multiple loads

                QueueAnimation("CloseLevelGenerateMenu");
                QueueAnimation("ShowLoadingScreen");

                string dir = AiGeneratorsDir.Replace("res://", ""); // remove the the godot resource path prefix
                SceneChanger.GenerateLevelAndLoad(dir + aiFiles[index]);
            }
        }
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

    private void PopulateList(ItemList list, string dir, List<string> nameList)
    {
        list.Clear(); // Clear the list before populating it
        nameList.Clear(); // Clear the provided nameList to ensure it starts fresh
        List<string> files = GetFilesInDir(dir);

        foreach (string fileName in files)
        {
            nameList.Add(fileName); // Add the raw file name to the provided nameList
            list.AddItem(CleanFileName(fileName)); // Add the cleaned file name to the displayed list on the UI
        }
    }

    private List<string> GetFilesInDir(string dirPath)
    {
        DirAccess dir = DirAccess.Open(dirPath);

        // Check if the directory exists and can be opened
        if (dir == null)
        {
            commandLine.LogError($"Level directory not found or cannot be opened at: {dirPath}");
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
