using System.Collections.Generic;
using System.Linq;
using MemoUploader.Models;


namespace MemoUploader.Engine;

public class Listener
{
    // base events
    private readonly Dictionary<uint, List<Trigger>> actionListeners    = [];
    private readonly Dictionary<uint, List<Trigger>> combatantListeners = [];
    private readonly Dictionary<uint, List<Trigger>> statusListeners    = [];

    // stateful events
    private readonly List<Trigger> statefulTriggers = [];

    /// <summary>
    ///     Clears all registered listeners.
    /// </summary>
    public void Clear()
    {
        actionListeners.Clear();
        combatantListeners.Clear();
        statusListeners.Clear();
        statefulTriggers.Clear();
    }

    /// <summary>
    ///     Registers a trigger for a specific event type.
    /// </summary>
    /// <param name="trigger"></param>
    public void Register(Trigger trigger)
    {
        // logical
        if (trigger is { Type: "LOGICAL_OPERATOR", Conditions.Count: > 0 })
        {
            foreach (var condition in trigger.Conditions)
                Register(condition);
            return;
        }

        switch (trigger.Type)
        {
            case "ACTION_EVENT":
                if (trigger.ActionId.HasValue)
                {
                    var id = trigger.ActionId.Value;
                    if (!actionListeners.ContainsKey(id))
                        actionListeners[id] = [];
                    actionListeners[id].Add(trigger);
                }
                break;

            case "COMBATANT_EVENT":
                if (trigger.NpcId.HasValue)
                {
                    var id = trigger.NpcId.Value;
                    if (!combatantListeners.ContainsKey(id))
                        combatantListeners[id] = [];
                    combatantListeners[id].Add(trigger);
                }
                break;

            case "STATUS_EVENT":
                if (trigger.StatusId.HasValue)
                {
                    var id = trigger.StatusId.Value;
                    if (!statusListeners.ContainsKey(id))
                        statusListeners[id] = [];
                    statusListeners[id].Add(trigger);
                }
                break;

            // stateful
            case "HP_THRESHOLD":
            case "EXPRESSION":
            case "TIMEOUT":
            case "MECHANIC_TRIGGERED":
                statefulTriggers.Add(trigger);
                break;
        }
    }

    /// <summary>
    ///     Finds all triggers that match the given event.
    /// </summary>
    /// <param name="gameEvent"></param>
    /// <returns></returns>
    public IEnumerable<Trigger> FetchTrigger(IEvent gameEvent)
    {
        return gameEvent switch
        {
            // ACTION EVENTS
            ActionStart e => actionListeners.TryGetValue(e.ActionId, out var list) ? list : Enumerable.Empty<Trigger>(),
            ActionCompleted e => actionListeners.TryGetValue(e.ActionId, out var list) ? list : Enumerable.Empty<Trigger>(),

            // COMBATANT EVENTS
            CombatantSpawn e => combatantListeners.TryGetValue(e.Object.DataId, out var list) ? list : Enumerable.Empty<Trigger>(),
            CombatantDestroy e => combatantListeners.TryGetValue(e.Object.DataId, out var list) ? list : Enumerable.Empty<Trigger>(),
            CombatantTargetable e => combatantListeners.TryGetValue(e.Object.DataId, out var list) ? list : Enumerable.Empty<Trigger>(),
            CombatantUntargetable e => combatantListeners.TryGetValue(e.Object.DataId, out var list) ? list : Enumerable.Empty<Trigger>(),

            // STATUS EVENTS
            StatusApplied e => statusListeners.TryGetValue(e.StatusId, out var list) ? list : Enumerable.Empty<Trigger>(),
            StatusRemoved e => statusListeners.TryGetValue(e.StatusId, out var list) ? list : Enumerable.Empty<Trigger>(),

            // empty
            _ => []
        };
    }

    /// <summary>
    ///     Finds all stateful triggers that match the given event.
    /// </summary>
    public IEnumerable<Trigger> FetchStatefulTriggers(IEvent gameEvent) => statefulTriggers;

    /// <summary>
    ///     Gets the total count of all registered listeners.
    /// </summary>
    public int Count => actionListeners.Count + combatantListeners.Count + statusListeners.Count + statefulTriggers.Count;
}
