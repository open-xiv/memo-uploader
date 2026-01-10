using System.Collections.Generic;
using MemoUploader.Events;
using MemoUploader.Models;


namespace MemoUploader;

public static class PluginContext
{
    // party provider
    public static IPartyProvider PartyProvider { get; set; } = new DalamudPartyProvider();

    // recorder
    public static IReadOnlyList<EventLog> EventHistory { get; set; } = [];

    // fight context
    public static EngineState?                         Lifecycle       { get; set; }
    public static string                               CurrentPhase    { get; set; } = string.Empty;
    public static string                               CurrentSubphase { get; set; } = string.Empty;
    public static uint                                 EnemyDataId     { get; set; }
    public static IReadOnlyList<(string, bool)>        Checkpoints     { get; set; } = [];
    public static IReadOnlyDictionary<string, object?> VariableStats   { get; set; } = new Dictionary<string, object?>();
}
