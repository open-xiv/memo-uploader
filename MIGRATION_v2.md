# Engine 2.0 迁移 — Windows 端待验证清单

工作机是 macOS，跑得起 dotnet restore（NuGet 解析正确）但跑不动 Dalamud SDK 完整 build（依赖 Windows-only 的 `Microsoft.WindowsDesktop.App` framework reference + 真实 Dalamud 安装）。

这份清单是落到 Windows + 真实 Dalamud + FFXIV 客户端环境继续验证的事项。

## Engine 2.0 对 Dalamud 端的影响

`MemoEngine` NuGet 包从 1.0.7 升到 **2.0.0**（已发布到 NuGet.org）。

- **DSL 重写**：`timeline / checkpoints / transitions / subphase` 全删；phase 用 `predicate`（TARGETABLE / SEEN / VAR / STATUS_ON / HP_RATIO + AND/OR/NOT）描述。Active phase = 声明顺序里最后一个谓词成立的。
- **Trigger 多态**：`ACTION_START / ACTION_COMPLETE / STATUS_APPLIED / STATUS_REMOVED`，`*_ids` 是数组，LOGICAL_OPERATOR 删除。
- **Action 重命名**：`INCREMENT_VARIABLE / SET_VARIABLE` → `INCREMENT / SET`，字段 `name` → `variable`。
- **Payload 改版**：`FightProgressPayload` 删 `subphase` 加 `phase_name`。服务端 schema 同步迁移中。
- **新增引擎被动 WorldModel**：引擎现在持续维护 TargetableEnemies / ActiveStatuses / HpRatios，不再因 Lifecycle != Recording 丢弃 Combatant 事件。CombatOptIn 时谓词直接基于 WorldModel 求值，**不需要新的 snapshot API**。
- **行为修复**：`e.TimeStamp` 替换 `DateTime.UtcNow`；TerritoryChanged 同步清状态再 fetch；INCREMENT 内部用 long；FindIndex 异常包 try/catch；EventRecorder 改惰性快照；ActionBlock 加 `BoundedCapacity = 4096`。
- **API 删除**：`IGeneralSink.RaisePartyChanged` 及其事件类彻底删除（双端都没有 caller）。

## Dalamud 端代码变化

### `MemoUploader.csproj`
- `Version` 7.4.7.3 → 7.5.0.0（engine 2.0 协议破坏）
- `<PackageReference MemoEngine>` 1.0.7 → **2.0.0**

### `Events/CombatantManager.cs` — **本次主要代码改动**
旧版 500ms 框架回调里同时干了两件事：
1. 给 `Context.EnemyDataId` 推 HP 更新（**与 HpManager 重复**，是个 bug）
2. 通过 PartyList 检测玩家死亡

新版做了三件事：
1. **删掉 HP 推送**（HpManager 200ms 是 HP 唯一来源）
2. **保留死亡检测**（PartyList diff）
3. **新增 BattleNpc 生命周期 diff**：每帧扫 `ObjectTable.Where(x => x.ObjectKind is BattleNpc)`，按 `DataId` 聚合"任一实体可选中"，与上一帧 diff 后调
   - `Event.Combatant.RaiseSpawned(dataId)` —— 第一次见
   - `Event.Combatant.RaiseDestroyed(dataId)` —— 上一帧见、这一帧消失
   - `Event.Combatant.RaiseBecameTargetable / Untargetable(dataId)` —— 可选中状态翻转

引擎的 `WorldModel` 通过这些事件持续更新；CombatOptIn 触发时谓词可直接读到当前世界状态决定起始 phase（自动恢复 / 中途加入场景）。

### 其他文件未改
- `Plugin.cs / Configuration.cs / Api/ApiClient.cs / Windows/MainWindow.cs / Events/EventManager.cs / Events/ActionManager.cs / Events/StatusManager.cs / Events/HpManager.cs` —— **零改动**
- 公共 API（`Context.Lifecycle / Context.EnemyDataId / Context.OnFightFinalized / Event.X.Raise*`）签名未变

## Windows 上的验证清单

打勾后可以删掉这个文件。

### 编译 / 打包
- [ ] `dotnet build MemoUploader/MemoUploader.csproj` 在 Windows + Dalamud 安装好的环境下成功
- [ ] CI/release pipeline 跑过

### 运行时基本性
- [ ] 在 Dalamud 里加载插件，命令 `/memo` 能开窗口
- [ ] 进入有副本配置的区域：`Context.Lifecycle` 切到 `WaitingStart`
- [ ] 进入无配置区域：`Context.Lifecycle` 保持 `Idle`，`Context.EnemyDataId` 为 0

### CombatantManager diff 逻辑（新代码，重点）
- [ ] 进入副本区域时，BattleNpc spawn 触发 `Event.Combatant.RaiseSpawned`（看引擎 EventHistory）
- [ ] Boss 变可选中时触发 `RaiseBecameTargetable`
- [ ] Boss 战中变不可选中（cutscene / 转阶段）触发 `RaiseBecameUntargetable`
- [ ] Boss 死亡 / despawn 触发 `RaiseDestroyed`
- [ ] 验证 boss 切换场景（m12s 门神 → 本体）：旧 dataId Untargetable + 新 dataId Targetable 序列正确

### 事件流端到端
- [ ] 开战 CombatOptIn → `Lifecycle = Recording`，`Context.EnemyDataId` 自动填上首个 phase 的 TARGETABLE 值（这是新行为，v1 是手动设的）
- [ ] HP 推送速率正常（200ms），CombatantManager 不再额外推 HP
- [ ] 触发 phase 推进的 mechanic（动作 / 状态）：active phase 推进
- [ ] 团灭 / 通关：`OnFightFinalized` 触发，payload 含 `phase_name`、不含 `subphase`

### 服务端联调（依赖 server v2 上线）
- [ ] payload 字段 `progress.phase_name` / `progress.enemy_hp` 被服务端正确接受
- [ ] X-Client-Name / X-Client-Version header 行为正确

### 老 yaml 兼容性
- [ ] 进 m9s/m10s/m11s/m12s/top/uwu/e1n 任意一个，引擎都能成功 fetch 新 yaml 并构造 PredicateEngine（不抛 JsonSerializationException）

## 已知留的坑

1. **STATUS_APPLIED.target_data_id 字段未实现**：YAML schema 支持，但 PredicateEngine 当前忽略（entityId → dataId 映射没建）。当前 yaml 没用到。
2. **Spawned 时如果实体已经 IsTargetable，会同时 emit Spawned + BecameTargetable**：这是 catch-up 行为（处理"插件加载/进入区域时 boss 已经可选"的边角），引擎 idempotent 接受，没问题。
3. **CombatantManager 500ms 频率对 Targetable 切换的延迟**：phase 转场反馈最大 500ms 延迟。如果觉得太慢，可以降到 200ms（与 HpManager 合并），代价是每帧扫 ObjectTable 的开销翻倍。
