using Godot;

public partial class PlayerAnimator : Sprite2D
{
    [Export] private float jumpStretchFactor = 0.2f; //Factor to apply to the jump stretch effect (should always be positive)
    [Export] private float horizontalStretchFactor = 0.15f; //Factor to apply to the horizontal stretch effect (should always be positive)
    [Export] private float maxStretchFactor = 1.15f;
    [Export] private float minStretchFactor = 0.7f;

    private CharacterBody2D player;
    private PlayerController playerController;
    private Node2D stretchTransformNode; //Node to apply the stretch transform to
    private Vector2 originalScale = Vector2.One; //Original scale of the sprite
    private bool wasOnFloor = false; //If the player was on the floor last frame
    private Vector2 velocity;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        player = FindParent("Player") as CharacterBody2D;
        player = player as PlayerController;
        stretchTransformNode = FindParent("StretchTransformNode") as Node2D;
        originalScale = Scale;
    }

    public override void _Process(double delta)
    {
        velocity = player.Velocity;
        HandleFaceDirection();
        ApplySquashAndStretch();
        wasOnFloor = player.IsOnFloor();
    }

    //Handles facing the player in the right direction based on velocity
    private void HandleFaceDirection()
    {
        if (velocity.X > 0)
        {
            //Face right
            FlipH = false; // Ensure the sprite is not flipped horizontally
        }
        else if (velocity.X < 0)
        {
            //Face left
            FlipH = true; // Ensure the sprite is flipped horizontally
        }
    }

    private void ApplySquashAndStretch()
    {
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
        Rotation = 0f;
    }

    private void ApplyDirectionalStretch(Vector2 velocity)
    {
        float stretchY = Mathf.Clamp(velocity.Y / 600f, -jumpStretchFactor, jumpStretchFactor);
        float squashX = -stretchY * 0.5f; //invert and reduce for x-axis
        float stretchX = Mathf.Clamp(Mathf.Abs(velocity.X) / 300, 0, horizontalStretchFactor);

        //horizontal and vertical speed squash
        Vector2 targetScale = originalScale + new Vector2(squashX + stretchX, stretchY);
        Scale = Scale.Lerp(targetScale, 0.2f);
    }

    private void ApplyAngularStretch(Vector2 velocity)
    {
        float angle = Mathf.Atan2(velocity.Y, velocity.X);
        float speed = velocity.Length();
        float stretchAmount = Mathf.Clamp(speed / 600f, minStretchFactor - 1f, maxStretchFactor - 1f);

        float scaleAlongMotion = 1f + stretchAmount;
        float scalePerpendicular = 1f - (stretchAmount * 0.5f);

        stretchTransformNode.Rotation = angle;
        Rotation = -angle;

        stretchTransformNode.Scale = new Vector2(scaleAlongMotion, scalePerpendicular);
        Scale = Scale.Lerp(originalScale, 0.4f);
    }

    private void ResetStretch()
    {
        stretchTransformNode.Scale = originalScale;
        Scale = originalScale;
    }

    private void ApplyLandingSquash()
    {
        if (!wasOnFloor && player.IsOnFloor())
        {
            ResetRotation();
            ResetStretch();
            Scale = new Vector2(originalScale.X * 1.2f, originalScale.Y * 0.8f);
        }
    }
}
