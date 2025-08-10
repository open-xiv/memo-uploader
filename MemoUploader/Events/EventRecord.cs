using Lumina.Excel.Sheets;
using Status = Lumina.Excel.Sheets.Status;


namespace MemoUploader.Events;

// GENERAL EVENTS
public record TerritoryChanged(ushort ZoneId) : IEvent
{
    public override string ToString()
    {
        var zoneName = "Unknown";
        if (LuminaGetter.TryGetRow<TerritoryType>(ZoneId, out var zone))
            zoneName = zone.PlaceName.Value.Name.ExtractText();

        return $"TerritoryChanged: {zoneName} ({ZoneId})";
    }
}

// ACTION EVENTS
public record ActionStart(uint EntityId, uint ActionId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        var actionName = "Unknown";
        if (LuminaGetter.TryGetRow<Action>(ActionId, out var action))
            actionName = action.Name.ExtractText();

        return $"ActionStart: {objName} ({EntityId}) - {actionName} ({ActionId})";
    }
}

public record ActionCompleted(uint EntityId, uint ActionId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        var actionName = "Unknown";
        if (LuminaGetter.TryGetRow<Action>(ActionId, out var action))
            actionName = action.Name.ExtractText();

        return $"ActionCompleted: {objName} ({EntityId}) - {actionName} ({ActionId})";
    }
}

// COMBATANT EVENTS
public record CombatantSpawn(uint EntityId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        return $"CombatantSpawn: {objName} ({EntityId})";
    }
}

public record CombatantDeath(uint EntityId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        return $"CombatantDeath: {objName} ({EntityId})";
    }
}

public record CombatantTargetable(uint EntityId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        return $"CombatantTargetable: {objName} ({EntityId})";
    }
}

public record CombatantUntargetable(uint EntityId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        return $"CombatantUntargetable: {objName} ({EntityId})";
    }
}

// STATUS EVENTS
public record StatusApplied(uint EntityId, uint StatusId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        var statusName = "Unknown";
        if (LuminaGetter.TryGetRow<Status>(StatusId, out var status))
            statusName = status.Name.ExtractText();

        return $"StatusApplied: {objName} ({EntityId}) - {statusName} ({StatusId})";
    }
}

public record StatusRemoved(uint EntityId, uint StatusId) : IEvent
{
    public override string ToString()
    {
        var objName = "Unknown";
        if (DService.ObjectTable.SearchByEntityId(EntityId) is { } obj)
            objName = obj.Name.ToString();

        var statusName = "Unknown";
        if (LuminaGetter.TryGetRow<Status>(StatusId, out var status))
            statusName = status.Name.ExtractText();

        return $"StatusRemoved: {objName} ({EntityId}) - {statusName} ({StatusId})";
    }
}

// DUTY EVENTS
public record DutyStarted : IEvent
{
    public override string ToString() => "DutyStarted";
}

public record DutyRecommenced : IEvent
{
    public override string ToString() => "DutyRecommenced";
}

public record DutyCompleted : IEvent
{
    public override string ToString() => "DutyCompleted";
}

public record DutyWiped : IEvent
{
    public override string ToString() => "DutyWiped";
}

// CONDITION EVENTS
public record CombatOptIn : IEvent
{
    public override string ToString() => "CombatOptIn";
}

public record CombatOptOut : IEvent
{
    public override string ToString() => "CombatOptOut";
}
