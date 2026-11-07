using Godot;

/// <summary>Phase 1 最小流程：死亡重生和 R 键重启。</summary>
public partial class GameManager : Node2D
{
    [Export] public NodePath PlayerPath { get; set; } = new NodePath("");
    [Export] public NodePath SpawnPointPath { get; set; } = new NodePath("");
    private PlayerController? _player;
    private Marker2D? _spawnPoint;

    public override void _Ready()
    {
        _player = GetNodeOrNull<PlayerController>(PlayerPath);
        _spawnPoint = GetNodeOrNull<Marker2D>(SpawnPointPath);
        if (_player is null || _spawnPoint is null)
        {
            GD.PushError("GameManager requires valid PlayerPath and SpawnPointPath.");
            return;
        }
        _player.Died += RespawnPlayer;
        RespawnPlayer();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (!@event.IsActionPressed("restart")) return;
        RespawnPlayer();
        GetViewport().SetInputAsHandled();
    }

    public override void _ExitTree()
    {
        if (_player is not null) _player.Died -= RespawnPlayer;
    }

    private void RespawnPlayer()
    {
        if (_player is not null && _spawnPoint is not null) _player.ResetAt(_spawnPoint.GlobalPosition);
    }
}
