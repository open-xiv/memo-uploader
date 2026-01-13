using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MemoUploader.Api;
using MemoUploader.Models;
using Action = MemoUploader.Models.Action;


namespace MemoUploader.Engine;

public class FightContext
{
    // duty config
    private readonly DutyConfig dutyConfig;

    #region Payload

    // time
    private DateTime  startTime;
    private DateTime? lastCombatOptOutTime;

    // progress
    private bool isClear;
    private int  phaseIndex;
    private int  subphaseIndex; // checkpoint index

    // enemy
    private double enemyHp;

    #endregion

    #region DutyState

    // players
    private ConcurrentDictionary<uint, PlayerPayload> players = [];

    // variables
    private ConcurrentDictionary<string, object?> variables = [];

    // listener
    private readonly ListenerManager listenerManager = new();

    // checkpoints
    private ConcurrentBag<string> completedCheckpoints = [];

    #endregion

    #region Windows

    private void UpdateContext()
    {
        var phase = dutyConfig.Timeline.Phases[Math.Max(phaseIndex, 0)];
        PluginContext.CurrentPhase = phase.Name;
        PluginContext.CurrentSubphase = subphaseIndex >= 0 && subphaseIndex < phase.CheckpointNames.Count
                                            ? phase.CheckpointNames[subphaseIndex]
                                            : string.Empty;
        PluginContext.Checkpoints   = phase.CheckpointNames.Select(name => (name, completedCheckpoints.Contains(name))).ToArray();
        PluginContext.VariableStats = variables;
    }

    #endregion

    #region Lifecycle

    public FightContext(DutyConfig dutyConfig)
    {
        // props
        this.dutyConfig = dutyConfig;

        // reset state
        ResetState();
    }

    #endregion

    #region EventProcess

    public void ProcessEvent(IEvent e)
    {
        // lifecycle related events
        LifecycleEvent(e);

        if (PluginContext.Lifecycle is not EngineState.InProgress)
            return;

        // specific events
        switch (e)
        {
            // death
            case PlayerDied death when players.TryGetValue(death.EntityId, out var player):
                player.DeathCount++;
                break;

            // enemy hp change
            case EnemyHpChanged hpChanged when hpChanged.DataId == PluginContext.EnemyDataId:
            {
                if (hpChanged.CurrentHp is not null && hpChanged.MaxHp is not null)
                    enemyHp = hpChanged.CurrentHp.Value / hpChanged.MaxHp.Value;
                else
                    enemyHp = 0;
                break;
            }
        }

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
            case CombatOptIn:
                if (PluginContext.Lifecycle is EngineState.Completed or EngineState.Ready)
                {
                    ResetState();
                    PluginContext.Lifecycle = EngineState.InProgress;
                    StartSnap();
                }
                break;

            case CombatOptOut:
                lastCombatOptOutTime = DateTime.UtcNow;
                break;

            case DutyWiped:
                PluginContext.Lifecycle = EngineState.Completed;
                isClear                 = false;
                CompletedSnap();
                return;

            case DutyCompleted:
                PluginContext.Lifecycle = EngineState.Completed;
                isClear                 = true;
                CompletedSnap();
                return;

            case DutyRecommenced:
                ResetState();
                PluginContext.Lifecycle = EngineState.Ready;
                break;
        }
    }

    #endregion

    #region Snapshot

    private void StartSnap()
    {
        // time
        startTime            = DateTime.UtcNow;
        lastCombatOptOutTime = null;

        // enemy hp
        enemyHp = 1.0;

        // players
        players = [];
        foreach (var p in PluginContext.PartyProvider.GetPartySnapshots())
        {
            players[p.EntityId] = new PlayerPayload
            {
                Name       = p.Name,
                Server     = p.Server,
                JobId      = p.JobId,
                Level      = p.Level,
                DeathCount = 0
            };
        }
    }

    public void CompletedSnap()
    {
        // time
        var endTime = lastCombatOptOutTime ?? DateTime.UtcNow;
        endTime = endTime > startTime ? endTime : DateTime.UtcNow;
        var duration = (endTime - startTime).Ticks * 100;

        // progress
        var progress = new FightProgressPayload
        {
            PhaseId    = (uint)Math.Max(phaseIndex, 0),
            SubphaseId = (uint)Math.Max(subphaseIndex, 0),
            EnemyId    = PluginContext.EnemyDataId,
            EnemyHp    = enemyHp
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

    private void ResetState()
    {
        // lifecycle
        PluginContext.Lifecycle = EngineState.Ready;
        isClear                 = false;

        // progress
        phaseIndex    = 0;
        subphaseIndex = -1;

        // enemy
        PluginContext.EnemyDataId = 0;

        // clear listeners & checkpoints
        listenerManager.Clear();
        completedCheckpoints = [];

        // variables
        variables = [];
        foreach (var vars in dutyConfig.Variables)
            variables[vars.Name] = vars.Initial;

        // enter start phase
        EnterPhase(0);

        // update context
        UpdateContext();
    }

    private void EnterPhase(int phaseId)
    {
        // phase transition
        var phase = dutyConfig.Timeline.Phases[phaseId];
        phaseIndex    = phaseId;
        subphaseIndex = -1;

        // enemy
        PluginContext.EnemyDataId = phase.TargetId;

        // clear triggers
        listenerManager.Clear();

        // reset checkpoints
        completedCheckpoints = [];

        // mechanics
        // from checkpoints
        var mechanics = new HashSet<string>(phase.CheckpointNames);
        // from transitions
        foreach (var transition in phase.Transitions)
        {
            foreach (var condition in transition.Conditions)
            {
                if (condition.Type != "MECHANIC_TRIGGERED")
                    continue;
                mechanics.Add(condition.MechanicName);
            }
        }

        // register listeners
        foreach (var mechanic in dutyConfig.Mechanics.Where(m => mechanics.Contains(m.Name)))
            listenerManager.Register(new ListenerState(mechanic, mechanic.Trigger));

        // enemy
        PluginContext.EnemyDataId = phase.TargetId;

        // update context (phase change)
        UpdateContext();
    }

    private void EmitMechanic(Mechanic mechanic)
    {
        completedCheckpoints.Add(mechanic.Name);

        // update progress
        var phase            = dutyConfig.Timeline.Phases[phaseIndex];
        var newSubphaseIndex = phase.CheckpointNames.IndexOf(mechanic.Name);
        if (newSubphaseIndex >= subphaseIndex)
            subphaseIndex = newSubphaseIndex;

        // emit event
        foreach (var action in mechanic.Actions)
            EmitAction(action);

        // check transition
        CheckTransition(mechanic);

        // update context (subphase and checkpoints change)
        UpdateContext();
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

        // update context (variables change)
        UpdateContext();
    }

    private void CheckTransition(Mechanic mechanic)
    {
        var phase = dutyConfig.Timeline.Phases[phaseIndex];
        foreach (var transition in phase.Transitions)
        {
            if (transition.Conditions
                          .Where(x => x.Type == "MECHANIC_TRIGGERED")
                          .Any(x => x.MechanicName == mechanic.Name))
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
                          .Any(x => x.Expression.Contains(variable) && CheckExpression(x.Expression)))
            {
                EnterPhase(dutyConfig.Timeline.Phases.IndexOf(x => x.Name == transition.TargetPhase));
                return;
            }
        }
    }

    private static bool CheckTrigger(Trigger trigger, IEvent? e = null)
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

            return op switch
            {
                "==" => Math.Abs(currentValue - targetValue) < 0.05,
                "!=" => Math.Abs(currentValue - targetValue) > 0.05,
                ">" => currentValue > targetValue,
                ">=" => currentValue >= targetValue,
                "<" => currentValue < targetValue,
                "<=" => currentValue <= targetValue,
                _ => false
            };
        }
        catch (Exception) { return false; }
    }

    #endregion
}
