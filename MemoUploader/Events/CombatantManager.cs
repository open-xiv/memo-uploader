using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using ObjectKind = Dalamud.Game.ClientState.Objects.Enums.ObjectKind;


namespace MemoUploader.Events;

public class CombatantManager(Plugin plugin)
{
    private readonly List<IGameObject> combatants = [];

    public void Init() => DService.Framework.Update += OnFrameworkUpdate;

    public void Uninit()
    {
        DService.Framework.Update -= OnFrameworkUpdate;
        combatants.Clear();
    }

    private void OnFrameworkUpdate(IFramework framework)
    {
        if (!Throttler.Throttle("sumemo-combatant-update", 200))
            return;

        var current = DService.ObjectTable.Where(x => x.ObjectKind is ObjectKind.Player or ObjectKind.BattleNpc).ToList();

        // spawn
        foreach (var obj in current.Except(combatants))
            plugin.EventQueue.Post(new CombatantSpawn(obj.EntityId));

        // death
        foreach (var obj in combatants.Except(current))
            plugin.EventQueue.Post(new CombatantDeath(obj.EntityId));

        // targetable and untargetable
        foreach (var obj in current.Intersect(combatants))
        {
            if (combatants.TryGetFirst(x => x.GameObjectId == obj.GameObjectId, out var shot) && shot is not null)
            {
                switch (shot.IsTargetable)
                {
                    case true when !obj.IsTargetable:
                        plugin.EventQueue.Post(new CombatantUntargetable(obj.EntityId));
                        break;
                    case false when obj.IsTargetable:
                        plugin.EventQueue.Post(new CombatantTargetable(obj.EntityId));
                        break;
                }
            }
        }

        combatants.Clear();
        combatants.AddRange(current);
    }
}
