using System;
using System.Collections.Generic;
using Newtonsoft.Json;


namespace MemoUploader.Models;

public class FightRecordPayload
{
    [JsonProperty("start_time")]
    public DateTime StartTime { get; set; }

    [JsonProperty("duration")]
    public long Duration { get; set; } // nano seconds

    [JsonProperty("zone_id")]
    public uint ZoneID { get; set; }

    [JsonProperty("players")]
    public List<PlayerPayload> Players { get; set; } = [];

    [JsonProperty("clear")]
    public bool IsClear { get; set; }

    [JsonProperty("progress")]
    public required FightProgressPayload Progress { get; set; }
}

public class PlayerPayload
{
    [JsonProperty("name")]
    public string Name { get; set; } = string.Empty;

    [JsonProperty("server")]
    public string Server { get; set; } = string.Empty;

    [JsonProperty("job_id")]
    public uint JobID { get; set; }

    [JsonProperty("level")]
    public uint Level { get; set; }

    [JsonProperty("death_count")]
    public uint DeathCount { get; set; }
}

public class FightProgressPayload
{
    [JsonProperty("phase")]
    public uint PhaseID { get; set; }

    [JsonProperty("subphase")]
    public uint SubphaseID { get; set; }

    [JsonProperty("enemy_id")]
    public uint EnemyID { get; set; }

    [JsonProperty("enemy_hp")]
    public double EnemyHP { get; set; }
}
