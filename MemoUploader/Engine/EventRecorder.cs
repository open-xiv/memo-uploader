using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using MemoUploader.Models;


namespace MemoUploader.Engine;

public class EventRecorder(int maxEventHistory)
{
    // event log queue
    private readonly ConcurrentQueue<EventLog> eventHistory = [];

    // snapshot
    private          List<EventLog> snapHistory = [];
    private          bool           isDirty     = true;
    private readonly Lock           snapLock    = new();

    public void Record(IEvent e)
    {
        eventHistory.Enqueue(new EventLog(DateTime.UtcNow, e.Category, e.Message));
        while (eventHistory.Count > maxEventHistory)
            eventHistory.TryDequeue(out _);

        lock (snapLock)
            isDirty = true;
    }

    public IReadOnlyList<EventLog> GetSnap()
    {
        lock (snapLock)
            if (isDirty)
            {
                snapHistory = eventHistory.ToList();
                isDirty     = false;
            }
        return snapHistory;
    }
}
