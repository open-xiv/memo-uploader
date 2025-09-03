using System;
using System.Collections.Concurrent;
using MemoUploader.Models;


namespace MemoUploader.Engine;

public class EventRecorder(int maxEventHistory)
{
    // event log queue
    private readonly ConcurrentQueue<EventLog> eventHistory = [];

    /// <summary>
    ///     log an event to the event history.
    /// </summary>
    /// <param name="e">event to log</param>
    public void Record(IEvent e)
    {
        eventHistory.Enqueue(new EventLog(DateTime.UtcNow, e.Category, e.Message));
        while (eventHistory.Count > maxEventHistory)
            eventHistory.TryDequeue(out _);

        PluginContext.EventHistory = eventHistory.ToArray();
    }
}
