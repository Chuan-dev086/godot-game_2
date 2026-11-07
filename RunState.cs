using Godot;

public sealed class RunState : IPlayerState
{
    private readonly PlayerController _player;
    private PlayerInputSnapshot _input;

    public PlayerStateId Id => PlayerStateId.Run;

    public RunState(PlayerController player) => _player = player;

    public void Enter() { }
    public void Exit() { }
    public void HandleInput(PlayerInputSnapshot input) => _input = input;

    public void PhysicsUpdate(double delta)
    {
        _player.SetHorizontalIntent(_input.MoveDirection, onGround: true);
        if (_player.TryConsumeGroundJump())
        {
            _player.StateMachine.ChangeState(_player.JumpState);
            return;
        }

        if (Mathf.IsZeroApprox(_input.MoveDirection))
        {
            _player.StateMachine.ChangeState(_player.IdleState);
        }
    }
}
