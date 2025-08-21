using System.Collections.Generic;
using MemoUploader.Models;


namespace MemoUploader.Engine;

public record ListenerState(Mechanic Mechanic, Trigger Trigger);

public class ListenerManager
{
    private readonly Dictionary<string, Dictionary<uint, List<ListenerState>>> listeners = [];

    /// <summary>
    ///     Clears all registered listeners.
    /// </summary>
    public void Clear()
        => listeners.Clear();

    /// <summary>
    ///     Registers a trigger for a specific event type.
    /// </summary>
    /// <param name="state"></param>
    public void Register(ListenerState state)
    {
        if (state.Trigger is { Type: "LOGICAL_OPERATOR", Conditions.Count: > 0 })
        {
            foreach (var condition in state.Trigger.Conditions)
                Register(new ListenerState(state.Mechanic, condition));
            return;
        }

        var type = state.Trigger.Type;
        uint id = type switch
        {
            "ACTION_EVENT" => state.Trigger.ActionId ?? 0,
            "COMBATANT_EVENT" => state.Trigger.NpcId ?? 0,
            "STATUS_EVENT" => state.Trigger.StatusId ?? 0,
            _ => 0
        };

        // create type listeners if needed
        if (!listeners.ContainsKey(type))
            listeners[type] = new Dictionary<uint, List<ListenerState>>();

        // create id listeners if needed
        var typeListeners = listeners[type];
        if (!typeListeners.ContainsKey(id))
            typeListeners[id] = [];

        typeListeners[id].Add(new ListenerState(state.Mechanic, state.Trigger));
    }

    public IEnumerable<ListenerState> FetchListeners(IEvent e)
    {
        var type = e switch
        {
            IActionEvent => "ACTION_EVENT",
            ICombatantEvent => "COMBATANT_EVENT",
            IStatusEvent => "STATUS_EVENT",
            _ => "UNKNOWN"
        };
        uint id = e switch
        {
            IActionEvent actionEvent => actionEvent.ActionId,
            ICombatantEvent combatantEvent => combatantEvent.Object.DataId,
            IStatusEvent statusEvent => statusEvent.StatusId,
            _ => 0
        };

        if (listeners.TryGetValue(type, out var typeListeners) && typeListeners.TryGetValue(id, out var list))
            return list;
        return [];
    }

    /// <summary>
    ///     Gets the total count of all registered listeners.
    /// </summary>
    public int Count => listeners.Count;
}
