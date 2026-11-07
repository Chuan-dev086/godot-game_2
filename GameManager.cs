using Godot;

/// <summary>
/// Phase 3 GameManager：Playing / GameOver / LevelComplete 状态流转，
/// 协调 LevelController 信号与 HUD 刷新。
/// </summary>
public partial class GameManager : Node2D
{
    // ── 状态枚举 ────────────────────────────────────────────────────────────
    public enum GameState { Playing, GameOver, LevelComplete }

    // ── 导出节点路径 ────────────────────────────────────────────────────────
    [Export] public NodePath PlayerPath       { get; set; } = new NodePath("");
    [Export] public NodePath SpawnPointPath   { get; set; } = new NodePath("");
    [Export] public NodePath LevelCtrlPath    { get; set; } = new NodePath("");

    // HUD 控件路径
    [Export] public NodePath CollectLabelPath { get; set; } = new NodePath("");
    [Export] public NodePath StatusLabelPath  { get; set; } = new NodePath("");
    [Export] public NodePath HintLabelPath    { get; set; } = new NodePath("");

    // ── 私有引用 ────────────────────────────────────────────────────────────
    private PlayerController?  _player;
    private Marker2D?          _spawnPoint;
    private LevelController?   _levelCtrl;
    private Label?             _collectLabel;
    private Label?             _statusLabel;
    private Label?             _hintLabel;

    private GameState _state = GameState.Playing;

    // ── 生命周期 ────────────────────────────────────────────────────────────

    public override void _Ready()
    {
        _player      = GetNodeOrNull<PlayerController>(PlayerPath);
        _spawnPoint  = GetNodeOrNull<Marker2D>(SpawnPointPath);
        _levelCtrl   = GetNodeOrNull<LevelController>(LevelCtrlPath);
        _collectLabel = GetNodeOrNull<Label>(CollectLabelPath);
        _statusLabel  = GetNodeOrNull<Label>(StatusLabelPath);
        _hintLabel    = GetNodeOrNull<Label>(HintLabelPath);

        if (_player is null || _spawnPoint is null)
        {
            GD.PushError("GameManager: PlayerPath 或 SpawnPointPath 未配置。");
            return;
        }

        // 连接玩家越界死亡
        _player.Died += OnPlayerDied;

        // 连接关卡控制器信号
        if (_levelCtrl is not null)
        {
            _levelCtrl.CollectiblePickedUp += OnCollectiblePickedUp;
            _levelCtrl.PlayerHitHazard     += OnPlayerDied;
            _levelCtrl.PlayerReachedGoal   += OnLevelComplete;
        }

        // 延迟一帧再执行首次进入，让 LevelBuilder（TileMapLayer）有时间
        // 完成物理体生成，避免玩家在第一个物理帧穿透地面
        CallDeferred(nameof(EnterPlayingState));
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("restart")) return;
        GetViewport().SetInputAsHandled();

        if (_state is GameState.GameOver or GameState.LevelComplete)
            EnterPlayingState();
    }

    public override void _ExitTree()
    {
        if (_player is not null)
        {
            _player.Died -= OnPlayerDied;
        }
        if (_levelCtrl is not null)
        {
            _levelCtrl.CollectiblePickedUp -= OnCollectiblePickedUp;
            _levelCtrl.PlayerHitHazard     -= OnPlayerDied;
            _levelCtrl.PlayerReachedGoal   -= OnLevelComplete;
        }
    }

    // ── 状态迁移 ────────────────────────────────────────────────────────────

    private void EnterPlayingState()
    {
        _state = GameState.Playing;
        _levelCtrl?.ResetLevel();
        RespawnPlayer();
        _player?.SetLocked(false);
        UpdateCollectLabel(0, _levelCtrl?.Total ?? 0);
        SetStatusText("");
        SetHintVisible(true);
    }

    private void OnPlayerDied()
    {
        if (_state != GameState.Playing) return;
        _state = GameState.GameOver;
        SetStatusText("Game Over\n按 R 重试");
        SetHintVisible(false);
        _player?.SetLocked(true);
    }

    private void OnLevelComplete()
    {
        if (_state != GameState.Playing) return;
        _state = GameState.LevelComplete;
        SetStatusText("Level Complete!\n按 R 重新开始");
        SetHintVisible(false);
        _player?.SetLocked(true);
    }

    private void OnCollectiblePickedUp(int current, int total)
    {
        UpdateCollectLabel(current, total);
    }

    // ── 工具方法 ────────────────────────────────────────────────────────────

    private void RespawnPlayer()
    {
        if (_player is null || _spawnPoint is null) return;
        _player.SetPhysicsProcess(true);
        _player.ResetAt(_spawnPoint.GlobalPosition);
    }

    private void UpdateCollectLabel(int current, int total)
    {
        if (_collectLabel is not null)
            _collectLabel.Text = $"收集物: {current}/{total}";
    }

    private void SetStatusText(string text)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = text;
    }

    private void SetHintVisible(bool visible)
    {
        if (_hintLabel is not null)
            _hintLabel.Visible = visible;
    }
}
