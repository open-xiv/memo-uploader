using System.Collections.Generic;
using Newtonsoft.Json;


namespace MemoUploader.Models;

public class DutyConfig
{
    [JsonProperty("zone_id")]
    public uint ZoneId { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("name_en")]
    public string NameEn { get; set; } = string.Empty;

    [JsonProperty("code")]
    public string Code { get; set; } = string.Empty;

    [JsonProperty("party_size")]
    public uint PartySize { get; set; }

    [JsonProperty("variables")]
    public List<Variable> Variables { get; set; } = new();

    [JsonProperty("mechanics")]
    public List<Mechanic> Mechanics { get; set; } = new();

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
    public string NameEn { get; set; } = string.Empty;

    [JsonProperty("trigger")]
    public required Trigger Trigger { get; set; }

    [JsonProperty("actions")]
    public List<Action> Actions { get; set; } = new();
}

public class Timeline
{
    [JsonProperty("start_phase")]
    public string StartPhase { get; set; } = string.Empty;

    [JsonProperty("phases")]
    public List<Phase> Phases { get; set; } = new();
}

public class Phase
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("target_id")]
    public uint TargetId { get; set; }

    [JsonProperty("checkpoints")]
    public List<string> Checkpoints { get; set; } = new();

    [JsonProperty("transitions")]
    public List<Transition> Transitions { get; set; } = new();
}

public class Transition
{
    [JsonProperty("target_phase")]
    public string TargetPhase { get; set; } = string.Empty;

    [JsonProperty("conditions")]
    public List<Trigger> Conditions { get; set; } = new();
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
    public uint? ActionId { get; set; }

    [JsonProperty("npc_id")]
    public uint? NpcId { get; set; }

    [JsonProperty("value")]
    public double? Value { get; set; }

    [JsonProperty("condition")]
    public string Condition { get; set; } = string.Empty;

    [JsonProperty("status_id")]
    public uint? StatusId { get; set; }

    [JsonProperty("stack_count")]
    public int? StackCount { get; set; }

    [JsonProperty("operator")]
    public string Operator { get; set; } = string.Empty;

    [JsonProperty("conditions")]
    public List<Trigger> Conditions { get; set; } = new();

    [JsonProperty("expression")]
    public string Expression { get; set; } = string.Empty;

    [JsonProperty("mechanic_name")]
    public string MechanicName { get; set; } = string.Empty;
}
