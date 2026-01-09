using System.Collections.Generic;
using Newtonsoft.Json;


namespace MemoUploader.Models;

public class DutyConfig
{
    [JsonProperty("zone_id")]
    public uint ZoneID { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("name_en")]
    public string NameEnglish { get; set; } = string.Empty;

    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("party_size")]
    public uint PartySize { get; set; }

    [JsonProperty("variables")]
    public List<Variable> Variables { get; set; } = [];

    [JsonProperty("mechanics")]
    public List<Mechanic> Mechanics { get; set; } = [];

    [JsonProperty("timeline")]
    public required Timeline Timeline { get; set; }
}

public class LogsEncounter
{
    [JsonProperty("zone")]
    public uint Zone { get; set; }

    [JsonProperty("encounter")]
    public uint Encounter { get; set; }

    [JsonProperty("difficulty")]
    public uint Difficulty { get; set; }
}

public class Variable
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("initial")]
    public object? Initial { get; set; }
}

public class Mechanic
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("name_en")]
    public string NameEnglish { get; set; } = string.Empty;

    [JsonProperty("trigger")]
    public required Trigger Trigger { get; set; }

    [JsonProperty("actions")]
    public List<Action> Actions { get; set; } = [];
}

public class Timeline
{
    [JsonProperty("start_phase")]
    public string StartPhase { get; set; } = string.Empty;

    [JsonProperty("phases")]
    public List<Phase> Phases { get; set; } = [];
}

public class Phase
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("target_id")]
    public uint TargetID { get; set; }

    [JsonProperty("checkpoints")]
    public List<string> CheckpointNames { get; set; } = [];

    [JsonProperty("transitions")]
    public List<Transition> Transitions { get; set; } = [];
}

public class Transition
{
    [JsonProperty("target_phase")]
    public string TargetPhase { get; set; } = string.Empty;

    [JsonProperty("conditions")]
    public List<Condition> Conditions { get; set; } = [];
}

public class Action
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("value")]
    public object? Value { get; set; }
}

public class Trigger
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("status")]
    public string Status { get; set; } = string.Empty;

    [JsonProperty("action_id")]
    public uint? ActionID { get; set; }

    [JsonProperty("npc_id")]
    public uint? NPCID { get; set; }

    [JsonProperty("value")]
    public double? Value { get; set; }

    [JsonProperty("condition")]
    public string ConditionLambda { get; set; } = string.Empty;

    [JsonProperty("status_id")]
    public uint? StatusID { get; set; }

    [JsonProperty("stack_count")]
    public int? StackCount { get; set; }

    [JsonProperty("operator")]
    public string Operator { get; set; } = string.Empty;

    [JsonProperty("conditions")]
    public List<Trigger> Conditions { get; set; } = [];
}

public class Condition
{
    [JsonProperty("type")]
    public string Type { get; set; } = string.Empty;

    [JsonProperty("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonProperty("mechanic_name")]
    public string MechanicName { get; set; } = string.Empty;
}
