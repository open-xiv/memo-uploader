using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Plugin.Services;
using MemoEngine;
using MemoEngine.Models;


namespace MemoUploader.Events;

/// <summary>
///     Polls ObjectTable each frame and feeds the engine three streams it needs to evaluate
///     phase predicates passively:
///       - BattleNpc lifecycle (Spawned / Destroyed) keyed by DataId
///       - BattleNpc targetable transitions
///       - party member death detection (was previously the only concern of this class)
///
///     HP polling lives in HpManager — kept separate to run at finer cadence.
/// </summary>
public class CombatantManager
{
    // dataId → isTargetable (true if any entity with this dataId is currently targetable)
    private readonly Dictionary<uint, bool> lastSeen = [];

    // entityId → isAlive (party only)
    private readonly Dictionary<uint, bool> lastAlive = [];

    public void Init() => FrameworkManager.Instance().Reg(OnFrameworkUpdate, 500);

    public void Uninit()
    {
        FrameworkManager.Instance().Unreg(OnFrameworkUpdate);
        lastSeen.Clear();
        lastAlive.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        // Idle = not in any tracked duty; nothing to feed the engine.
        if (Context.Lifecycle is EngineState.Idle)
            return;

        var now = DateTimeOffset.UtcNow;

        var currentSeen = new Dictionary<uint, bool>();
        foreach (var obj in DService.Instance().ObjectTable)
        {
            if (obj.ObjectKind is not ObjectKind.BattleNpc) continue;
            // multiple entities can share the same dataId (boss adds) —
            // collapse to "is ANY entity with this dataId targetable"
            currentSeen[obj.DataID] = currentSeen.TryGetValue(obj.DataID, out var prev)
                                          ? prev || obj.IsTargetable
                                          : obj.IsTargetable;
        }

        foreach (var dataId in lastSeen.Keys.Where(k => !currentSeen.ContainsKey(k)).ToList())
            Event.Combatant.RaiseDestroyed(now, dataId);

        foreach (var (dataId, isTargetable) in currentSeen)
        {
            if (!lastSeen.TryGetValue(dataId, out var wasTargetable))
            {
                Event.Combatant.RaiseSpawned(now, dataId);
                if (isTargetable) Event.Combatant.RaiseBecameTargetable(now, dataId);
                continue;
            }

            if (wasTargetable == isTargetable) continue;
            if (isTargetable) Event.Combatant.RaiseBecameTargetable(now, dataId);
            else              Event.Combatant.RaiseBecameUntargetable(now, dataId);
        }

        lastSeen.Clear();
        foreach (var kv in currentSeen) lastSeen[kv.Key] = kv.Value;

        var currentAlive = new Dictionary<uint, bool>();
        if (DService.Instance().PartyList.Length >= 1)
            currentAlive = DService.Instance().PartyList.ToDictionary(
                p => p.EntityId,
                p => p.GameObject is not null && !p.GameObject.IsDead && p.CurrentHP > 0);

        foreach (var (entityId, isAlive) in currentAlive)
        {
            if (lastAlive.TryGetValue(entityId, out var wasAlive) && wasAlive && !isAlive)
                Event.General.RaisePlayerDied(now, entityId);
        }

        lastAlive.Clear();
        foreach (var kv in currentAlive) lastAlive[kv.Key] = kv.Value;
    }
}
