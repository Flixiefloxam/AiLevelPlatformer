using Godot;
using System;
using System.Collections.Generic;

public partial class LevelLoader : Node2D
{
    public Vector2 playerSpawnPoint;// Where the player should spawn in the level

    private bool levelLoaded = false; // Flag to check if the level was loaded correctly
    private TileMapLayer tileMapLayer;
    private Vector2I tileSize;
    private const char spawnPointChar = 'a'; // Character representing the spawn point
    private CharacterBody2D player;
    private PlayerController playerController;
    private Rect2 tileMapBounds;
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
        playerController = player as PlayerController;
        playerController.LevelLoader = this; // Set the LevelLoader reference in PlayerController
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        // Check if the player is out of bounds and respawn if necessary
        if (IsPlayerOutOfBounds())
        {
            GD.Print($"Player out of bounds: {player.GlobalPosition}. Respawning...");
            playerController.Respawn();
        }
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
                    playerSpawnPoint = new Vector2(x * tileMapLayer.TileSet.TileSize.X, y * tileMapLayer.TileSet.TileSize.Y);
                    GD.Print("Player spawn point set at: " + playerSpawnPoint);
                    playerController.Respawn(true);
                }
            }
        }

        tileSize = tileMapLayer.TileSet.TileSize; // Get the tile size from the TileSet
        tileMapBounds = GetTileMapBounds(); // Set the bounds of the tile map
        SetCameraLimits(); // Set camera limits based on the tile map
    }

    private Rect2 GetTileMapBounds()
    {
        Rect2I usedRect = tileMapLayer.GetUsedRect();
        Rect2 pixelBounds = new Rect2(
        tileMapLayer.MapToLocal(usedRect.Position),
        tileMapLayer.MapToLocal(usedRect.Size) - tileMapLayer.MapToLocal(Vector2I.Zero)
        );
        return pixelBounds;
    }

    private bool IsPlayerOutOfBounds()
    {
        if (tileMapBounds.Size != Vector2.Zero) // Ensures this only works after the bounds have been defined
        {
            Vector2 playerPos = player.GlobalPosition;

            // Expand the bounds by one tile in all directions so the player doesn't instantly respawn when they brush the edge of the level.
            Rect2 expandedBounds = new Rect2(
            tileMapBounds.Position - tileSize, 
            tileMapBounds.Size + (tileSize * 2));

            bool isOutOfBounds = 
                playerPos.X < expandedBounds.Position.X ||
                playerPos.X > expandedBounds.End.X ||
                playerPos.Y > expandedBounds.End.Y; // Allow the player to go above the top of the level for things like jumping, but not below the bottom or beyond the sides.

            return isOutOfBounds;
        }
        return false;
    }

    // Sets the camera limits based on the tile map's used rect. This ensures the camera does not go out of bounds of the tile map.
    private void SetCameraLimits()
    {
        Camera2D camera = player.GetNode<Camera2D>("Camera2D");

        camera.LimitLeft = (int)tileMapBounds.Position.X;
        //camera.LimitTop = (int)pixelBounds.Position.Y;
        camera.LimitRight = (int)(tileMapBounds.Position.X + tileMapBounds.Size.X - tileSize.X);
        camera.LimitBottom = (int)(tileMapBounds.Position.Y + tileMapBounds.Size.Y - tileSize.Y);
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
