using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Object;
using MemoUploader.Models;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace MemoUploader.Events;

public class ActionManager(Action<IEvent> eventRaiser)
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
        if (player.ObjectKind is not ObjectKind.BattleNpc || PluginContext.Lifecycle is null)
            return;

        if (DService.Instance().ObjectTable.SearchByEntityID(player.EntityID) is { } obj)
            eventRaiser(new ActionStarted(obj.DataID, actionId));
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
        if (player.ObjectKind is not ObjectKind.BattleNpc || PluginContext.Lifecycle is null)
            return;

        if (DService.Instance().ObjectTable.SearchByEntityID(player.EntityID) is { } obj)
            eventRaiser(new ActionCompleted(obj.DataID, actionId));
    }
}
