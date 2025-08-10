using Dalamud.Game.ClientState.Conditions;


namespace MemoUploader.Events;

public class EventManager(Plugin plugin)
{
    // service
    public ActionManager?    ActionService;
    public CombatantManager? CombatantService;
    public StatusManager?    StatusService;

    public void Init()
    {
        // GENERAL EVENTS
        DService.ClientState.TerritoryChanged += OnTerritoryChanged;

        // ACTION EVENTS
        ActionService = new ActionManager(plugin);
        ActionService.Init();

        // COMBATANT EVENTS
        CombatantService = new CombatantManager(plugin);
        CombatantService.Init();

        // STATUS EVENTS
        StatusService = new StatusManager(plugin);
        StatusService.Init();

        // DUTY EVENTS
        DService.DutyState.DutyStarted     += OnDutyStarted;
        DService.DutyState.DutyRecommenced += OnDutyRecommenced;
        DService.DutyState.DutyCompleted   += OnDutyCompleted;
        DService.DutyState.DutyWiped       += OnDutyWiped;

        // CONDITION EVENTS
        DService.Condition.ConditionChange += OnConditionChange;
    }

    public void Uninit()
    {
        // GENERAL EVENTS
        DService.ClientState.TerritoryChanged -= OnTerritoryChanged;

        // ACTION EVENTS
        ActionService?.Uninit();

        // COMBATANT EVENTS
        CombatantService?.Uninit();

        // STATUS EVENTS
        StatusService?.Uninit();

        // DUTY EVENTS
        DService.DutyState.DutyStarted     -= OnDutyStarted;
        DService.DutyState.DutyRecommenced -= OnDutyRecommenced;
        DService.DutyState.DutyCompleted   -= OnDutyCompleted;
        DService.DutyState.DutyWiped       -= OnDutyWiped;

        // CONDITION EVENTS
        DService.Condition.ConditionChange -= OnConditionChange;
    }

    private void OnTerritoryChanged(ushort zoneId)
        => plugin.EventQueue.Post(new TerritoryChanged(zoneId));

    private void OnDutyStarted(object? sender, ushort e)
        => plugin.EventQueue.Post(new DutyStarted());

    private void OnDutyRecommenced(object? sender, ushort e)
        => plugin.EventQueue.Post(new DutyRecommenced());

    private void OnDutyCompleted(object? sender, ushort e)
        => plugin.EventQueue.Post(new DutyCompleted());

    private void OnDutyWiped(object? sender, ushort e)
        => plugin.EventQueue.Post(new DutyWiped());

    private void OnConditionChange(ConditionFlag flag, bool value)
    {
        if (flag is not ConditionFlag.InCombat)
            return;

        plugin.EventQueue.Post(value ? new CombatOptIn() : new CombatOptOut());
    }
}
