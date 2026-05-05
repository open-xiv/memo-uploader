using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.UI;


namespace MemoUploader.Tags;

/// <summary>
///     Snapshots <see cref="ContentsFinder"/> queue context the moment we
///     enter a duty zone and converts it into the engine's opaque-string
///     tag form. Engine-side stays game-agnostic — this file is the only
///     place in the codebase that knows about FFXIV's queue UI internals.
///
///     Leak-prevention strategy mirrors DailyRoutines'
///     DungeonLoggerUploader (Assist/DungeonLoggerUploader.cs:111): we
///     don't gate on <c>QueueState</c> at all — instead we gate on whether
///     the destination zone itself is a duty zone, by checking
///     <c>GameMain.CurrentContentFinderConditionId</c> (zero in towns,
///     wilderness, housing, etc.). This is the cleanest discriminator
///     because:
///
///       - Player queueing for a roulette and walking around town:
///         <c>QueuedContentRouletteId</c> is set but CFC is 0 → no tag.
///       - Player accepting a pop and entering the duty: CFC flips to
///         non-zero on the destination → tag attaches.
///       - Multi-zone duties (Crystal Tower etc.): every sub-zone has
///         CFC != 0 → tag re-emitted on each TerritoryChanged so the
///         engine's Recorder keeps it across zone resets.
///       - Exit back to town: destination CFC is 0 → no tag.
///
///     QueueState was the previous gate but proved fragile — it's only
///     <c>Accepted</c> for an instant during loading and the timing
///     relative to the TerritoryChanged event isn't guaranteed.
/// </summary>
internal static unsafe class RouletteTags
{
    public static IReadOnlyList<string>? Build()
    {
        // Mirrors DungeonLoggerUploader.OnZoneChanged:111 — destination
        // must be a duty zone (CFC != 0) before we even peek at the queue.
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

    /// <summary>
    /// Maps ContentRoulette excel row id to the lowercased-hyphenated
    /// name memo-server's tag whitelist accepts (see
    /// memo-server/service/fight_validate.go:clientTagRegistry).
    /// </summary>
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
        // unknown id — server's whitelist will 400 it. Surfaces as a
        // visible failure rather than silent misclassification.
        _  => "id-" + id,
    };
}
