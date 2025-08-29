using Godot;
using Godot.NativeInterop;
using System;

public partial class PlayerController : CharacterBody2D
{
	[Export] private float Speed = 300.0f; //The players movment speed
	[Export] private float JumpVelocity = -400.0f; //The upwards velocity applied when the player jumps
    [Export] private float coyoteTime = 0.1f; //Time in seconds to allow jumping after leaving the ground
	[Export] private float jumpBufferTime = 0.1f; //Time in seconds to allow jumping after pressing the jump button
    [Export] private float jumpStretchFactor = 0.2f; //Factor to apply to the jump stretch effect (should always be positive)
    [Export] private float horizontalStretchFactor = 0.15f; //Factor to apply to the horizontal stretch effect (should always be positive)
    [Export] private float maxStretchFactor = 1.15f;
    [Export] private float minStretchFactor = 0.7f;

    public LevelLoader LevelLoader; //Reference to the LevelLoader node to access the player spawn point

    private Vector2 prevVelocity = Vector2.Zero; //The velocity from the previous frame
	private bool wasOnFloor = false; //If the player was on the floor last frame
	private float timeSinceLastOnFloor = 0.0f; //Time since the player was last on the floor
	private float jumpBufferTimeCounter = 0.0f; //Time since the jump button was pressed
    private Sprite2D sprite;
    private Vector2 originalScale = Vector2.One; //Original scale of the sprite
    private Node2D stretchTransformNode; //Node to apply the stretch transform to

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        sprite = GetNode<Sprite2D>("StretchTransformNode/Sprite");
        originalScale = sprite.Scale; //Store the original scale of the sprite
        stretchTransformNode = GetNode<Node2D>("StretchTransformNode");
    }
    public override void _PhysicsProcess(double delta)
	{
		Vector2 velocity = Velocity;
        //Incrementing the time since the player was last on the floor.
        if (!IsOnFloor()) timeSinceLastOnFloor += (float)delta;
		else timeSinceLastOnFloor = 0.0f;

        //Handling the jump buffer time
		if (jumpBufferTimeCounter > 0 || Input.IsActionJustPressed("jump"))
		{
            if (jumpBufferTimeCounter < jumpBufferTime)
            {
                jumpBufferTimeCounter += (float)delta;
            }
            else
            {
                jumpBufferTimeCounter = 0.0f;
            }
        }


        //Applying y-velocity damping/lerping.
        velocity.Y = Mathf.Lerp(prevVelocity.Y, velocity.Y, 0.8f);

		//Add the gravity.
		if (!IsOnFloor())
		{
            //Handling airhang
            if (Mathf.Abs(velocity.Y) < 50)
            {
                velocity += GetGravity() * (float)delta * 0.85f;
            }
            else
            {
                velocity += GetGravity() * (float)delta;
            }
		}

		//Handle Jump.
		if ((Input.IsActionPressed("jump") || jumpBufferTimeCounter > 0) && CanJump())
		{
			//animationPlayer.Stop();
            velocity.Y = JumpVelocity;
			//animationPlayer.Play("jump");
        }
        //Handling variable jump height.
        if (Input.IsActionJustReleased("jump") && velocity.Y < 0)
        {
			velocity.Y *= 0.4f;
        }

        //Handling air resistance.
        if (!IsOnFloor())
		{
			velocity.X = Mathf.Lerp(prevVelocity.X, velocity.X, 0.1f);
		}

        //Get the input direction and handle the movement/deceleration.
        Vector2 direction = Input.GetVector("moveLeft", "moveRight", "moveUp", "moveDown");
		if (direction != Vector2.Zero)
		{
			velocity.X = direction.X * Speed;
		}
		else
		{
			velocity.X = Mathf.MoveToward(Velocity.X, 0, Speed);
		}

		Velocity = velocity;
		MoveAndSlide();
		prevVelocity = Velocity;
    }

    public void Respawn(bool skipAnim = false)
    {
        if (LevelLoader != null)
        {
            GD.Print("Respawning player at spawn point: " + LevelLoader.playerSpawnPoint);
            GlobalPosition = LevelLoader.playerSpawnPoint;
            //Velocity = Vector2.Zero;
            //timeSinceLastOnFloor = 0.0f;
            //jumpBufferTimeCounter = 0.0f;
        }
        else
        {
            GD.PrintErr("LevelLoader is not set in PlayerController. Cannot respawn.");
        }
    }

    private bool CanJump()
    {
        return IsOnFloor() || timeSinceLastOnFloor < coyoteTime;
    }
}
