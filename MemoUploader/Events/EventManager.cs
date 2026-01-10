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
    private HpManager?        hpService;

    // event
    public event Action<IEvent>? OnEvent;

    public void Init()
    {
        // GENERAL EVENTS
        DService.Instance().ClientState.TerritoryChanged += OnTerritoryChanged;

        // ACTION EVENTS
        actionService = new ActionManager(RaiseEvent);
        actionService.Init();

        // COMBATANT EVENTS
        combatantService = new CombatantManager(RaiseEvent);
        combatantService.Init();

        // STATUS EVENTS
        statusService = new StatusManager(RaiseEvent);
        statusService.Init();

        // ENEMY HP EVENTS
        hpService = new HpManager(RaiseEvent);
        hpService.Init();

        // DUTY EVENTS
        DService.Instance().DutyState.DutyStarted     += OnDutyStarted;
        DService.Instance().DutyState.DutyRecommenced += OnDutyRecommenced;
        DService.Instance().DutyState.DutyCompleted   += OnDutyCompleted;
        DService.Instance().DutyState.DutyWiped       += OnDutyWiped;

        // CONDITION EVENTS
        DService.Instance().Condition.ConditionChange += OnConditionChange;
    }

    public void Uninit()
    {
        // GENERAL EVENTS
        DService.Instance().ClientState.TerritoryChanged -= OnTerritoryChanged;

        // ACTION EVENTS
        actionService?.Uninit();

        // COMBATANT EVENTS
        combatantService?.Uninit();

        // STATUS EVENTS
        statusService?.Uninit();

        // ENEMY HP EVENTS
        hpService?.Uninit();

        // DUTY EVENTS
        DService.Instance().DutyState.DutyStarted     -= OnDutyStarted;
        DService.Instance().DutyState.DutyRecommenced -= OnDutyRecommenced;
        DService.Instance().DutyState.DutyCompleted   -= OnDutyCompleted;
        DService.Instance().DutyState.DutyWiped       -= OnDutyWiped;

        // CONDITION EVENTS
        DService.Instance().Condition.ConditionChange -= OnConditionChange;
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
