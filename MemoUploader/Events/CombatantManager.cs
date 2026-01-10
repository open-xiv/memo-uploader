using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using MemoUploader.Models;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace MemoUploader.Events;

public class CombatantManager(Action<IEvent> eventRaiser)
{
    private readonly List<IGameObject>                lastCombatants = [];
    private readonly Dictionary<uint, CombatantState> lastStates     = [];

    private record CombatantState(bool IsTargetable, bool IsDead);

    public void Init() => FrameworkManager.Instance().Reg(OnFrameworkUpdate, throttleMS: 200);

    public void Uninit()
    {
        FrameworkManager.Instance().Unreg(OnFrameworkUpdate);
        lastCombatants.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (PluginContext.Lifecycle is null)
            return;

        // combatants
        var currentCombatants = DService.Instance().ObjectTable.Where(x => x.ObjectKind is ObjectKind.BattleNpc).ToList();

        // spawn
        foreach (var obj in currentCombatants.Except(lastCombatants))
            eventRaiser(new CombatantSpawned(obj.DataID));

        // destroy
        foreach (var obj in lastCombatants.Except(currentCombatants))
            eventRaiser(new CombatantDestroyed(obj.DataID));

        // update combatants
        lastCombatants.Clear();
        lastCombatants.AddRange(currentCombatants);

        // last states
        var currentStates = DService.Instance().ObjectTable
                                    .Where(x => x.ObjectKind is ObjectKind.Player or ObjectKind.BattleNpc)
                                    .DistinctBy(x => x.EntityID)
                                    .ToDictionary(x => x.EntityID, x => x);

        // status changes
        foreach (var (entityId, currentState) in currentStates)
        {
            if (!lastStates.TryGetValue(entityId, out var lastState))
                continue;

            // targetable and untargetable
            switch (lastState.IsTargetable)
            {
                case true when !currentState.IsTargetable:
                    eventRaiser(new CombatantBecameUntargetable(currentState.DataID));
                    break;
                case false when currentState.IsTargetable:
                    eventRaiser(new CombatantBecameTargetable(currentState.DataID));
                    break;
            }

            // death
            if (currentState.ObjectKind is not ObjectKind.Player || lastState.IsDead || !currentState.IsDead)
                continue;

            eventRaiser(new PlayerDied(currentState.EntityID));
        }

        // update states
        lastStates.Clear();
        foreach (var obj in currentStates.Values)
            lastStates[obj.EntityID] = new CombatantState(obj.IsTargetable, obj.IsDead);
    }
}
