using System;
using System.Linq;
using Dalamud.Plugin.Services;
using MemoEngine;
using MemoEngine.Models;


namespace MemoUploader.Events;

public class HpManager
{
    public void Init() => FrameworkManager.Instance().Reg(OnFrameworkUpdate, 200);

    public void Uninit() => FrameworkManager.Instance().Unreg(OnFrameworkUpdate);

    private void OnFrameworkUpdate(IFramework _)
    {
        if (Context.Lifecycle is EngineState.Idle || Context.EnemyDataId == 0)
            return;

        if (DService.Instance().ObjectTable.FirstOrDefault(x => x.DataID == Context.EnemyDataId) is IBattleChara enemy)
            Event.Combatant.RaiseHpUpdated(DateTimeOffset.UtcNow, enemy.DataID, enemy.CurrentHp, enemy.MaxHp);
    }
}
