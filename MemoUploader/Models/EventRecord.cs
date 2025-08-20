using Lumina.Excel.Sheets;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using LuminaAction = Lumina.Excel.Sheets.Action;


namespace MemoUploader.Models;

// GENERAL EVENTS
public record TerritoryChanged(ushort ZoneId) : IEvent
{
    public override string ToString()
    {
        var zoneName = "Unknown";
        if (LuminaGetter.TryGetRow<TerritoryType>(ZoneId, out var zone))
            zoneName = zone.PlaceName.Value.Name.ExtractText();

        return $"{zoneName} ({ZoneId})";
    }
}

#region ActionEvents

// ACTION EVENTS
public interface IActionEvent : IEvent
{
    IGameObject Object   { get; }
    uint        ActionId { get; }

    string Status => this switch
    {
        ActionStart _ => "START",
        ActionCompleted _ => "COMPLETE",
        _ => "UNKNOWN"
    };

    bool Match(Trigger trigger)
    {
        if (trigger.Type != "ACTION_EVENT")
            return false;
        var actionMatch = trigger.ActionId.HasValue && trigger.ActionId.Value == ActionId;
        var statusMatch = trigger.Status == Status;
        return actionMatch && statusMatch;
    }

    string IEvent.FormatMessage()
    {
        var actionName = LuminaGetter.TryGetRow<LuminaAction>(ActionId, out var action)
                             ? action.Name.ExtractText()
                             : "Unknown";

        return $"{Object.Name.ExtractText()} ({Object.DataId}) - {actionName} ({ActionId})";
    }
}

public record ActionStart(IGameObject Object, uint ActionId) : IActionEvent { }

public record ActionCompleted(IGameObject Object, uint ActionId) : IActionEvent { }

#endregion

#region CombatantEvents

// COMBATANT EVENTS
public interface ICombatantEvent : IEvent
{
    IGameObject Object { get; }

    string Status => this switch
    {
        CombatantSpawn _ => "SPAWN",
        CombatantDestroy _ => "DESTROY",
        CombatantTargetable _ => "TARGETABLE",
        CombatantUntargetable _ => "UNTARGETABLE",
        _ => "UNKNOWN"
    };

    bool Match(Trigger trigger)
    {
        if (trigger.Type != "COMBATANT_EVENT")
            return false;
        var combatantMatch = trigger.NpcId.HasValue && trigger.NpcId.Value == Object.DataId;
        var statusMatch    = trigger.Condition == Status;
        return combatantMatch && statusMatch;
    }

    string IEvent.FormatMessage() => $"{Object.Name.ExtractText()} ({Object.DataId}) - {Status}";
}

public record CombatantSpawn(IGameObject Object) : ICombatantEvent { }

public record CombatantDestroy(IGameObject Object) : ICombatantEvent { }

public record CombatantTargetable(IGameObject Object) : ICombatantEvent { }

public record CombatantUntargetable(IGameObject Object) : ICombatantEvent { }

#endregion

#region StatusEvents

// STATUS EVENTS
public interface IStatusEvent : IEvent
{
    uint EntityId { get; }
    uint StatusId { get; }

    string Status => this switch
    {
        StatusApplied _ => "APPLIED",
        StatusRemoved _ => "REMOVED",
        _ => "UNKNOWN"
    };

    bool Match(Trigger trigger)
    {
        if (trigger.Type != "STATUS_EVENT")
            return false;
        var statusMatch = trigger.StatusId.HasValue && trigger.StatusId.Value == StatusId;
        var entityMatch = trigger.NpcId.HasValue && trigger.NpcId.Value == EntityId;
        return statusMatch && entityMatch;
    }

    string IEvent.FormatMessage()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        var statusName = "Unknown";
        if (LuminaGetter.TryGetRow<LuminaStatus>(StatusId, out var status))
            statusName = status.Name.ExtractText();

        return $"{objName} ({EntityId}) - {statusName} ({StatusId})";
    }
}

public record StatusApplied(uint EntityId, uint StatusId) : IStatusEvent { }

public record StatusRemoved(uint EntityId, uint StatusId) : IStatusEvent { }

#endregion

#region DutyEvents

// DUTY EVENTS
public interface IDutyEvent : IEvent
{
    string IEvent.FormatMessage()
    {
        return this switch
        {
            DutyStarted _ => "DutyStarted",
            DutyRecommenced _ => "DutyRecommenced",
            DutyCompleted _ => "DutyCompleted",
            DutyWiped _ => "DutyWiped",
            _ => "UnknownDutyEvent"
        };
    }
}

public record DutyStarted : IDutyEvent { }

public record DutyRecommenced : IDutyEvent { }

public record DutyCompleted : IDutyEvent { }

public record DutyWiped : IDutyEvent { }

#endregion

#region ConditionEvents

// CONDITION EVENTS
public interface IConditionEvent : IEvent
{
    string IEvent.FormatMessage()
    {
        return this switch
        {
            CombatOptIn _ => "CombatOptIn",
            CombatOptOut _ => "CombatOptOut",
            _ => "UnknownConditionEvent"
        };
    }
}

public record CombatOptIn : IConditionEvent { }

public record CombatOptOut : IConditionEvent { }

#endregion

#region FightEvents

// FIGHT EVENTS
public record Death(IGameObject Object) : IEvent
{
    string IEvent.FormatMessage() => $"{Object.Name.ExtractText()} ({Object.EntityId})";
}

#endregion
