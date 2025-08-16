using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Plugin.Services;
using MemoUploader.Api;
using MemoUploader.Models;
using Action = MemoUploader.Models.Action;


namespace MemoUploader.Engine;

public class FightContext
{
    // event props
    public Action<IEvent> eventRaiser;

    // duty config
    public DutyConfig dutyConfig;

    // lifecycle
    public EngineState Lifecycle { get; private set; } = EngineState.Ready;

    #region Payload

    // time
    private DateTime  startTime;
    private DateTime? lastCombatOptOutTime;

    // progress
    private bool isClear;
    private int  phaseIndex;
    private int  subphaseIndex; // checkpoint index

    // enemy
    private uint   enemyId;
    private double enemyHp;

    #endregion

    #region DutyState

    // players
    private ConcurrentDictionary<uint, PlayerPayload> players = [];

    // variables
    private readonly ConcurrentDictionary<string, object?> variables = [];

    // listener
    private readonly Listener listener = new();

    // trigger
    private readonly ConcurrentDictionary<Trigger, object> triggerMap = [];

    // checkpoints
    private readonly ConcurrentBag<string> completedCheckpoints = [];

    // hp tracking
    private readonly ConcurrentDictionary<uint, double> hpTracking = [];

    #endregion

    #region Lifecycle

    public FightContext(Action<IEvent> eventRaiser, DutyConfig dutyConfig)
    {
        // props
        this.eventRaiser = eventRaiser;
        this.dutyConfig  = dutyConfig;

        // lifecycle
        Lifecycle = EngineState.Ready;

        // variables
        foreach (var vars in dutyConfig.Variables)
            variables[vars.Name] = vars.Initial;
    }

    public void Init() => DService.Framework.Update += OnFrameworkUpdate;

    public void Uninit() => DService.Framework.Update -= OnFrameworkUpdate;

    #endregion

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

    #region EventProcess

    public void ProcessEvent(IEvent e)
    {
        if (Lifecycle is EngineState.Completed)
            return;

        // lifecycle related events
        LifecycleEvent(e);

        if (Lifecycle is not EngineState.InProgress)
            return;

        var relatedTiggers = listener.FetchTrigger(e);
        foreach (var trigger in relatedTiggers)
        {
            if (CheckTrigger(trigger, e))
            {
                if (triggerMap.TryGetValue(trigger, out var owner))
                {
                    switch (owner)
                    {
                        case Mechanic mechanic:
                            EmitMechanic(mechanic);
                            break;
                        case Transition:
                            CheckTransition();
                            break;
                    }
                }
            }
        }
    }

    public void LifecycleEvent(IEvent e)
    {
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

    #endregion

    #region Snapshot

    private void StartSnap()
    {
        // time
        startTime = DateTime.UtcNow;

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

        // progress
        phaseIndex    = 0;
        subphaseIndex = -1;

        // start phase
        EnterPhase(0);
    }

    public void CompletedSnap()
    {
        // time
        var endTime  = lastCombatOptOutTime ?? DateTime.UtcNow;
        var duration = (endTime - startTime).Ticks * 100;

        // progress
        var progress = new FightProgressPayload
        {
            Phase    = (uint)Math.Min(phaseIndex, 0),
            Subphase = (uint)Math.Min(subphaseIndex, 0),
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

    #endregion

    #region StateMachine

    private void EnterPhase(int phaseId)
    {
        // phase transition
        var phase = dutyConfig.Timeline.Phases[phaseId];
        phaseIndex    = phaseId;
        subphaseIndex = -1;

        // clear outdated contexts
        listener.Clear();
        triggerMap.Clear();
        completedCheckpoints.Clear();

        // transition related mechanics
        var mechanics = new HashSet<string>(phase.Checkpoints);
        foreach (var transition in phase.Transitions)
        {
            foreach (var condition in transition.Conditions)
            {
                if (condition.Type == "MECHANIC_TRIGGERED")
                    mechanics.Add(condition.MechanicName);
            }
        }

        // normal mechanics
        foreach (var mechanic in dutyConfig.Mechanics.Where(m => mechanics.Contains(m.Name)))
        {
            listener.Register(mechanic.Trigger);
            triggerMap[mechanic.Trigger] = mechanic;
        }

        // non-mechanics triggers
        foreach (var transition in phase.Transitions)
        {
            foreach (var condition in transition.Conditions)
            {
                if (condition.Type != "MECHANIC_TRIGGERED")
                {
                    listener.Register(condition);
                    triggerMap[condition] = transition;
                }
            }
        }

        // enemy
        enemyId = phase.TargetId;

        DService.Log.Info($"Entered phase: {phase.Name}. Registered {listener.Count} listeners.");
    }

    private void EmitMechanic(Mechanic mechanic)
    {
        DService.Log.Info($"Emit mechanic: {mechanic.Name} ({mechanic.NameEn})");
        completedCheckpoints.Add(mechanic.Name);

        // update progress
        var phase            = dutyConfig.Timeline.Phases[phaseIndex];
        var newSubphaseIndex = phase.Checkpoints.IndexOf(mechanic.Name);
        if (newSubphaseIndex >= subphaseIndex)
            subphaseIndex = newSubphaseIndex;

        // emit event
        foreach (var action in mechanic.Actions)
            EmitAction(action);

        // check for transition
        CheckTransition();
    }

    private void EmitAction(Action action)
    {
        DService.Log.Info($"Emit action: {action.Type} for {action.Name} with value {action.Value}");
        switch (action.Type)
        {
            case "INCREMENT_VARIABLE":
                if (variables.TryGetValue(action.Name, out var val) && val is long or int)
                    variables[action.Name] = Convert.ToInt64(val) + 1;
                break;
            case "SET_VARIABLE":
                variables[action.Name] = action.Value;
                break;
        }
    }

    private void CheckTransition()
    {
        var phase = dutyConfig.Timeline.Phases[phaseIndex];
        foreach (var transition in phase.Transitions)
        {
            if (CheckCondition(transition.Conditions))
            {
                EnterPhase(dutyConfig.Timeline.Phases.IndexOf(x => x.Name == transition.TargetPhase));
                return;
            }
        }
    }

    private bool CheckCondition(List<Trigger> conditions)
    {
        foreach (var condition in conditions)
        {
            if (!CheckTrigger(condition))
                return false;
        }
        return conditions.Count != 0;
    }

    private bool CheckTrigger(Trigger trigger, IEvent? e = null)
    {
        return trigger.Type switch
        {
            "HP_THRESHOLD" => enemyHp <= trigger.Value,
            "MECHANIC_TRIGGERED" => completedCheckpoints.Contains(trigger.MechanicName),
            "EXPRESSION" =>
                // TODO: Implement expression evaluation
                false,
            _ => false
        };
    }

    #endregion
}
