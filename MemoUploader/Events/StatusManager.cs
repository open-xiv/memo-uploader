using System;
using MemoUploader.Models;


namespace MemoUploader.Events;

public class StatusManager(Action<IEvent> eventRaiser)
{
    public void Init()
    {
        CharacterStatusManager.Instance().RegGain(OnStatusAppliedHook);
        CharacterStatusManager.Instance().RegLose(OnStatusRemovedHook);
    }

    public void Uninit()
    {
        CharacterStatusManager.Instance().Unreg(OnStatusAppliedHook);
        CharacterStatusManager.Instance().Unreg(OnStatusRemovedHook);
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
