using Godot;
using Godot.Collections;

/// <summary>
/// Phase 3：在 _Ready 中用纯代码构建 TileSet（含物理层）并程序化绘制地形。
/// 替换旧 World/StaticBody2D 测试平台。
/// </summary>
public partial class LevelBuilder : TileMapLayer
{
    // ── 常量 ────────────────────────────────────────────────────────────────
    private const int TileSize        = 64;   // px
    private const int SourceId        = 0;
    private static readonly Vector2I AtlasCoords = Vector2I.Zero;

    // ── 瓦片颜色（用 CanvasTexture 渲染纯色图块）─────────────────────────
    private static readonly Color GroundColor   = new(0.16f, 0.33f, 0.27f);
    private static readonly Color PlatformColor = new(0.92f, 0.48f, 0.22f);

    // ── 地图格子布局（以 tile 为单位，原点为瓦片地图左上角）──────────────
    // Ground row：y=10 (y=640px)，x 0..19（1280px 宽）
    // Platform L：y=8 (y=512px)，x 0..3
    // Platform C：y=6 (y=384px)，x 8..11
    // Platform R：y=7 (y=448px)，x 15..18
    private static readonly Vector2I[] GroundTiles    = BuildRow(y: 10, xStart: 0, count: 20);
    private static readonly Vector2I[] PlatformLeft   = BuildRow(y:  8, xStart: 1, count:  4);
    private static readonly Vector2I[] PlatformCenter = BuildRow(y:  6, xStart: 8, count:  4);
    private static readonly Vector2I[] PlatformRight  = BuildRow(y:  7, xStart: 15, count: 4);

    public override void _Ready()
    {
        // 1. 构建一个带物理层的 TileSet（全程序化，不依赖图集图片）
        var tileSet = BuildTileSet();
        TileSet = tileSet;

        // 2. 绘制地形
        PlaceTiles(GroundTiles);
        PlaceTiles(PlatformLeft);
        PlaceTiles(PlatformCenter);
        PlaceTiles(PlatformRight);
    }

    // ── TileSet 构建 ────────────────────────────────────────────────────────

    private static TileSet BuildTileSet()
    {
        var ts = new TileSet();
        ts.TileSize = new Vector2I(TileSize, TileSize);

        // 添加物理层（layer 0）：与 World（1）和 Player（2）均碰撞
        ts.AddPhysicsLayer();
        ts.SetPhysicsLayerCollisionLayer(0, 1);   // layer bit 0
        ts.SetPhysicsLayerCollisionMask(0, 2);    // mask bit 1（Player 在 layer 2）

        // 创建 TileSetAtlasSource（单张 1×1 像素纯白纹理即可；图块颜色通过 modulate 区分）
        var source = new TileSetAtlasSource();
        source.TextureRegionSize = new Vector2I(TileSize, TileSize);

        // 创建一张 64×64 白色 Image 作为图集
        var img = Image.CreateEmpty(TileSize, TileSize, false, Image.Format.Rgba8);
        img.Fill(Colors.White);
        source.Texture = ImageTexture.CreateFromImage(img);

        // 创建唯一的 Atlas tile (0,0)
        source.CreateTile(Vector2I.Zero);

        // 为这个图块添加物理碰撞多边形（整格正方形）
        var tileData = source.GetTileData(Vector2I.Zero, 0);
        var polygon = new Vector2[]
        {
            new(-TileSize / 2f, -TileSize / 2f),
            new( TileSize / 2f, -TileSize / 2f),
            new( TileSize / 2f,  TileSize / 2f),
            new(-TileSize / 2f,  TileSize / 2f),
        };
        tileData.AddCollisionPolygon(0);
        tileData.SetCollisionPolygonPoints(0, 0, polygon);

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
