using System;

/// <summary>唯一负责状态生命周期切换的协调器。</summary>
public sealed class PlayerStateMachine
{
    public IPlayerState CurrentState { get; private set; }
    public PlayerStateId CurrentStateId => CurrentState.Id;

    public PlayerStateMachine(IPlayerState initialState)
    {
        CurrentState = initialState ?? throw new ArgumentNullException(nameof(initialState));
        CurrentState.Enter();
    }

    public void ChangeState(IPlayerState newState)
    {
        ArgumentNullException.ThrowIfNull(newState);
        if (CurrentState.Id == newState.Id)
        {
            return;
        }

        CurrentState.Exit();
        CurrentState = newState;
        CurrentState.Enter();
    }

    public void HandleInput(PlayerInputSnapshot input) => CurrentState.HandleInput(input);

    public void PhysicsUpdate(double delta) => CurrentState.PhysicsUpdate(delta);
}
