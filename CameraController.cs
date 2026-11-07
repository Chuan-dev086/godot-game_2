using Godot;

/// <summary>平滑跟随目标，且将相机中心钳制在关卡边界内。</summary>
public partial class CameraController : Camera2D
{
    [Export] public NodePath TargetPath { get; set; } = new NodePath("");
    [Export] public float FollowSpeed { get; set; } = 7.0f;
    private Node2D? _target;

    public override void _Ready()
    {
        _target = GetNodeOrNull<Node2D>(TargetPath);
        MakeCurrent();
    }

    public override void _Process(double delta)
    {
        if (_target is null) return;
        var weight = 1.0f - Mathf.Exp(-FollowSpeed * (float)delta);
        GlobalPosition = GlobalPosition.Lerp(_target.GlobalPosition, weight);
        GlobalPosition = new Vector2(Mathf.Clamp(GlobalPosition.X, LimitLeft, LimitRight), Mathf.Clamp(GlobalPosition.Y, LimitTop, LimitBottom));
    }
}
