using Godot;

namespace AgentPlatformer;

/// <summary>Phase 1 玩家控制器。每个物理帧恰好调用一次 MoveAndSlide。</summary>
public partial class PlayerController : CharacterBody2D
{
    [Signal] public delegate void DiedEventHandler();

    [Export] public float MaxRunSpeed { get; set; } = 340.0f;
    [Export] public float GroundAcceleration { get; set; } = 2_200.0f;
    [Export] public float GroundDeceleration { get; set; } = 2_800.0f;
    [Export] public float AirAcceleration { get; set; } = 1_200.0f;
    [Export] public float Gravity { get; set; } = 1_500.0f;
    [Export] public float MaxFallSpeed { get; set; } = 1_000.0f;
    [Export] public float JumpVelocity { get; set; } = 590.0f;
    [Export] public float DeathY { get; set; } = 950.0f;

    private bool _deathReported;

    public override void _PhysicsProcess(double delta)
    {
        if (_deathReported) return;

        var seconds = (float)delta;
        ApplyHorizontalMovement(Input.GetAxis("move_left", "move_right"), seconds);
        ApplyVerticalMovement(seconds);

        // Deliberately the only MoveAndSlide call for this physics frame.
        MoveAndSlide();

        if (GlobalPosition.Y > DeathY)
        {
            _deathReported = true;
            EmitSignal(SignalName.Died);
        }
    }

    public void ResetAt(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
        Velocity = Vector2.Zero;
        _deathReported = false;
    }

    private void ApplyHorizontalMovement(float inputDirection, float seconds)
    {
        if (!Mathf.IsZeroApprox(inputDirection))
        {
            var acceleration = IsOnFloor() ? GroundAcceleration : AirAcceleration;
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, inputDirection * MaxRunSpeed, acceleration * seconds), Velocity.Y);
            return;
        }

        var deceleration = IsOnFloor() ? GroundDeceleration : AirAcceleration;
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0.0f, deceleration * seconds), Velocity.Y);
    }

    private void ApplyVerticalMovement(float seconds)
    {
        if (IsOnFloor())
        {
            if (Velocity.Y > 0.0f) Velocity = new Vector2(Velocity.X, 0.0f);
            if (Input.IsActionJustPressed("jump")) Velocity = new Vector2(Velocity.X, -JumpVelocity);
            return;
        }

        Velocity = new Vector2(Velocity.X, Mathf.Min(Velocity.Y + Gravity * seconds, MaxFallSpeed));
    }
}
