using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using MemoEngine;
using MemoEngine.Models;


namespace MemoUploader.Events;

public class CombatantManager
{
    private readonly Dictionary<uint, bool> lastAlive = [];

    public void Init() => FrameworkManager.Instance().Reg(OnFrameworkUpdate, 500);

    public void Uninit()
    {
        FrameworkManager.Instance().Unreg(OnFrameworkUpdate);
        lastAlive.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (Context.Lifecycle is EngineState.Idle)
            return;

        if (Context.EnemyDataId != 0 && DService.Instance().ObjectTable.FirstOrDefault(x => x.DataID == Context.EnemyDataId) is IBattleChara enemy)
            Event.Combatant.RaiseHpUpdated(DateTimeOffset.UtcNow, enemy.DataID, enemy.CurrentHp, enemy.MaxHp);

        var currentAlive = new Dictionary<uint, bool>();
        if (DService.Instance().PartyList.Length >= 1)
            currentAlive = DService.Instance().PartyList.ToDictionary(p => p.EntityId, p => p.GameObject is not null && !p.GameObject.IsDead && p.CurrentHP > 0);

        foreach (var (entityId, isAlive) in currentAlive)
        {
            if (lastAlive.TryGetValue(entityId, out var wasAlive) && wasAlive && !isAlive)
                Event.General.RaisePlayerDied(DateTimeOffset.UtcNow, entityId);
        }

        lastAlive.Clear();
        foreach (var (entityId, isAlive) in currentAlive)
            lastAlive[entityId] = isAlive;
    }
}
