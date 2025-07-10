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


    private Vector2 prevVelocity = Vector2.Zero; //The velocity from the previous frame
    private AnimationPlayer animationPlayer;
	private bool wasOnFloor = false; //If the player was on the floor last frame
	private float timeSinceLastOnFloor = 0.0f; //Time since the player was last on the floor
	private float jumpBufferTimeCounter = 0.0f; //Time since the jump button was pressed
    private Sprite2D sprite;
    private Vector2 originalScale = Vector2.One; //Original scale of the sprite
    private ShaderMaterial material;
    private Node2D stretchTransformNode; //Node to apply the stretch transform to

    public override void _Ready()
    {
		animationPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        sprite = GetNode<Sprite2D>("StretchTransformNode/Sprite");
        originalScale = sprite.Scale; //Store the original scale of the sprite
        material = sprite.Material as ShaderMaterial;
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
        //velocity.Y = Mathf.Lerp(prevVelocity.Y, velocity.Y, 0.8f);

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
        //material.SetShaderParameter("velocity", Velocity);
        wasOnFloor = IsOnFloor();// Has to come after ApplySquashAndStretch and MoveAndSlide to ensure the squash and stretch is applied before checking if the player was on the floor and get the correct velocity values.
    }

    private void ApplySquashAndStretch1()
    {
        Vector2 velocity = Velocity;
        if (Mathf.Abs(velocity.X) == 0 || Mathf.Abs(velocity.Y) == 0)
        {
            float stretchY = Mathf.Clamp(velocity.Y / 600, -jumpStretchFactor, jumpStretchFactor); //Jump/fall stretch factor

            stretchTransformNode.Rotation = 0f;
            sprite.Rotation = 0f; // Reset rotation to avoid rotation when only vertical movement
            float squashX = -stretchY * 0.5f; //invert and reduce for x-axis

            //horizontal speed squash
            float stretchX = Mathf.Clamp(Mathf.Abs(velocity.X) / 300, 0, horizontalStretchFactor);

            Vector2 targetScale = originalScale + new Vector2(squashX + stretchX, stretchY);
            sprite.Scale = sprite.Scale.Lerp(targetScale, 0.2f); //Smoothly interpolate to the target scale
        }
        else
        {
            // Avoid zero-length direction
            if (velocity.Length() < 10f)
            {
                stretchTransformNode.Scale = originalScale;
                return;
            }

            // Compute motion angle and stretch amount
            float angle = Mathf.Atan2(velocity.Y, velocity.X);

            float speed = velocity.Length();
            float stretchAmount = Mathf.Clamp(speed / 600f, minStretchFactor - 1f, maxStretchFactor - 1f);

            // Compute squash and stretch
            float scaleAlongMotion = 1f + stretchAmount;
            float scalePerpendicular = 1f - (stretchAmount * 0.5f);

            // Build stretch matrix
            // First, reset transform
            stretchTransformNode.Rotation = 0f;
            stretchTransformNode.Scale = Vector2.One;

            // Apply rotation
            stretchTransformNode.Rotation = angle;
            sprite.Rotation = -stretchTransformNode.Rotation; // prevents the sprite itself from rotating when the stretch transform is rotated

            // Apply scale in rotated space
            stretchTransformNode.Scale = new Vector2(scaleAlongMotion, scalePerpendicular);

            // If sprite is scaled by other half of if else statment then smoothly reset it to original scale
            sprite.Scale = sprite.Scale.Lerp(originalScale, 0.4f); //Smoothly interpolate to the target scale
        }
        // landing squash
        if (!wasOnFloor && IsOnFloor())
        {
            stretchTransformNode.Rotation = sprite.Rotation = 0f;
            sprite.Scale = new Vector2(originalScale.X * 1.2f, originalScale.Y * 0.8f);
        }
    }

    private bool CanJump()
    {
        return IsOnFloor() || timeSinceLastOnFloor < coyoteTime;
    }

    /*
    //applies squash and stretch to the player sprite based on the velocity
    private void ApplySquashAndStretch2()
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

    private void ApplySquashAndStretch3()
    {
        Vector2 velocity = Velocity;
        Vector2 velocityNorm = velocity.Normalized();
        float speed = velocity.Length();

        // How strong the stretch is
        float stretchAmount = Mathf.Clamp(speed / 600, 0, 0.3f);

        // Stretch more in the movment direction
        Vector2 stretchDir = new Vector2(
            1 + stretchAmount * Mathf.Abs(velocityNorm.X),
            1 + stretchAmount * Mathf.Abs(velocityNorm.Y)
        );

        // Inverse squash: if x stretches, y compresses slightly and vice versa
        float squashCompensation = 1 / Mathf.Sqrt(stretchDir.X * stretchDir.Y); //keep volume consistent

        Vector2 finalScale = new Vector2(
            originalScale.X * stretchDir.X * squashCompensation,
            originalScale.Y * stretchDir.Y * squashCompensation
        );

        // Smoothly interpolate to the target scale
        sprite.Scale = sprite.Scale.Lerp(finalScale, 0.2f);
    }

    private void ApplySquashAndStretch4()
    {
        Vector2 velocity = Velocity;

        // Avoid zero-length direction
        if (velocity.Length() < 10f)
        {
            stretchTransformNode.Scale = originalScale;
            return;
        }

        // Compute motion angle and stretch amount
        float angle = Mathf.Atan2(velocity.Y, velocity.X);

        float speed = velocity.Length();
        float stretchAmount = Mathf.Clamp(speed / 600f, minStretchFactor-1f, maxStretchFactor - 1.15f);

        // Compute squash and stretch
        float scaleAlongMotion = 1f + stretchAmount;
        float scalePerpendicular = 1f - (stretchAmount * 0.5f);

        // Build stretch matrix
        // First, reset transform
        stretchTransformNode.Rotation = 0f;
        stretchTransformNode.Scale = Vector2.One;

        // Apply rotation
        stretchTransformNode.Rotation = angle;
        sprite.Rotation = -stretchTransformNode.Rotation; // prevents the sprite itself from rotating when the stretch transform is rotated

        // Apply scale in rotated space
        stretchTransformNode.Scale = new Vector2(scaleAlongMotion, scalePerpendicular);

        // Landing squash
        if (!wasOnFloor && IsOnFloor())
        {
            stretchTransformNode.Rotation = 0f;
            stretchTransformNode.Scale = new Vector2(1.2f, 0.8f);
        }
    }
    */
    private void ApplySquashAndStretch()
    {
        Vector2 velocity = Velocity;
        float speed = velocity.Length();
        bool isMovingHorizontally = Mathf.Abs(velocity.X) > 0;
        bool isMovingVertically = Mathf.Abs(velocity.Y) > 0;

        ResetRotation();

        if (!isMovingHorizontally || !isMovingVertically)
        {
            ApplyDirectionalStretch(velocity);
        }
        else if (speed >= 10f)
        {
            ApplyAngularStretch(velocity);
        }
        else
        {
            ResetStretch();
            return;
        }

        ApplyLandingSquash();
    }

    private void ResetRotation()
    {
        stretchTransformNode.Rotation = 0f;
        sprite.Rotation = 0f;
    }

    private void ApplyDirectionalStretch(Vector2 velocity)
    {
        float stretchY = Mathf.Clamp(velocity.Y / 600f, -jumpStretchFactor, jumpStretchFactor);
        float squashX = -stretchY * 0.5f; //invert and reduce for x-axis
        float stretchX = Mathf.Clamp(Mathf.Abs(velocity.X) / 300, 0, horizontalStretchFactor);

        //horizontal and vertical speed squash
        Vector2 targetScale = originalScale + new Vector2(squashX + stretchX, stretchY);
        sprite.Scale = sprite.Scale.Lerp(targetScale, 0.2f);
    }

    private void ApplyAngularStretch(Vector2 velocity)
    {
        float angle = Mathf.Atan2(velocity.Y, velocity.X);
        float speed = velocity.Length();
        float stretchAmount = Mathf.Clamp(speed / 600f, minStretchFactor - 1f, maxStretchFactor - 1f);

        float scaleAlongMotion = 1f + stretchAmount;
        float scalePerpendicular = 1f - (stretchAmount * 0.5f);

        stretchTransformNode.Rotation = angle;
        sprite.Rotation = -angle;

        stretchTransformNode.Scale = new Vector2(scaleAlongMotion, scalePerpendicular);
        sprite.Scale = sprite.Scale.Lerp(originalScale, 0.4f);
    }

    private void ResetStretch()
    {
        stretchTransformNode.Scale = originalScale;
    }

    private void ApplyLandingSquash()
    {
        if (!wasOnFloor && IsOnFloor())
        {
            ResetRotation();
            sprite.Scale = new Vector2(originalScale.X * 1.2f, originalScale.Y * 0.8f);
        }
    }
}
