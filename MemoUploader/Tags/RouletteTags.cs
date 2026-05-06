using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.UI;


namespace MemoUploader.Tags;

/// <summary>
///     Snapshots <see cref="ContentsFinder"/> queue context at duty entry
///     and converts it into the engine's opaque-string tag form. Engine
///     stays game-agnostic — this is the only place that knows about
///     FFXIV's queue UI internals.
///
///     Gate is destination-zone CFC, not <c>QueueState</c>: only emit
///     when <c>GameMain.CurrentContentFinderConditionId != 0</c> so
///     queue-while-traveling between non-duty zones doesn't leak the
///     roulette tag. Mirrors DungeonLoggerUploader.OnZoneChanged:111.
/// </summary>
internal static unsafe class RouletteTags
{
    public static IReadOnlyList<string>? Build()
    {
        var cfc = GameState.ContentFinderCondition;
        if (cfc == 0)
        {
            Plugin.Log.Info("[Roulette] consume: empty reason=cfc-zero");
            return null;
        }

        var cf = ContentsFinder.Instance();
        if (cf == null)
        {
            Plugin.Log.Info($"[Roulette] consume: empty reason=contents-finder-null cfc={cfc}");
            return null;
        }

        var info = cf->GetQueueInfo();
        if (info == null)
        {
            Plugin.Log.Info($"[Roulette] consume: empty reason=queue-info-null cfc={cfc}");
            return null;
        }

        var rid   = info->QueuedContentRouletteId;
        var state = info->QueueState;

        if (rid == 0)
        {
            Plugin.Log.Info($"[Roulette] consume: empty reason=direct-entry cfc={cfc} state={state}");
            return null;
        }

        var tag = "roulette:" + RouletteName(rid);
        Plugin.Log.Info($"[Roulette] consume: tag={tag} cfc={cfc} id={rid} state={state}");
        return new List<string> { tag };
    }

    // Mirrors memo-server/service/fight_validate.go:clientTagRegistry.
    private static string RouletteName(byte id) => id switch
    {
        1  => "leveling",
        2  => "high-level",
        3  => "msq",
        4  => "guildhests",
        5  => "expert",
        6  => "trials",
        7  => "frontline",
        8  => "level-cap",
        9  => "mentor",
        15 => "alliance",
        17 => "normal-raids",
        _  => "id-" + id,  // server whitelist will 400 — surfaces as visible failure
    };
}
