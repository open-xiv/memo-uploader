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
        UseActionManager.RegCharacterStartCast(OnActionStart);
        UseActionManager.RegCharacterCompleteCast(OnActionComplete);
    }

    public void Uninit()
    {
        UseActionManager.Unreg(OnActionStart);
        UseActionManager.Unreg(OnActionComplete);
    }

    private void OnActionStart(nint a1, IBattleChara player, ActionType type, uint actionID, nint a4, float rotation, float a6)
    {
        if (player.ObjectKind is not ObjectKind.BattleNpc || PluginContext.Lifecycle is null)
            return;

        if (DService.ObjectTable.SearchByEntityID(player.EntityID) is { } obj)
            eventRaiser(new ActionStart(obj, actionID));
    }

    private void OnActionComplete(
        nint         a1,
        IBattleChara player,
        ActionType   type,
        uint         actionID,
        uint         spellID,
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

        if (DService.ObjectTable.SearchByEntityID(player.EntityID) is { } obj)
            eventRaiser(new ActionCompleted(obj, actionID));
    }
}
