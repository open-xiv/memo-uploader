using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using MemoUploader.Api;
using MemoUploader.Models;


namespace MemoUploader.Engine;

public class RuleEngine
{
    // event queue
    private readonly ActionBlock<IEvent> eventQueue;

    // event history
    private readonly EventRecorder eventHistory = new(1000);

    // fight context
    private FightContext? fightContext;

    public RuleEngine()
        => eventQueue = new ActionBlock<IEvent>(ProcessEventAsync, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });


    public void PostEvent(IEvent e)
        => eventQueue.Post(e);

    public async Task ProcessEventAsync(IEvent e)
    {
        // event logs
        eventHistory.Record(e);

        if (e is TerritoryChanged tc)
        {
            fightContext?.CompletedSnap();

            var dutyConfig = await ApiClient.FetchDutyConfigAsync(tc.ZoneId);
            fightContext = dutyConfig is not null ? new FightContext(dutyConfig) : null;
        }

        fightContext?.ProcessEvent(e);
    }
}
