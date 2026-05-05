using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.DutyState;
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

    private void OnTerritoryChanged(uint zoneId)
    {
        // Pass observed roulette / popped-party tags along with the
        // territory event so the engine can bake them into the eventual
        // FightRecordPayload. The engine caches and forwards them to the
        // next DutyCompleted/Wiped, then resets on the next TerritoryChanged.
        var tags = Tags.RouletteTags.Build();
        Plugin.Log.Info($"[Zone] change: id={zoneId} tags=[{(tags is null ? "" : string.Join(",", tags))}]");
        Event.General.RaiseTerritoryChanged(DateTimeOffset.UtcNow, (ushort)zoneId, tags);
    }

    private void OnDutyCompleted(IDutyStateEventArgs args)
    {
        Plugin.Log.Info("[Duty] completed");
        Event.General.RaiseDutyCompleted(DateTimeOffset.UtcNow);
    }

    private void OnDutyWiped(IDutyStateEventArgs args)
    {
        Plugin.Log.Info("[Duty] wiped");
        Event.General.RaiseDutyWiped(DateTimeOffset.UtcNow);
    }

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag is not ConditionFlag.InCombat)
            return;

        if (value)
        {
            var partySnapshots = GetPartySnapshots();
            Plugin.Log.Info($"[Combat] start: members={partySnapshots.Count}");
            foreach (var kv in partySnapshots)
                Plugin.Log.Info($"[Party] member: name={kv.Value.Name} server={kv.Value.Server} id={kv.Key} job={kv.Value.JobId}");
            Event.General.RaiseCombatOptIn(DateTimeOffset.UtcNow, partySnapshots);
        }
        else
        {
            Plugin.Log.Info("[Combat] end");
            Event.General.RaiseCombatOptOut(DateTimeOffset.UtcNow);
        }
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
