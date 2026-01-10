using System;
using System.Linq;
using Dalamud.Plugin.Services;
using MemoUploader.Models;


namespace MemoUploader.Events;

public class HpManager(Action<IEvent> eventRaiser)
{
    public void Init() => FrameworkManager.Instance().Reg(OnFrameworkUpdate, throttleMS: 200);

    public void Uninit() => FrameworkManager.Instance().Unreg(OnFrameworkUpdate);

    private void OnFrameworkUpdate(IFramework _)
    {
        if (PluginContext.Lifecycle is null || PluginContext.EnemyDataId == 0)
            return;

        if (DService.Instance().ObjectTable.FirstOrDefault(x => x.DataID == PluginContext.EnemyDataId) is IBattleChara enemy)
            eventRaiser(new EnemyHpChanged(PluginContext.EnemyDataId, enemy.CurrentHp, enemy.MaxHp));
    }
}
