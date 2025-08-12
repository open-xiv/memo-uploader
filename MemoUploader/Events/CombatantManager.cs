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

    public void Init() => DService.Framework.Update += OnFrameworkUpdate;

    public void Uninit()
    {
        DService.Framework.Update -= OnFrameworkUpdate;
        lastCombatants.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Throttler.Throttle("sumemo-combatant-update", 200))
            return;

        // combatants
        var currentCombatants = DService.ObjectTable.Where(x => x.ObjectKind is ObjectKind.BattleNpc).ToList();

        // spawn
        foreach (var obj in currentCombatants.Except(lastCombatants))
            eventRaiser(new CombatantSpawn(obj));

        // destroy
        foreach (var obj in lastCombatants.Except(currentCombatants))
            eventRaiser(new CombatantDestroy(obj));

        // update combatants
        lastCombatants.Clear();
        lastCombatants.AddRange(currentCombatants);

        // last states
        var currentStates = DService.ObjectTable
                                    .Where(x => x.ObjectKind is ObjectKind.Player or ObjectKind.BattleNpc)
                                    .DistinctBy(x => x.EntityId)
                                    .ToDictionary(x => x.EntityId, x => x);

        // status changes
        foreach (var (entityId, currentState) in currentStates)
        {
            if (lastStates.TryGetValue(entityId, out var lastState))
            {
                // targetable and untargetable
                switch (lastState.IsTargetable)
                {
                    case true when !currentState.IsTargetable:
                        eventRaiser(new CombatantUntargetable(currentState));
                        break;
                    case false when currentState.IsTargetable:
                        eventRaiser(new CombatantTargetable(currentState));
                        break;
                }

                // death
                if (currentState.ObjectKind is ObjectKind.Player && !lastState.IsDead && currentState.IsDead)
                    eventRaiser(new Death(currentState));
            }
        }

        // update states
        lastStates.Clear();
        foreach (var obj in currentStates.Values)
            lastStates[obj.EntityId] = new CombatantState(obj.IsTargetable, obj.IsDead);
    }
}
