using System;
using Dalamud.Game.ClientState.Conditions;
using MemoUploader.Models;


namespace MemoUploader.Events;

public class EventManager
{
    // service
    private ActionManager?    actionService;
    private CombatantManager? combatantService;
    private StatusManager?    statusService;

    // event
    public event Action<IEvent>? OnEvent;

    public void Init()
    {
        // GENERAL EVENTS
        DService.ClientState.TerritoryChanged += OnTerritoryChanged;

        // ACTION EVENTS
        actionService = new ActionManager(RaiseEvent);
        actionService.Init();

        // COMBATANT EVENTS
        combatantService = new CombatantManager(RaiseEvent);
        combatantService.Init();

        // STATUS EVENTS
        statusService = new StatusManager(RaiseEvent);
        statusService.Init();

        // DUTY EVENTS
        DService.DutyState.DutyStarted     += OnDutyStarted;
        DService.DutyState.DutyRecommenced += OnDutyRecommenced;
        DService.DutyState.DutyCompleted   += OnDutyCompleted;
        DService.DutyState.DutyWiped       += OnDutyWiped;

        // CONDITION EVENTS
        DService.Condition.ConditionChange += OnConditionChange;
    }

    public void Uninit()
    {
        // GENERAL EVENTS
        DService.ClientState.TerritoryChanged -= OnTerritoryChanged;

        // ACTION EVENTS
        actionService?.Uninit();

        // COMBATANT EVENTS
        combatantService?.Uninit();

        // STATUS EVENTS
        statusService?.Uninit();

        // DUTY EVENTS
        DService.DutyState.DutyStarted     -= OnDutyStarted;
        DService.DutyState.DutyRecommenced -= OnDutyRecommenced;
        DService.DutyState.DutyCompleted   -= OnDutyCompleted;
        DService.DutyState.DutyWiped       -= OnDutyWiped;

        // CONDITION EVENTS
        DService.Condition.ConditionChange -= OnConditionChange;
    }

    private void RaiseEvent(IEvent e) =>
        OnEvent?.Invoke(e);

    private void OnTerritoryChanged(ushort zoneId) =>
        RaiseEvent(new TerritoryChanged(zoneId));

    private void OnDutyStarted(object? sender, ushort e) =>
        RaiseEvent(new DutyStarted());

    private void OnDutyRecommenced(object? sender, ushort e) =>
        RaiseEvent(new DutyRecommenced());

    private void OnDutyCompleted(object? sender, ushort e) =>
        RaiseEvent(new DutyCompleted());

    private void OnDutyWiped(object? sender, ushort e) =>
        RaiseEvent(new DutyWiped());

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag is not ConditionFlag.InCombat)
            return;

        RaiseEvent(value ? new CombatOptIn() : new CombatOptOut());
    }
}
