using System.Collections.Generic;
using MemoUploader.Models;


namespace MemoUploader;

public static class PluginContext
{
    // recorder
    public static IReadOnlyList<EventLog> EventHistory = [];

    // fight context
    public static EngineState?                         Lifecycle;
    public static string                               CurrentPhase    = string.Empty;
    public static string                               CurrentSubphase = string.Empty;
    public static IReadOnlyList<(string, bool)>        Checkpoints     = [];
    public static IReadOnlyDictionary<string, object?> Variables       = new Dictionary<string, object?>();
}
