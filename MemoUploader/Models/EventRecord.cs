using System;
using Lumina.Excel.Sheets;
using LuminaStatus = Lumina.Excel.Sheets.Status;
using LuminaAction = Lumina.Excel.Sheets.Action;


namespace MemoUploader.Models;

public interface IEvent
{
    string Category => GetType().Name;
    string Message  => FormatMessage();

    string FormatMessage() => ToString() ?? string.Empty;
}

public record EventLog(DateTime Time, string Category, string Message);

// GENERAL EVENTS
public record TerritoryChanged(ushort zoneID) : IEvent
{
    public override string ToString()
    {
        var zoneName = "Unknown";
        if (LuminaGetter.TryGetRow<TerritoryType>(zoneID, out var zone))
            zoneName = zone.PlaceName.Value.Name.ToString();

        return $"{zoneName} ({zoneID})";
    }
}

#region ActionEvents

// ACTION EVENTS
public interface IActionEvent : IEvent
{
    IGameObject Object   { get; }
    uint        ActionID { get; }

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
        var actionMatch = trigger.ActionID.HasValue && trigger.ActionID.Value == ActionID;
        var statusMatch = trigger.Status == Status;
        return actionMatch && statusMatch;
    }

    string IEvent.FormatMessage()
    {
        var actionName = LuminaGetter.TryGetRow<LuminaAction>(ActionID, out var action)
                             ? action.Name.ToString()
                             : "Unknown";

        return $"{Object.Name} ({Object.DataID}) - {actionName} ({ActionID})";
    }
}

public record ActionStart(IGameObject     Object, uint ActionID) : IActionEvent { }
public record ActionCompleted(IGameObject Object, uint ActionID) : IActionEvent { }

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
        var combatantMatch = trigger.NPCID.HasValue && trigger.NPCID.Value == Object.DataID;
        var statusMatch    = trigger.Status == Status;
        return combatantMatch && statusMatch;
    }

    string IEvent.FormatMessage() => $"{Object.Name} ({Object.DataID}) - {Status}";
}

public record CombatantSpawn(IGameObject        Object) : ICombatantEvent { }
public record CombatantDestroy(IGameObject      Object) : ICombatantEvent { }
public record CombatantTargetable(IGameObject   Object) : ICombatantEvent { }
public record CombatantUntargetable(IGameObject Object) : ICombatantEvent { }

#endregion

#region StatusEvents

// STATUS EVENTS
public interface IStatusEvent : IEvent
{
    uint EntityID { get; }
    uint StatusID { get; }

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
        var staMatch    = trigger.StatusID.HasValue && trigger.StatusID.Value == StatusID;
        var statusMatch = trigger.Status == Status;
        return staMatch && statusMatch;
    }

    string IEvent.FormatMessage()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityID(EntityID) is { } obj)
            objName = obj.Name.ToString();

        var statusName = "Unknown";
        if (LuminaGetter.TryGetRow<LuminaStatus>(StatusID, out var status))
            statusName = status.Name.ToString();

        return $"{objName} ({EntityID}) - {statusName} ({StatusID})";
    }
}

public record StatusApplied(uint EntityID, uint StatusID) : IStatusEvent { }
public record StatusRemoved(uint EntityID, uint StatusID) : IStatusEvent { }

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
    string IEvent.FormatMessage() => $"{Object.Name} ({Object.EntityID})";
}

#endregion
