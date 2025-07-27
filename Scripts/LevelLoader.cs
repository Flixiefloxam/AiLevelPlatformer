using Godot;
using System;
using System.Collections.Generic;

public partial class LevelLoader : Node2D
{
	private bool levelLoaded = false; // Flag to check if the level was loaded correctly
    private TileMapLayer tileMapLayer;
    private const char spawnPointChar = 'a'; // Character representing the spawn point
    private CharacterBody2D player;
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
        player = GetNode<CharacterBody2D>("Player");
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
                else if (c == spawnPointChar)
                {
                    // Set the player's position to the spawn point
                    player.GlobalPosition = new Vector2(x * tileMapLayer.TileSet.TileSize.X, y * tileMapLayer.TileSet.TileSize.Y);
                    GD.Print("Player spawn point set at: " + player.GlobalPosition);
                }
            }
        }

        SetCameraLimits(); // Set camera limits based on the tile map
    }

    // Sets the camera limits based on the tile map's used rect. This ensures the camera does not go out of bounds of the tile map.
    private void SetCameraLimits()
    {
        Camera2D camera = player.GetNode<Camera2D>("Camera2D");
        Rect2I usedRect = tileMapLayer.GetUsedRect();
        Vector2I tileSize = tileMapLayer.TileSet.TileSize;

        Rect2 pixelBounds = new Rect2(
        tileMapLayer.MapToLocal(usedRect.Position),
        tileMapLayer.MapToLocal(usedRect.Size) - tileMapLayer.MapToLocal(Vector2I.Zero)
        );

        camera.LimitLeft = (int)pixelBounds.Position.X;
        //camera.LimitTop = (int)pixelBounds.Position.Y;
        camera.LimitRight = (int)(pixelBounds.Position.X + pixelBounds.Size.X - tileSize.X);
        camera.LimitBottom = (int)(pixelBounds.Position.Y + pixelBounds.Size.Y - tileSize.Y);
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
