/// <summary>所有玩家移动状态遵循的生命周期和每帧行为契约。</summary>
public interface IPlayerState
{
    PlayerStateId Id { get; }
    void Enter();
    void Exit();
    void HandleInput(PlayerInputSnapshot input);
    void PhysicsUpdate(double delta);
}
