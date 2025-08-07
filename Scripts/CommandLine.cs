using Godot;
using System;
using System.Text.RegularExpressions;

public partial class CommandLine : CanvasLayer
{
    private SceneChanger sceneChanger; // Reference to the SceneChanger node
    private LineEdit cmdLine; // Input field for the command line

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		Visible = false; // Hide the command line by default
        sceneChanger = GetNode<SceneChanger>("/root/SceneChanger"); // Get the SceneChanger node from the root
        cmdLine = GetNode<LineEdit>("CommandLineInput"); // Get the LineEdit node for command input
    }

    // Called every time an input event is received.
    public override void _Input(InputEvent @event)
	{
        if (@event.IsActionPressed("OpenCommandLine"))
        {
            CallDeferred(nameof(OpenCommandButtonPressed));// The method call is deffered so that the button pressed to open the command line is not typed into the command line input
        }
    }

    // Called when the button to open/close the command line is pressed.
    private void OpenCommandButtonPressed()
	{
        Visible = !Visible; // Toggle visibility of the command line
        if (Visible)
        {
            // When opening the command line, grab focus and clear the input field
            cmdLine.GrabFocus();
            cmdLine.Clear();
        }
    }

    private void CommandEntered(string command)
    {
        string[] words = OrganiseCommand(command);

        bool correctCommand = true;
        switch (words[0])
        {
            case "load":
                if (words.Length == 2)
                {
                    correctCommand = sceneChanger.ChangeScene(words[1]);
                }
                else correctCommand = false;
                break;
            case "loadpath":
                if (words.Length == 2)
                {
                    sceneChanger.LoadLevelFromFile(words[1]);
                }
                else correctCommand = false;
                break;
            case "generate":
                if (words.Length == 2)
                {
                    sceneChanger.GenerateLevelAndLoad(words[1]);
                }
                else correctCommand = false;
                break;
            default:
                correctCommand = false;
                break;
        }
        if (correctCommand)
        {
            cmdLine.Clear(); // Clear the command line input after processing the command
        }
    }

    private static string[] OrganiseCommand(string command)//organises the given command into words that are either space delimited or text in speech marks
    {
        string pattern = @"[^\s""]+|""([^""]*)""";  // Match words OR text in speech marks

        var matches = Regex.Matches(command, pattern);
        string[] words = new string[matches.Count];

        for (int i = 0; i < matches.Count; i++)
        {
            words[i] = matches[i].Groups[1].Success ? matches[i].Groups[1].Value : matches[i].Value;
        }

        return words;
    }
}
