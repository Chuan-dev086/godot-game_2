using Godot;

/// <summary>集中存放可调的移动与手感参数，可作为 Godot Resource 复用。</summary>
[GlobalClass]
public partial class PlayerMovementSettings : Resource
{
    [ExportGroup("Horizontal Movement")]
    [Export] public float MaxRunSpeed { get; set; } = 340.0f;
    [Export] public float GroundAcceleration { get; set; } = 2_200.0f;
    [Export] public float GroundDeceleration { get; set; } = 2_800.0f;
    [Export] public float AirAcceleration { get; set; } = 1_200.0f;

    [ExportGroup("Vertical Movement")]
    [Export] public float Gravity { get; set; } = 1_400.0f;
    [Export] public float MaxFallSpeed { get; set; } = 1_200.0f;
    [Export] public float JumpVelocity { get; set; } = 750.0f;
    [Export(PropertyHint.Range, "1.0,6.0,0.25")]
    public float JumpReleaseGravityMultiplier { get; set; } = 3.0f;

    [ExportGroup("Jump Count")]
    [Export(PropertyHint.Range, "1,4,1")]
    public int MaxJumps { get; set; } = 2;

    [ExportGroup("Forgiveness")]
    [Export(PropertyHint.Range, "0.01,0.3,0.01")]
    public float CoyoteTimeSeconds { get; set; } = 0.10f;
    [Export(PropertyHint.Range, "0.01,0.3,0.01")]
    public float JumpBufferSeconds { get; set; } = 0.10f;
}
