using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using MemoUploader.Events;


namespace MemoUploader.Engine;

public class RuleEngine
{
    // event history
    private const    int                     MaxEventHistory = 80;
    private readonly ConcurrentQueue<string> eventHistory    = [];
    public           IEnumerable<string>     EventHistory => eventHistory.ToArray();

    public async Task ProcessEventAsync(IEvent e)
    {
        // event logs
        var eventString = $"[{DateTime.Now:HH:mm:ss.fff}] {e}";
        eventHistory.Enqueue(eventString);
        while (eventHistory.Count > MaxEventHistory)
            eventHistory.TryDequeue(out _);
    }
}
