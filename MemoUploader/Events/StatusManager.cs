using System;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MemoUploader.Models;


namespace MemoUploader.Events;

public unsafe class StatusManager(Action<IEvent> eventRaiser)
{
    public void Init()
    {
        PlayerStatusManager.RegGainStatus(OnStatusAppliedHook);
        PlayerStatusManager.RegLoseStatus(OnStatusRemovedHook);
    }

    public void Uninit()
    {
        PlayerStatusManager.Unreg(OnStatusAppliedHook);
        PlayerStatusManager.Unreg(OnStatusRemovedHook);
    }

    private void OnStatusAppliedHook(
        BattleChara* player,
        ushort       statusID,
        ushort       param,
        ushort       stackCount,
        TimeSpan     remainingTime,
        ulong        sourceID)
    {
        if (PluginContext.Lifecycle is null)
            return;

        try { eventRaiser(new StatusApplied(player->EntityId, statusID)); }
        catch
        {
            // ignored
        }
    }

    private void OnStatusRemovedHook(BattleChara* player, ushort statusID, ushort param, ushort stackCount, ulong sourceID)
    {
        if (PluginContext.Lifecycle is null)
            return;

        try { eventRaiser(new StatusRemoved(player->EntityId, statusID)); }
        catch
        {
            // ignored
        }
    }
}
