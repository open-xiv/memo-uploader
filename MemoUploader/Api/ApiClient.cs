using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Game.ClientState.Conditions;
using MemoUploader.Models;
using Newtonsoft.Json;


namespace MemoUploader.Api;

public static class ApiClient
{
    private static readonly HttpClient Client;

    private static readonly string[] AssetUrls =
    {
        "https://assets.sumemo.dev",
        "https://haku.diemoe.net/assets"
    };

    private const string ApiUrl  = "https://api.sumemo.dev";
    private const string AuthKey = ApiSecrets.AuthKey;

    static ApiClient()
    {
        Client = new HttpClient
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrLower
        };

        Client.DefaultRequestHeaders.Add("X-Auth-Key", AuthKey);
        Client.Timeout = TimeSpan.FromSeconds(5);
    }

    public static async Task<DutyConfig?> FetchDuty(uint zoneId)
    {
        var tasks = AssetUrls.Select(assetUrl => FetchDutyFromUrl(assetUrl, zoneId)).ToList();
        while (tasks.Count > 0)
        {
            var complete = await Task.WhenAny(tasks);
            var result   = await complete;
            if (result is not null)
                return result;
            tasks.Remove(complete);
        }
        return null;
    }

    public static async Task<DutyConfig?> FetchDutyFromUrl(string assetUrl, uint zoneId)
    {
        var       url = $"{assetUrl}/duty/{zoneId}";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            var resp = await Client.GetAsync(url, cts.Token);
            if (!resp.IsSuccessStatusCode)
                return null;

            var content = await resp.Content.ReadAsStringAsync(cts.Token);
            return JsonConvert.DeserializeObject<DutyConfig>(content);
        }
        catch (Exception) { return null; }
    }

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

            var resp = await Client.PostAsync(url, content);
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
