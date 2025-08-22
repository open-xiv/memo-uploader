using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using MemoUploader.Models;
using Newtonsoft.Json;


namespace MemoUploader.Api;

public static class ApiClient
{
    private static readonly HttpClient client;

    private const string AssetsUrl = "https://assets.sumemo.dev";
    private const string ApiUrl    = "https://api.sumemo.dev";
    private const string AuthKey   = "1a81c443-ceec-4df3-a676-e90203da007f";

    static ApiClient()
    {
        client = new HttpClient
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrLower
        };

        client.DefaultRequestHeaders.Add("X-Auth-Key", AuthKey);
        client.Timeout = TimeSpan.FromSeconds(5);
    }

    /// <summary>
    ///     Fetch duty configuration from the API.
    /// </summary>
    /// <param name="zoneId">The zone ID of the duty.</param>
    /// <returns>Duty config if successful, otherwise null.</returns>
    public static async Task<DutyConfig?> FetchDutyConfigAsync(uint zoneId)
    {
        var url = $"{AssetsUrl}/duty/{zoneId}?use-cache=false";
        try
        {
            var resp = await client.GetAsync(url);
            if (!resp.IsSuccessStatusCode)
                return null;

            var content = await resp.Content.ReadAsStringAsync();
            var duty    = JsonConvert.DeserializeObject<DutyConfig>(content);
            return duty;
        }
        catch (Exception) { return null; }
    }

    public static async Task<bool> UploadFightRecordAsync(FightRecordPayload payload)
    {
        const string url = $"{ApiUrl}/fight";
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            if (DService.Condition[ConditionFlag.DutyRecorderPlayback])
            {
                DService.Log.Debug($"{content.ReadAsStringAsync().Result}");
                DService.Log.Debug("fight record uploaded canceled [playback]");
                return true;
            }

            var resp = await client.PostAsync(url, content);
            if (resp.StatusCode == HttpStatusCode.Created)
            {
                DService.Log.Debug("fight record uploaded successfully");
                return true;
            }
            return false;
        }
        catch (Exception) { return false; }
    }
}
