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

// ACTION EVENTS
public record ActionStart(IGameObject Object, uint ActionId) : IEvent
{
    public override string ToString()
    {
        var actionName = LuminaGetter.TryGetRow<LuminaAction>(ActionId, out var action)
                             ? action.Name.ExtractText()
                             : "Unknown";

        return $"{Object.Name.ExtractText()} ({Object.DataId}) - {actionName} ({ActionId})";
    }
}

public record ActionCompleted(IGameObject Object, uint ActionId) : IEvent
{
    public override string ToString()
    {
        var actionName = LuminaGetter.TryGetRow<LuminaAction>(ActionId, out var action)
                             ? action.Name.ExtractText()
                             : "Unknown";

        return $"{Object.Name.ExtractText()} ({Object.DataId}) - {actionName} ({ActionId})";
    }
}

// COMBATANT EVENTS
public record CombatantSpawn(IGameObject Object) : IEvent
{
    public override string ToString() => $"{Object.Name.ExtractText()} ({Object.DataId})";
}

public record CombatantDestroy(IGameObject Object) : IEvent
{
    public override string ToString() => $"{Object.Name.ExtractText()} ({Object.DataId})";
}

public record CombatantTargetable(IGameObject Object) : IEvent
{
    public override string ToString() => $"{Object.Name.ExtractText()} ({Object.DataId})";
}

public record CombatantUntargetable(IGameObject Object) : IEvent
{
    public override string ToString() => $"{Object.Name.ExtractText()} ({Object.DataId})";
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
        if (LuminaGetter.TryGetRow<LuminaStatus>(StatusId, out var status))
            statusName = status.Name.ExtractText();

        return $"{objName} ({EntityId}) - {statusName} ({StatusId})";
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
        if (LuminaGetter.TryGetRow<LuminaStatus>(StatusId, out var status))
            statusName = status.Name.ExtractText();

        return $"{objName} ({EntityId}) - {statusName} ({StatusId})";
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

// FIGHT EVENTS
public record Death(IGameObject Object) : IEvent
{
    public override string ToString() => $"{Object.Name.ExtractText()} ({Object.EntityId})";
}

public record HpBelowThreshold(IGameObject Object, double HpPercent) : IEvent
{
    public override string ToString() => $"{Object.Name.ExtractText()} ({Object.EntityId}) - {HpPercent:P2} HP";
}
