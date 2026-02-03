using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using MemoEngine;
using MemoEngine.Models;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace MemoUploader.Events;

public class ActionManager
{
    public void Init()
    {
        UseActionManager.Instance().RegPostCharacterStartCast(OnActionStart);
        UseActionManager.Instance().RegPostCharacterCompleteCast(OnActionComplete);
    }

    public void Uninit()
    {
        UseActionManager.Instance().Unreg(OnActionStart);
        UseActionManager.Instance().Unreg(OnActionComplete);
    }

    private void OnActionStart(bool result, IBattleChara player, ActionType type, uint actionId, nint a4, float rotation, float a6)
    {
        if (player.ObjectKind is not ObjectKind.BattleNpc || Context.Lifecycle is EngineState.Idle)
            return;

        if (DService.Instance().ObjectTable.SearchByEntityID(player.EntityID) is { } obj)
            Event.Action.RaiseStarted(DateTimeOffset.UtcNow, obj.DataID, actionId);
    }

    private void OnActionComplete
    (
        bool         result,
        IBattleChara player,
        ActionType   type,
        uint         actionId,
        uint         spellId,
        GameObjectId a5,
        Vector3      a6,
        float        rotation,
        short        a8,
        int          a9,
        int          a10
    )
    {
        if (player.ObjectKind is not ObjectKind.BattleNpc || Context.Lifecycle is EngineState.Idle)
            return;

        if (DService.Instance().ObjectTable.SearchByEntityID(player.EntityID) is { } obj)
            Event.Action.RaiseCompleted(DateTimeOffset.UtcNow, obj.DataID, actionId);
    }
}
