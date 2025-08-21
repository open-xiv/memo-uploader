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
    // duty config
    private readonly DutyConfig dutyConfig;

    // lifecycle
    public EngineState Lifecycle { get; private set; }

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
    private readonly ListenerManager listenerManager = new();

    // checkpoints
    private readonly ConcurrentBag<string> completedCheckpoints = [];

    #endregion

    #region Windows

    private Phase CurrentPhaseConfig => dutyConfig.Timeline.Phases[Math.Max(phaseIndex, 0)];

    // phase
    public string CurrentPhase => CurrentPhaseConfig.Name;

    public string CurrentSubphase => subphaseIndex >= 0 && subphaseIndex < CurrentPhaseConfig.Checkpoints.Count
                                         ? CurrentPhaseConfig.Checkpoints[subphaseIndex]
                                         : string.Empty;

    // checkpoints
    public List<(string, bool)> Checkpoints => dutyConfig.Timeline.Phases[phaseIndex].Checkpoints
                                                         .Select(name => (name, completedCheckpoints.Contains(name)))
                                                         .ToList();

    // variables
    public IReadOnlyDictionary<string, object?> Variables => variables;

    #endregion

    #region Lifecycle

    public FightContext(DutyConfig dutyConfig)
    {
        // props
        this.dutyConfig = dutyConfig;

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

        // death
        if (e is Death death && players.TryGetValue(death.Object.EntityId, out var player))
            player.DeathCount++;

        // listeners
        var relatedListener = listenerManager.FetchListeners(e);
        foreach (var listener in relatedListener)
        {
            if (CheckTrigger(listener.Trigger, e))
                EmitMechanic(listener.Mechanic);
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
            Phase    = (uint)Math.Max(phaseIndex, 0),
            Subphase = (uint)Math.Max(subphaseIndex, 0),
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

        // clear triggers
        listenerManager.Clear();

        // reset checkpoints
        completedCheckpoints.Clear();

        // mechanics
        // from checkpoints
        var mechanics = new HashSet<string>(phase.Checkpoints);
        // from transitions
        foreach (var transition in phase.Transitions)
        {
            foreach (var condition in transition.Conditions)
            {
                if (condition.Type == "MECHANIC_TRIGGERED")
                    mechanics.Add(condition.MechanicName);
            }
        }

        // register listeners
        foreach (var mechanic in dutyConfig.Mechanics.Where(m => mechanics.Contains(m.Name)))
            listenerManager.Register(new ListenerState(mechanic, mechanic.Trigger));

        // enemy
        enemyId = phase.TargetId;
    }

    private void EmitMechanic(Mechanic mechanic)
    {
        completedCheckpoints.Add(mechanic.Name);

        // update progress
        var phase            = dutyConfig.Timeline.Phases[phaseIndex];
        var newSubphaseIndex = phase.Checkpoints.IndexOf(mechanic.Name);
        if (newSubphaseIndex >= subphaseIndex)
            subphaseIndex = newSubphaseIndex;

        // emit event
        foreach (var action in mechanic.Actions)
            EmitAction(action);

        // check transition
        CheckTransition(mechanic);
    }

    private void EmitAction(Action action)
    {
        // update variables
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

        // check transition
        CheckTransition(action.Name);
    }

    private void CheckTransition(Mechanic mechanic)
    {
        var phase = dutyConfig.Timeline.Phases[phaseIndex];
        foreach (var transition in phase.Transitions)
        {
            if (transition.Conditions
                          .Where(x => x.Type == "MECHANIC_TRIGGERED")
                          .Any(x => x.MechanicName == mechanic.Name)
               )
            {
                EnterPhase(dutyConfig.Timeline.Phases.IndexOf(x => x.Name == transition.TargetPhase));
                return;
            }
        }
    }

    private void CheckTransition(string variable)
    {
        var phase = dutyConfig.Timeline.Phases[phaseIndex];
        foreach (var transition in phase.Transitions)
        {
            if (transition.Conditions
                          .Where(x => x.Type == "EXPRESSION")
                          .Any(x => x.Expression.Contains(variable) && CheckExpression(x.Expression))
               )
            {
                EnterPhase(dutyConfig.Timeline.Phases.IndexOf(x => x.Name == transition.TargetPhase));
                return;
            }
        }
    }

    /// <summary>
    ///     Check if a trigger condition is met.
    /// </summary>
    /// <param name="trigger"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    private bool CheckTrigger(Trigger trigger, IEvent? e = null)
    {
        switch (trigger.Type)
        {
            case "ACTION_EVENT":
                if (e is IActionEvent actionEvent)
                    return actionEvent.Match(trigger);
                return false;
            case "COMBATANT_EVENT":
                if (e is ICombatantEvent combatantEvent)
                    return combatantEvent.Match(trigger);
                return false;
            case "STATUS_EVENT":
                if (e is IStatusEvent statusEvent)
                    return statusEvent.Match(trigger);
                return false;
            default:
                return false;
        }
    }

    private bool CheckExpression(string expression)
    {
        if (string.IsNullOrWhiteSpace(expression))
            return false;

        var parts = expression.Split([' '], StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        var variablePath    = parts[0];
        var op              = parts[1];
        var literalValueStr = parts[2];

        if (!variablePath.StartsWith("variables."))
            return false;
        var variableName = variablePath["variables.".Length..];

        if (!variables.TryGetValue(variableName, out var currentValueObj))
            return false;

        try
        {
            var currentValue = Convert.ToDouble(currentValueObj);
            var targetValue  = Convert.ToDouble(literalValueStr);

            switch (op)
            {
                case "==":
                    return Math.Abs(currentValue - targetValue) < 0.05;
                case "!=":
                    return Math.Abs(currentValue - targetValue) > 0.05;
                case ">":
                    return currentValue > targetValue;
                case ">=":
                    return currentValue >= targetValue;
                case "<":
                    return currentValue < targetValue;
                case "<=":
                    return currentValue <= targetValue;
                default:
                    return false;
            }
        }
        catch (Exception) { return false; }
    }

    #endregion
}
