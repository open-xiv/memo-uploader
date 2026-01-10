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
    private const string AuthKey   = ApiSecrets.AuthKey;

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
    ///     fetch duty configuration from the API.
    /// </summary>
    /// <param name="zoneId">zone id of territory</param>
    /// <returns>duty config if successful, otherwise null</returns>
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

    /// <summary>
    ///     upload fight record to the API.
    /// </summary>
    /// <param name="payload">fight record payload</param>
    /// <returns>true if successful, otherwise false</returns>
    public static async Task<bool> UploadFightRecordAsync(FightRecordPayload payload)
    {
        const string url = $"{ApiUrl}/fight";
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            if (DService.Instance().Condition[ConditionFlag.DutyRecorderPlayback])
            {
                DService.Instance().Log.Debug($"{content.ReadAsStringAsync().Result}");
                DService.Instance().Log.Debug("fight record uploaded canceled [playback mode]");
                return true;
            }

            var resp = await client.PostAsync(url, content);
            if (resp.StatusCode == HttpStatusCode.Created)
            {
                DService.Instance().Log.Debug("fight record uploaded successfully");
                return true;
            }
            DService.Instance().Log.Warning($"fight record upload failed: {resp.StatusCode}");
            DService.Instance().Log.Warning(resp.Content.ReadAsStringAsync().Result);
            return false;
        }
        catch (Exception) { return false; }
    }
}
