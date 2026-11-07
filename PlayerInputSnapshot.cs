/// <summary>单个物理帧内不可变的玩家输入快照。</summary>
public readonly record struct PlayerInputSnapshot(
    float MoveDirection,
    bool JumpPressed,
    bool JumpReleased,
    bool JumpHeld);
