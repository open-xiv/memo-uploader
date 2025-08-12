using System;
using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using MemoUploader.Models;


namespace MemoUploader.Events;

public unsafe class StatusManager(Action<IEvent> eventRaiser)
{
    private static readonly CompSig                      statusAppliedSig = new("E8 ?? ?? ?? ?? 80 BC 24 C0 00 00 00 00");
    private static          Hook<StatusAppliedDelegate>? statusAppliedHook;

    private static readonly CompSig                      statusRemovedSig = new("40 53 55 41 55 41 57 48 83 EC ?? 48 83 39");
    private static          Hook<StatusRemovedDelegate>? statusRemovedHook;

    public void Init()
    {
        statusAppliedHook ??= statusAppliedSig.GetHook<StatusAppliedDelegate>(OnStatusAppliedDetour);
        statusAppliedHook.Enable();

        statusRemovedHook ??= statusRemovedSig.GetHook<StatusRemovedDelegate>(OnStatusRemovedDetour);
        statusRemovedHook.Enable();
    }

    public void Uninit()
    {
        statusAppliedHook?.Dispose();
        statusAppliedHook = null;

        statusRemovedHook?.Dispose();
        statusRemovedHook = null;
    }

    private void OnStatusAppliedDetour(BattleChara** player, ushort statusId, float remainingTime, ushort statusParam, ulong sourceId, ushort stackCount)
    {
        statusAppliedHook?.Original(player, statusId, remainingTime, statusParam, sourceId, stackCount);
        if (statusId == 0)
            return;

        try { eventRaiser(new StatusApplied((*player)->EntityId, statusId)); }
        catch
        {
            // ignored
        }
    }

    private void OnStatusRemovedDetour(BattleChara** player, ushort statusId, ushort statusParam, ulong sourceId, ushort stackCount)
    {
        statusRemovedHook?.Original(player, statusId, statusParam, sourceId, stackCount);
        if (statusId == 0)
            return;

        try { eventRaiser(new StatusRemoved((*player)->EntityId, statusId)); }
        catch
        {
            // ignored
        }
    }

    private delegate void StatusAppliedDelegate(BattleChara** player, ushort statusId, float remainingTime, ushort statusParam, ulong sourceId, ushort stackCount);

    private delegate void StatusRemovedDelegate(BattleChara** player, ushort statusId, ushort statusParam, ulong sourceId, ushort stackCount);
}
