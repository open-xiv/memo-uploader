using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Enums;
using FFXIVClientStructs.FFXIV.Client.Game.UI;


namespace MemoUploader.Tags;

/// <summary>
///     Snapshots <see cref="ContentsFinder"/> queue context the moment we
///     enter a duty zone and converts it into the engine's opaque-string
///     tag form. Engine-side stays game-agnostic — this file is the only
///     place in the codebase that knows about FFXIV's queue UI internals.
///
///     Read at TerritoryChanged because that's the most reliable window:
///     queue state is still <c>InContent</c>, RouletteId / popped flags
///     are fresh, and the cached read survives all the way to the
///     subsequent DutyCompleted via <c>FightRecordPayload.Tags</c>
///     passthrough.
/// </summary>
internal static unsafe class RouletteTags
{
    /// <summary>
    /// Returns null if the player isn't in a roulette / popped party
    /// (no tags worth attaching). Engine accepts null and clears
    /// observed tags for the new zone.
    /// </summary>
    public static IReadOnlyList<string>? Build()
    {
        var cf = ContentsFinder.Instance();
        if (cf == null) return null;

        var info = cf->GetQueueInfo();
        if (info == null || info->QueueState != ContentsFinderQueueState.InContent)
            return null;

        var tags = new List<string>();

        // 0 = direct entry (not from a roulette). Skip the roulette tag
        // but the popped-party flags below may still apply (unrestricted
        // / min ilvl / etc are picked at duty finder time regardless of
        // roulette).
        if (info->QueuedContentRouletteId != 0)
            tags.Add("roulette:" + RouletteName(info->QueuedContentRouletteId));

        if (info->PoppedContentIsInProgress)        tags.Add("in-progress");
        if (info->PoppedContentIsUnrestrictedParty) tags.Add("unrestricted-party");
        if (info->PoppedContentIsMinimalIL)         tags.Add("min-ilvl");
        if (info->PoppedContentIsLevelSync)         tags.Add("level-sync");
        if (info->PoppedContentIsSilenceEcho)       tags.Add("echo-off");
        if (info->PoppedContentIsExplorerMode)      tags.Add("explorer");

        return tags.Count > 0 ? tags : null;
    }

    /// <summary>
    /// Maps ContentRoulette excel row id to the lowercased-hyphenated
    /// name memo-server's tag whitelist accepts (see
    /// utils/ContentRoulette mirror in memo-server).
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
