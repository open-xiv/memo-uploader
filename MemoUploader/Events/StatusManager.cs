using System;
using MemoUploader.Models;


namespace MemoUploader.Events;

public class StatusManager(Action<IEvent> eventRaiser)
{
    public void Init()
    {
        PlayerStatusManager.Instance().RegGain(OnStatusAppliedHook);
        PlayerStatusManager.Instance().RegLose(OnStatusRemovedHook);
    }

    public void Uninit()
    {
        PlayerStatusManager.Instance().Unreg(OnStatusAppliedHook);
        PlayerStatusManager.Instance().Unreg(OnStatusRemovedHook);
    }

    private void OnStatusAppliedHook
    (
        IBattleChara player,
        ushort       statusId,
        ushort       param,
        ushort       stackCount,
        TimeSpan     remainingTime,
        ulong        sourceId
    )
    {
        if (PluginContext.Lifecycle is null)
            return;

        try { eventRaiser(new StatusApplied(player.EntityID, statusId)); }
        catch
        {
            // ignored
        }
    }

    private void OnStatusRemovedHook(IBattleChara player, ushort statusId, ushort param, ushort stackCount, ulong sourceId)
    {
        if (PluginContext.Lifecycle is null)
            return;

        try { eventRaiser(new StatusRemoved(player.EntityID, statusId)); }
        catch
        {
            // ignored
        }
    }
}
