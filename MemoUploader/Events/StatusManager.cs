using System;
using MemoEngine;
using MemoEngine.Models;


namespace MemoUploader.Events;

public class StatusManager
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
        if (Context.Lifecycle is EngineState.Idle)
            return;

        try { Event.Status.RaiseApplied(DateTimeOffset.UtcNow, player.EntityID, statusId); }
        catch
        {
            // ignored
        }
    }

    private void OnStatusRemovedHook(IBattleChara player, ushort statusId, ushort param, ushort stackCount, ulong sourceId)
    {
        if (Context.Lifecycle is EngineState.Idle)
            return;

        try { Event.Status.RaiseRemoved(DateTimeOffset.UtcNow, player.EntityID, statusId); }
        catch
        {
            // ignored
        }
    }
}
