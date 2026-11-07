# 2D 平台跳跃小游戏开发计划

## 项目目标与约束

- 使用 Godot 4.x .NET、C# 与 .NET 10；这是纯单机 2D 平台跳跃游戏。
- 玩家物理根节点固定为 `CharacterBody2D`，移动碰撞统一使用 `MoveAndSlide()`。
- 架构采用强类型枚举、接口、状态模式和事件通信，避免将 UI、关卡、输入、物理混合在同一类。

## 场景与节点树

```text
Main (Node2D) [GameManager.cs]
├── Background (ColorRect)
├── World (Node2D)
│   ├── Ground (StaticBody2D)
│   │   ├── CollisionShape2D
│   │   └── ColorRect
│   ├── PlatformLeft (StaticBody2D)
│   ├── PlatformCenter (StaticBody2D)
│   └── PlatformRight (StaticBody2D)
├── PlayerSpawn (Marker2D)
├── Player (Player.tscn instance)
├── Camera2D [CameraController.cs]
└── CanvasLayer/Hud (Control)

Player (CharacterBody2D) [PlayerController.cs]
├── CollisionShape2D
└── Visuals/Body (ColorRect)
```

第一阶段用文本定义的 `StaticBody2D + CollisionShape2D + ColorRect` 搭建测试地形。第三阶段会以 Godot 4.3+ 的 `TileMapLayer` 替换它，并将实体地形、装饰和自定义图块数据分层。

## C# 架构

最终状态机使用：

```text
PlayerController : CharacterBody2D
  └── PlayerStateMachine
        └── IPlayerState
             ├── IdleState
             ├── RunState
             ├── JumpState
             ├── FallState
             └── DeadState
```

`PlayerStateId` 包含 Idle、Run、Jump、Fall、Dead；`GameState` 包含 Playing、Paused、LevelComplete、GameOver。`IPlayerState` 定义状态标识、进入、退出、输入处理、物理更新。状态不直接访问 UI 或关卡。第一阶段先提供可玩的基础控制器；第二阶段抽取完整状态机，同时保持节点职责边界。

## 核心物理设计

`_PhysicsProcess(double delta)` 的固定顺序：读取 Input Map，平滑处理水平加减速，累加重力并限制最大下落速度，处理地面跳跃，然后每帧仅调用一次 `MoveAndSlide()`，最后检查出界并由游戏管理器重生。

第二阶段将增加：跳跃松开后的短跳、coyote time（土狼时间）和 jump buffer（跳跃缓冲）。离地时启动土狼窗口，按跳但暂不可跳时启动缓冲窗口；任一窗口内满足资格即消耗请求、设置负的 `Velocity.Y` 跳跃初速度。

## 关卡与数据设计

`LevelController` 在第三阶段管理出生点、危险区、收集物、终点和 `TileMapLayer`。TileSet Custom Data Layer 预留 `SurfaceType`（Normal、Ice、Bounce、Hazard）与 `IsCheckpoint`，使特殊地面不耦合玩家状态机。

## 开发阶段

### Phase 1：场景骨架与基础可玩移动

生成文本化项目、输入映射、测试平台、基础跑跳重力、出界重生、带边界的平滑相机。验收：角色可稳定跑、跳、落地和重生。

### Phase 2：状态机与高级手感

实现 `IPlayerState`、具体状态、强类型移动配置、短跳、coyote time、jump buffer，以及动画/音效/粒子分离。

### Phase 3：TileMapLayer、目标与 UI

以 `TileMapLayer` 替换测试平台，实现关卡控制器、收集物、危险区、终点、HUD、暂停、通关和重开，并使用 TileSet 自定义数据扩展特殊地面。

## 验收重点

- 所有游戏脚本均为 C#，所有场景和配置均为直接生成的文本文件。
- 玩家继承 `CharacterBody2D`，每物理帧严格只有一次 `MoveAndSlide()`。
- 输入由 `project.godot` 的 Input Map 配置。
- Phase 1 不需要手动拖放或配置任何节点。
