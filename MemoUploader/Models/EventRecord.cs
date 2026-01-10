using System;
using Lumina.Excel.Sheets;


namespace MemoUploader.Models;

public interface IEvent
{
    string Category => GetType().Name;
    string Message  => FormatMessage();

    string FormatMessage() => ToString() ?? string.Empty;
}

public record EventLog(DateTime Time, string Category, string Message);

// GENERAL EVENTS
public record TerritoryChanged(ushort ZoneId) : IEvent
{
    public override string ToString()
    {
        var zoneName = "Unknown";
        if (LuminaGetter.TryGetRow<TerritoryType>(ZoneId, out var zone))
            zoneName = zone.PlaceName.Value.Name.ToString();

        return $"{zoneName} ({ZoneId})";
    }
}

#region ActionEvents

// ACTION EVENTS
public interface IActionEvent : IEvent
{
    uint DataId   { get; }
    uint ActionId { get; }

    string Status => this switch
    {
        ActionStarted _ => "START",
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

    string IEvent.FormatMessage() => $"{DataId} - {ActionId}";
}

public record ActionStarted(uint   DataId, uint ActionId) : IActionEvent { }
public record ActionCompleted(uint DataId, uint ActionId) : IActionEvent { }

#endregion

#region CombatantEvents

// COMBATANT EVENTS
public interface ICombatantEvent : IEvent
{
    uint DataId { get; }

    string Status => this switch
    {
        CombatantSpawned _ => "SPAWN",
        CombatantDestroyed _ => "DESTROY",
        CombatantBecameTargetable _ => "TARGETABLE",
        CombatantBecameUntargetable _ => "UNTARGETABLE",
        _ => "UNKNOWN"
    };

    bool Match(Trigger trigger)
    {
        if (trigger.Type != "COMBATANT_EVENT")
            return false;
        var combatantMatch = trigger.NpcId.HasValue && trigger.NpcId.Value == DataId;
        var statusMatch    = trigger.Status == Status;
        return combatantMatch && statusMatch;
    }

    string IEvent.FormatMessage() => $"{DataId}) - {Status}";
}

public record CombatantSpawned(uint            DataId) : ICombatantEvent { }
public record CombatantDestroyed(uint          DataId) : ICombatantEvent { }
public record CombatantBecameTargetable(uint   DataId) : ICombatantEvent { }
public record CombatantBecameUntargetable(uint DataId) : ICombatantEvent { }

#endregion

#region EnemyStateEvents

public record EnemyHpChanged(uint DataId, double? CurrentHp, double? MaxHp) : IEvent
{
    string IEvent.FormatMessage() => $"{DataId} - HP: {CurrentHp}/{MaxHp}";
}

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
        var staMatch    = trigger.StatusId.HasValue && trigger.StatusId.Value == StatusId;
        var statusMatch = trigger.Status == Status;
        return staMatch && statusMatch;
    }

    string IEvent.FormatMessage()
        => $"{EntityId} - {StatusId}";
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
public record PlayerDied(uint EntityId) : IEvent
{
    string IEvent.FormatMessage() => $"{EntityId}";
}

#endregion
