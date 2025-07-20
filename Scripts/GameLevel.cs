using Godot;
using System;
using System.Collections.Generic;

public partial class GameLevel : Node2D
{
	[Export] private string levelPath = "res://Levels/TrainingLevels/mario-1-1.txt";

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

	public override void _Ready()
	{
		tileMapLayer = GetNode<TileMapLayer>("TileMapLayer");
        LoadLevelFromFile(levelPath);
    }

	private void LoadLevelFromFile(string path)
	{
		var file = FileAccess.Open(path, FileAccess.ModeFlags.Read);
		if (file == null)
		{
			GD.PrintErr("Failed to open level file: " + path);
			return;
        }
		GD.Print("Loading level from file: " + path);

        var lines = new List<string>();
		while (!file.EofReached())
			lines.Add(file.GetLine());

		file.Close();

        GD.Print("Total lines loaded: " + lines.Count);
        foreach (var line in lines)
            GD.Print(line);

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
}
