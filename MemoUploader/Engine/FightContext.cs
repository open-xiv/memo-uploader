using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using MemoUploader.Api;
using MemoUploader.Models;


namespace MemoUploader.Engine;

public class FightContext(Action<IEvent> eventRaiser, DutyConfig dutyConfig)
{
    // lifecycle
    public EngineState Lifecycle { get; private set; } = EngineState.Ready;

    // time
    private DateTime  startTime;
    private DateTime? lastCombatOptOutTime;

    // progress
    private bool isClear;
    private uint phaseIndex;
    private uint subphaseIndex; // checkpoint index

    // enemy
    private uint   enemyId;
    private double enemyHp;

    // players
    private ConcurrentDictionary<uint, PlayerPayload> players = [];

    // variables
    private readonly ConcurrentDictionary<string, object> variables = [];

    // hp tracking
    private readonly ConcurrentDictionary<uint, double> hpTracking = [];

    public void Init() => DService.Framework.Update += OnFrameworkUpdate;

    public void Uninit() => DService.Framework.Update -= OnFrameworkUpdate;

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Throttler.Throttle("sumemo-fight-context-update", 200))
            return;

        // main enemy hp
        if (DService.ObjectTable.FirstOrDefault(x => x.DataId == enemyId) is IBattleChara enemy)
            enemyHp = (double)enemy.CurrentHp / enemy.MaxHp;

        // hp tracking
        var revoke = new List<uint>();
        foreach (var sub in hpTracking)
        {
            if (DService.ObjectTable.FirstOrDefault(x => x.DataId == sub.Key) is IBattleChara obj)
            {
                var percent = (double)obj.CurrentHp / obj.MaxHp;
                if (percent < sub.Value)
                {
                    eventRaiser(new HpBelowThreshold(obj, sub.Value));
                    revoke.Add(sub.Key);
                }
            }
        }
        foreach (var id in revoke)
            hpTracking.TryRemove(id, out _);
    }

    public void ProcessEvent(IEvent e)
    {
        if (Lifecycle is EngineState.Completed)
            return;

        switch (e)
        {
            case CombatOptIn when Lifecycle is EngineState.Ready:
                Lifecycle = EngineState.InProgress;
                StartSnap();
                break;

            case CombatOptOut:
                lastCombatOptOutTime = DateTime.UtcNow;
                break;

            case DutyWiped:
                Lifecycle = EngineState.Completed;
                isClear   = false;
                CompletedSnap();
                return;

            case DutyCompleted:
                Lifecycle = EngineState.Completed;
                isClear   = true;
                CompletedSnap();
                return;

            // dev: regard first cast as combat opt-in when playback
            case ActionCompleted when Lifecycle is EngineState.Ready && DService.Condition[ConditionFlag.DutyRecorderPlayback]:
                Lifecycle = EngineState.InProgress;
                StartSnap();
                break;

            // dev: regard last cast as combat opt-out when playback
            case ActionCompleted when Lifecycle is EngineState.InProgress && DService.Condition[ConditionFlag.DutyRecorderPlayback]:
                lastCombatOptOutTime = DateTime.UtcNow;
                break;
        }
    }

    private void StartSnap()
    {
        // time
        startTime = DateTime.UtcNow;

        // progress
        phaseIndex    = 0;
        subphaseIndex = 0;

        // players
        players.Clear();
        players = DService.PartyList.ToConcurrentDictionary(
            p => p.ObjectId,
            p => new PlayerPayload
            {
                Name       = p.Name.ExtractText(),
                Server     = p.World.Value.Name.ExtractText(),
                JobId      = p.ClassJob.RowId,
                Level      = p.Level,
                DeathCount = 0
            }
        );
    }

    public void CompletedSnap()
    {
        // time
        var endTime  = lastCombatOptOutTime ?? DateTime.UtcNow;
        var duration = (endTime - startTime).Ticks * 100;

        // progress
        var progress = new FightProgressPayload
        {
            Phase    = phaseIndex,
            Subphase = subphaseIndex,
            EnemyId  = enemyId,
            EnemyHp  = enemyHp
        };

        // payload
        var payload = new FightRecordPayload
        {
            StartTime = startTime,
            Duration  = duration,
            ZoneId    = dutyConfig.ZoneId,
            Players   = players.Values.ToList(),
            IsClear   = isClear,
            Progress  = progress
        };

        // upload
        _ = Task.Run(async () => await ApiClient.UploadFightRecordAsync(payload));
    }
}
