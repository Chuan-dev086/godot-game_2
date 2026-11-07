public sealed class FallState : IPlayerState
{
    private readonly PlayerController _player;
    private PlayerInputSnapshot _input;

    public PlayerStateId Id => PlayerStateId.Fall;

    public FallState(PlayerController player) => _player = player;

    public void Enter() { }
    public void Exit() { }
    public void HandleInput(PlayerInputSnapshot input) => _input = input;

    public void PhysicsUpdate(double delta)
    {
        _player.SetHorizontalIntent(_input.MoveDirection, onGround: false);
        if (_player.TryConsumeAirJump())
        {
            _player.StateMachine.ChangeState(_player.JumpState);
        }
    }
}
