using Godot;

/// <summary>
/// Phase 3：管理收集物计数、危险区死亡、终点通关信号。
/// 由 GameManager 在 _Ready 中注册。
/// </summary>
public partial class LevelController : Node
{
    // ── 信号 ───────────────────────────────────────────────────────────────
    [Signal] public delegate void CollectiblePickedUpEventHandler(int current, int total);
    [Signal] public delegate void PlayerHitHazardEventHandler();
    [Signal] public delegate void PlayerReachedGoalEventHandler();

    // ── 内部状态 ────────────────────────────────────────────────────────────
    private int _totalCollectibles;
    private int _collected;

    public int Collected => _collected;
    public int Total     => _totalCollectibles;

    public override void _Ready()
    {
        // 自动扫描场景树中所有带对应 group 的 Area2D 并连线信号
        // 分组在 Main.tscn 中用 groups 属性标记
        foreach (Node node in GetTree().GetNodesInGroup("collectible"))
        {
            if (node is Area2D area)
            {
                _totalCollectibles++;
                area.BodyEntered += body => OnCollectibleBodyEntered(area, body);
            }
        }

        foreach (Node node in GetTree().GetNodesInGroup("hazard"))
        {
            if (node is Area2D area)
                area.BodyEntered += OnHazardBodyEntered;
        }

        foreach (Node node in GetTree().GetNodesInGroup("goal"))
        {
            if (node is Area2D area)
                area.BodyEntered += OnGoalBodyEntered;
        }
    }

    /// <summary>重置收集物状态（重生后）。</summary>
    public void ResetLevel()
    {
        _collected = 0;
        // 重新显示所有收集物
        foreach (Node node in GetTree().GetNodesInGroup("collectible"))
            node.SetDeferred("visible", true);
    }

    // ── 私有回调 ────────────────────────────────────────────────────────────

    private void OnCollectibleBodyEntered(Area2D collectible, Node body)
    {
        if (body is not CharacterBody2D) return;
        if (!collectible.Visible) return;   // 已拾取过

        collectible.Visible = false;
        _collected++;
        EmitSignal(SignalName.CollectiblePickedUp, _collected, _totalCollectibles);
    }

    private void OnHazardBodyEntered(Node body)
    {
        if (body is CharacterBody2D)
            EmitSignal(SignalName.PlayerHitHazard);
    }

    private void OnGoalBodyEntered(Node body)
    {
        if (body is CharacterBody2D)
            EmitSignal(SignalName.PlayerReachedGoal);
    }
}
