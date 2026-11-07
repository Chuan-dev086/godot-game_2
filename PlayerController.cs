using Godot;

/// <summary>唯一物理入口：输入、状态、统一物理、单次 MoveAndSlide、状态转换。</summary>
public partial class PlayerController : CharacterBody2D
{
    [Signal] public delegate void DiedEventHandler();

    [Export] public PlayerMovementSettings MovementSettings { get; set; } = new();
    [Export] public float DeathY { get; set; } = 950.0f;

    public PlayerStateMachine StateMachine { get; private set; } = null!;
    public IdleState IdleState { get; private set; } = null!;
    public RunState RunState { get; private set; } = null!;
    public JumpState JumpState { get; private set; } = null!;
    public FallState FallState { get; private set; } = null!;

    private Godot.Timer _coyoteTimer = null!;
    private Godot.Timer _jumpBufferTimer = null!;
    private bool _deathReported;
    private bool _wasOnFloor;
    private bool _jumpReleasedEarly;
    private int _jumpCount;
    private float _horizontalIntent;
    private bool _horizontalIntentIsGrounded;

    public override void _Ready()
    {
        _coyoteTimer = GetNode<Godot.Timer>("CoyoteTimer");
        _jumpBufferTimer = GetNode<Godot.Timer>("JumpBufferTimer");
        _coyoteTimer.WaitTime = MovementSettings.CoyoteTimeSeconds;
        _jumpBufferTimer.WaitTime = MovementSettings.JumpBufferSeconds;

        IdleState = new IdleState(this);
        RunState = new RunState(this);
        JumpState = new JumpState(this);
        FallState = new FallState(this);
        StateMachine = new PlayerStateMachine(IdleState);
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_deathReported) return;

        var input = CaptureInput();
        if (input.JumpPressed) _jumpBufferTimer.Start();

        StateMachine.HandleInput(input);
        StateMachine.PhysicsUpdate(delta);
        ApplyPhysics((float)delta);

        // The sole MoveAndSlide call in each physics frame.
        MoveAndSlide();

        UpdateStateAfterMovement();
        ReportDeathWhenOutOfBounds();
    }

    public void ResetAt(Vector2 spawnPosition)
    {
        GlobalPosition = spawnPosition;
        Velocity = Vector2.Zero;
        _deathReported = false;
        _wasOnFloor = false;
        _jumpReleasedEarly = false;
        _jumpCount = 0;
        _horizontalIntent = 0.0f;
        _coyoteTimer?.Stop();
        _jumpBufferTimer?.Stop();
        if (StateMachine is not null) StateMachine.ChangeState(IdleState);
    }

    public bool TryConsumeGroundJump()
    {
        if (!HasBufferedJump()) return false;
        ConsumeJumpRequest();
        return true;
    }

    public bool TryConsumeAirJump()
    {
        if (!HasBufferedJump()) return false;
        var hasCoyoteJump = !_coyoteTimer.IsStopped();
        if (!hasCoyoteJump && _jumpCount >= MovementSettings.MaxJumps) return false;
        ConsumeJumpRequest();
        return true;
    }

    public void BeginJump()
    {
        if (_jumpCount >= MovementSettings.MaxJumps) return;

        _jumpCount++;
        _jumpReleasedEarly = false;
        Velocity = new Vector2(Velocity.X, -MovementSettings.JumpVelocity);
        _coyoteTimer.Stop();
    }

    /// <summary>松键后直到到达最高点，持续施加额外重力，形成显著短跳。</summary>
    public void ApplyJumpRelease()
    {
        if (Velocity.Y >= 0.0f) return;
        _jumpReleasedEarly = true;
    }

    public void SetHorizontalIntent(float direction, bool onGround)
    {
        _horizontalIntent = direction;
        _horizontalIntentIsGrounded = onGround;
    }

    private PlayerInputSnapshot CaptureInput() => new(
        Input.GetAxis("move_left", "move_right"),
        Input.IsActionJustPressed("jump"),
        Input.IsActionJustReleased("jump"),
        Input.IsActionPressed("jump"));

    private void ApplyPhysics(float deltaSeconds)
    {
        ApplyHorizontalMovement(deltaSeconds);
        ApplyGravity(deltaSeconds);
    }

    private void ApplyHorizontalMovement(float deltaSeconds)
    {
        if (!Mathf.IsZeroApprox(_horizontalIntent))
        {
            var acceleration = _horizontalIntentIsGrounded ? MovementSettings.GroundAcceleration : MovementSettings.AirAcceleration;
            Velocity = new Vector2(Mathf.MoveToward(Velocity.X, _horizontalIntent * MovementSettings.MaxRunSpeed, acceleration * deltaSeconds), Velocity.Y);
            return;
        }

        var deceleration = _horizontalIntentIsGrounded ? MovementSettings.GroundDeceleration : MovementSettings.AirAcceleration;
        Velocity = new Vector2(Mathf.MoveToward(Velocity.X, 0.0f, deceleration * deltaSeconds), Velocity.Y);
    }

    private void ApplyGravity(float deltaSeconds)
    {
        var isAirborneState = StateMachine.CurrentStateId is PlayerStateId.Jump or PlayerStateId.Fall;
        if (!IsOnFloor() || isAirborneState)
        {
            var gravity = MovementSettings.Gravity;
            if (_jumpReleasedEarly && Velocity.Y < 0.0f)
                gravity *= MovementSettings.JumpReleaseGravityMultiplier;
            else if (Velocity.Y >= 0.0f)
                _jumpReleasedEarly = false;

            Velocity = new Vector2(Velocity.X, Mathf.Min(Velocity.Y + gravity * deltaSeconds, MovementSettings.MaxFallSpeed));
            return;
        }

        if (Velocity.Y > 0.0f) Velocity = new Vector2(Velocity.X, 0.0f);
    }

    private void UpdateStateAfterMovement()
    {
        var isOnFloor = IsOnFloor();
        if (isOnFloor)
        {
            _coyoteTimer.Stop();
            if (Velocity.Y >= 0.0f) _jumpCount = 0;
            if (HasBufferedJump())
            {
                ConsumeJumpRequest();
                StateMachine.ChangeState(JumpState);
            }
            else if (StateMachine.CurrentStateId is PlayerStateId.Jump or PlayerStateId.Fall)
            {
                StateMachine.ChangeState(Mathf.IsZeroApprox(_horizontalIntent) ? IdleState : RunState);
            }
        }
        else
        {
            if (_wasOnFloor) _coyoteTimer.Start();

            if (StateMachine.CurrentStateId is PlayerStateId.Idle or PlayerStateId.Run)
                StateMachine.ChangeState(FallState);
            else if (StateMachine.CurrentStateId == PlayerStateId.Jump && Velocity.Y >= 0.0f)
                StateMachine.ChangeState(FallState);
        }

        _wasOnFloor = isOnFloor;
    }

    private bool HasBufferedJump() => !_jumpBufferTimer.IsStopped();

    private void ConsumeJumpRequest()
    {
        _jumpBufferTimer.Stop();
        _coyoteTimer.Stop();
    }

    private void ReportDeathWhenOutOfBounds()
    {
        if (GlobalPosition.Y <= DeathY) return;
        _deathReported = true;
        EmitSignal(SignalName.Died);
    }
}
