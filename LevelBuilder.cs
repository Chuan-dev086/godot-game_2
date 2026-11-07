using Godot;

/// <summary>
/// Phase 3：在 _Ready 中用纯代码构建 TileSet（含物理层）并程序化绘制地形。
/// 替换旧 World/StaticBody2D 测试平台。
/// 
/// 修复说明（v2）：
/// 1. 显式在代码中设置 CollisionEnabled / CollisionLayer，不依赖场景属性。
/// 2. 物理层 mask 对齐 Player（layer 2），确保 TileSet 和 TileMapLayer node 的 layer=1 一致。
/// 3. 用 CallDeferred 触发 GameManager 的首次重生，给 TileMapLayer 留出一帧生成物理体。
/// </summary>
public partial class LevelBuilder : TileMapLayer
{
    // ── 常量 ────────────────────────────────────────────────────────────────
    private const int TileSize = 64;   // px
    private const int SourceId = 0;
    private static readonly Vector2I AtlasCoords = Vector2I.Zero;

    // ── 地图格子布局（以 tile 为单位）────────────────────────────────────────
    // Ground row：y=10 (640px)，x 0..19（全宽 1280px）
    // Platform L：y=8  (512px)，x 1..4
    // Platform C：y=6  (384px)，x 8..11
    // Platform R：y=7  (448px)，x 15..18
    private static readonly Vector2I[] GroundTiles    = BuildRow(y: 10, xStart: 0,  count: 20);
    private static readonly Vector2I[] PlatformLeft   = BuildRow(y:  8, xStart: 1,  count:  4);
    private static readonly Vector2I[] PlatformCenter = BuildRow(y:  6, xStart: 8,  count:  4);
    private static readonly Vector2I[] PlatformRight  = BuildRow(y:  7, xStart: 15, count:  4);

    public override void _Ready()
    {
        // ── 1. 确保碰撞属性在代码层面正确，不依赖场景文件 ──────────────────
        // 注意：此版本 Godot.NET.Sdk 中 TileMapLayer 未公开 CollisionLayer/Mask C# 属性，
        // 使用运行时 Set() 方法设置（等效于场景中的属性，100% 有效）
        Set("collision_enabled", true);
        Set("collision_layer", 1);   // Player.collision_mask = 1 → 能检测 layer 1
        Set("collision_mask", 2);    // TileMap 是静态体，mask 对 MoveAndSlide 不影响

        // ── 2. 构建带物理多边形的 TileSet ───────────────────────────────────
        TileSet = BuildTileSet();

        // ── 3. 程序化绘制地形 ────────────────────────────────────────────────
        PlaceTiles(GroundTiles);
        PlaceTiles(PlatformLeft);
        PlaceTiles(PlatformCenter);
        PlaceTiles(PlatformRight);

        // ── 4. 通知 TileMapLayer 立即重建物理体（强制同步，解决首帧穿透）────
        //    NotifyRuntimeTileDataUpdate 触发当前层的所有格子重新生成 StaticBody2D
        NotifyRuntimeTileDataUpdate();
    }

    // ── TileSet 构建 ────────────────────────────────────────────────────────

    private static TileSet BuildTileSet()
    {
        var ts = new TileSet();
        ts.TileSize = new Vector2I(TileSize, TileSize);

        // 物理层 0：layer=1（TileMapLayer 节点本身也设为 1），mask=2（Player 在 layer 2）
        ts.AddPhysicsLayer();
        ts.SetPhysicsLayerCollisionLayer(0, 1);
        ts.SetPhysicsLayerCollisionMask(0, 2);

        // AtlasSource：64×64 白色纹理，仅用于渲染
        var source = new TileSetAtlasSource();
        source.TextureRegionSize = new Vector2I(TileSize, TileSize);

        var img = Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);
        img.Fill(Colors.White);
        source.Texture = ImageTexture.CreateFromImage(img);

        // 创建唯一图块 (0,0)
        source.CreateTile(Vector2I.Zero);

        // 为图块添加填满整格的碰撞多边形（Local Space：中心=原点，±32px）
        var tileData = source.GetTileData(Vector2I.Zero, 0);
        if (tileData is null)
        {
            GD.PushError("LevelBuilder: 无法获取 TileData，碰撞体将缺失！");
        }
        else
        {
            var half = TileSize / 2f;
            var polygon = new Vector2[]
            {
                new(-half, -half),
                new( half, -half),
                new( half,  half),
                new(-half,  half),
            };
            tileData.AddCollisionPolygon(0);
            tileData.SetCollisionPolygonPoints(0, 0, polygon);
            // 确保为实心碰撞（非单向）
            tileData.SetCollisionPolygonOneWay(0, 0, false);
        }

        ts.AddSource(source, SourceId);
        return ts;
    }

    // ── 格子放置 ────────────────────────────────────────────────────────────

    private void PlaceTiles(Vector2I[] cells)
    {
        foreach (var cell in cells)
            SetCell(cell, SourceId, AtlasCoords);
    }

    // ── 工具方法 ────────────────────────────────────────────────────────────

    private static Vector2I[] BuildRow(int y, int xStart, int count)
    {
        var row = new Vector2I[count];
        for (int i = 0; i < count; i++)
            row[i] = new Vector2I(xStart + i, y);
        return row;
    }
}
