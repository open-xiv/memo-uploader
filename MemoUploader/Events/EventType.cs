namespace MemoUploader.Events;

public enum EventType
{
    // GENERAL EVENTS
    TerritoryChanged = 0,

    // ACTION EVENTS
    ActionStart     = 1,
    ActionCompleted = 2,

    // COMBATANT EVENTS
    CombatantSpawn        = 3,
    CombatantDeath        = 4,
    CombatantTargetable   = 5,
    CombatantUntargetable = 6,

    // STATUS EVENTS
    StatusApplied = 7,
    StatusRemoved = 8,

    // DUTY EVENTS
    DutyStarted     = 9,
    DutyRecommenced = 10,
    DutyCompleted   = 11,
    DutyWiped       = 12,

    // CONDITION EVENTS
    CombatOptIn  = 13,
    CombatOptOut = 14
}
