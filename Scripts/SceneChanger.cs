using Godot;
using System;

public partial class SceneChanger : CanvasLayer
{
	private string newScenePath;
	private AnimationPlayer animationPlayer;

    public override void _Ready()
	{
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }
    public void ChangeScene(string scenePath)
	{
		newScenePath = scenePath;
		GD.Print("Changing scene to: " + newScenePath);

        if (animationPlayer.IsPlaying())
        {
            animationPlayer.Stop();
        }
        animationPlayer.Play("FadeInOut");
    }
	private void NewScene()
	{
		CallDeferred(nameof(ChangeSceneToFile), newScenePath);
    }
	private void ChangeSceneToFile(string path)
	{
		GetTree().ChangeSceneToFile(path);
    }
}
