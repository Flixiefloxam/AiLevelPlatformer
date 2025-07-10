using Godot;
using Godot.NativeInterop;
using System;

public partial class PlayerController : CharacterBody2D
{
	[Export] public float Speed = 300.0f; //The players movment speed
	[Export] public float JumpVelocity = -400.0f; //The upwards velocity applied when the player jumps
    [Export] public float coyoteTime = 0.1f; //Time in seconds to allow jumping after leaving the ground
	[Export] public float jumpBufferTime = 0.1f; //Time in seconds to allow jumping after pressing the jump button
    [Export] public float jumpStretchFactor = 0.5f; //Factor to apply to the jump stretch effect (should always be positive)
    [Export] public float horizontalStretchFactor = 0.3f; //Factor to apply to the horizontal stretch effect (should always be positive)

    private Vector2 prevVelocity = Vector2.Zero; //The velocity from the previous frame
    private AnimationPlayer animationPlayer;
	private bool wasOnFloor = false; //If the player was on the floor last frame
	private float timeSinceLastOnFloor = 0.0f; //Time since the player was last on the floor
	private float jumpBufferTimeCounter = 0.0f; //Time since the jump button was pressed
    private Sprite2D sprite;
    private Vector2 originalScale = Vector2.One; //Original scale of the sprite

    public override void _Ready()
    {
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        sprite = GetNode<Sprite2D>("Sprite");
        originalScale = sprite.Scale; //Store the original scale of the sprite
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
        ApplySquashAndStretch();
        wasOnFloor = IsOnFloor();// Has to come after ApplySquashAndStretch and MoveAndSlide to ensure the squash and stretch is applied before checking if the player was on the floor and get the correct velocity values.
    }

    private void ApplySquashAndStretch()
    {
        Vector2 velocity = Velocity;
        float stretchY = Mathf.Clamp(velocity.Y / 600, -jumpStretchFactor, jumpStretchFactor); //Jump/fall stretch factor
        float squashX = -stretchY * 0.5f; //invert and reduce for x-axis

        //horizontal speed squash
        float stretchX = Mathf.Clamp(Mathf.Abs(velocity.X) / 300, 0, horizontalStretchFactor);

        Vector2 targetScale = originalScale + new Vector2(squashX + stretchX, stretchY);
        sprite.Scale = sprite.Scale.Lerp(targetScale, 0.2f); //Smoothly interpolate to the target scale

        // landing squash
        if (!wasOnFloor && IsOnFloor())
        {
            sprite.Scale = new Vector2(originalScale.X * 1.2f, originalScale.Y * 0.8f);
        }
    }


    private bool CanJump()
	{
		return IsOnFloor() || timeSinceLastOnFloor < coyoteTime ;
    }
}
