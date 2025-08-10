using Dalamud.Hooking;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;


namespace MemoUploader.Events;

public unsafe class ActionManager(Plugin plugin)
{
    private static readonly CompSig                    actionStartSig = new("E8 ?? ?? ?? ?? E9 ?? ?? ?? ?? 48 89 BC 24 D0 00 00 00");
    private static          Hook<ActionStartDelegate>? actionStartHook;

    private static readonly CompSig                       actionCompletedSig = new("E8 ?? ?? ?? ?? 48 8B CF E8 ?? ?? ?? ?? 45 33 C0 48 8D 0D");
    private static          Hook<ActionCompleteDelegate>? actionCompletedHook;

    public void Init()
    {
        actionStartHook ??= actionStartSig.GetHook<ActionStartDelegate>(OnActionStartDetour);
        actionStartHook.Enable();

        actionCompletedHook ??= actionCompletedSig.GetHook<ActionCompleteDelegate>(OnActionCompleteDetour);
        actionCompletedHook.Enable();
    }

    public void Uninit()
    {
        actionStartHook?.Disable();
        actionStartHook?.Dispose();
        actionStartHook = null;

        actionCompletedHook?.Disable();
        actionCompletedHook?.Dispose();
        actionCompletedHook = null;
    }

    private nint OnActionStartDetour(BattleChara* player, ActionType type, uint actionId, nint a4, float rotation, float a6)
    {
        plugin.EventQueue.Post(new ActionStart(player->EntityId, actionId));

        if (actionStartHook is null)
            return nint.Zero;

        var original = actionStartHook.Original(player, type, actionId, a4, rotation, a6);
        return original;
    }

    private nint OnActionCompleteDetour(BattleChara* player, ActionType type, uint actionId, uint spellId, GameObjectId a5, nint a6, float rotation, short a8, int a9, int a10)
    {
        plugin.EventQueue.Post(new ActionCompleted(player->EntityId, actionId));

        if (actionCompletedHook is null)
            return nint.Zero;

        var original = actionCompletedHook.Original(player, type, actionId, spellId, a5, a6, rotation, a8, a9, a10);
        return original;
    }

    private delegate nint ActionStartDelegate(BattleChara* player, ActionType type, uint actionId, nint a4, float rotation, float a6);

    private delegate nint ActionCompleteDelegate(
        BattleChara* player, ActionType type, uint actionId, uint spellId, GameObjectId a5, nint a6, float rotation, short a8, int a9, int a10);
}
