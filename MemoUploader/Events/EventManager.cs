using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using MemoEngine;
using MemoEngine.Models;


namespace MemoUploader.Events;

public class EventManager
{
    // service
    private ActionManager?    actionService;
    private CombatantManager? combatantService;
    private StatusManager?    statusService;
    private HpManager?        hpService;

    public void Init()
    {
        // GENERAL EVENTS
        DService.Instance().ClientState.TerritoryChanged += OnTerritoryChanged;

        // ACTION EVENTS
        actionService = new ActionManager();
        actionService.Init();

        // COMBATANT EVENTS
        combatantService = new CombatantManager();
        combatantService.Init();

        // STATUS EVENTS
        statusService = new StatusManager();
        statusService.Init();

        // ENEMY HP EVENTS
        hpService = new HpManager();
        hpService.Init();

        // DUTY EVENTS
        DService.Instance().DutyState.DutyCompleted += OnDutyCompleted;
        DService.Instance().DutyState.DutyWiped     += OnDutyWiped;

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
        DService.Instance().DutyState.DutyCompleted -= OnDutyCompleted;
        DService.Instance().DutyState.DutyWiped     -= OnDutyWiped;

        // CONDITION EVENTS
        DService.Instance().Condition.ConditionChange -= OnConditionChange;
    }

    private void OnTerritoryChanged(ushort zoneId) =>
        Event.General.RaiseTerritoryChanged(DateTimeOffset.UtcNow, zoneId);

    private void OnDutyCompleted(object? sender, ushort e) =>
        Event.General.RaiseDutyCompleted(DateTimeOffset.UtcNow);

    private void OnDutyWiped(object? sender, ushort e) =>
        Event.General.RaiseDutyWiped(DateTimeOffset.UtcNow);

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag is not ConditionFlag.InCombat)
            return;

        if (value)
            Event.General.RaiseCombatOptIn(DateTimeOffset.UtcNow, GetPartySnapshots());
        else
            Event.General.RaiseCombatOptOut(DateTimeOffset.UtcNow);
    }

    private static Dictionary<uint, PlayerPayload> GetPartySnapshots()
    {
        if (DService.Instance().PartyList.Length >= 1)
        {
            return DService.Instance().PartyList.ToDictionary(p => p.EntityId,
                                                              p => new PlayerPayload
                                                              {
                                                                  Name       = p.Name.ToString(),
                                                                  Server     = p.World.Value.Name.ToString(),
                                                                  JobId      = p.ClassJob.RowId,
                                                                  Level      = p.Level,
                                                                  DeathCount = 0
                                                              });
        }

        if (DService.Instance().ObjectTable.LocalPlayer is { } local)
        {
            return new Dictionary<uint, PlayerPayload>
            {
                {
                    local.EntityID,
                    new PlayerPayload
                    {
                        Name       = local.Name.ToString(),
                        Server     = local.HomeWorld.Value.Name.ToString(),
                        JobId      = local.ClassJob.RowId,
                        Level      = local.Level,
                        DeathCount = 0
                    }
                }
            };
        }

        return new Dictionary<uint, PlayerPayload>();
    }
}
