public sealed class JumpState : IPlayerState
{
    private readonly PlayerController _player;
    private PlayerInputSnapshot _input;

    public PlayerStateId Id => PlayerStateId.Jump;

    public JumpState(PlayerController player) => _player = player;

    public void Enter() => _player.BeginJump();
    public void Exit() { }
    public void HandleInput(PlayerInputSnapshot input) => _input = input;

    public void PhysicsUpdate(double delta)
    {
        _player.SetHorizontalIntent(_input.MoveDirection, onGround: false);
        if (_player.TryConsumeAirJump())
        {
            _player.BeginJump();
            return;
        }

        if (_input.JumpReleased)
        {
            _player.ApplyJumpRelease();
        }
    }
}
