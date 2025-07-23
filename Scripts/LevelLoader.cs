using Godot;
using System;
using System.Collections.Generic;

public partial class LevelLoader : Node2D
{
	private bool levelLoaded = false; // Flag to check if the level was loaded correctly
    private TileMapLayer tileMapLayer;
	private Dictionary<char, int> tileMapping = new()
	{
		{ 'X', 0 }, // Solid tile
		{ '[', 0 },
        { ']', 0 },
		{ '<', 0 },
		{ '>', 0 },
        { 'S', 0 },
        { 'Q', 0 }
    };

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		tileMapLayer = GetNode<TileMapLayer>("TileMapLayer");
    }

	public void LoadLevelFromFile(string path)
	{
		using var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr("Failed to open level file: " + path);
			return;
        }
        levelLoaded = true; // Set the flag to true when the level is loaded
        GD.Print("Loading level from file: " + path);

        var lines = new List<string>();
		while (!file.EofReached())
			lines.Add(file.GetLine());

        tileMapLayer.Clear(); // Clear existing tiles
        for (int y = 0; y < lines.Count; y++)
        {
            string line = lines[y];
            for (int x = 0; x < line.Length; x++)
            {
				char c = line[x];
                if (tileMapping.TryGetValue(c, out int tileId))
                {
					tileMapLayer.SetCell(new Vector2I(x, y), tileId, new Vector2I(0,0), 0);
				}
            }
        }
    }

    // Called deffered after start to check if the scene was loaded correctly through the SceneChanger's LoadLevelFromFile method.
    private void IsLevelLoadedCorrectly()
	{
        if (!levelLoaded)
        {
            GD.PrintErr("LevelLoader scene not loaded correctly through SceneChanger or failed to find level.");
        }
    }
}
