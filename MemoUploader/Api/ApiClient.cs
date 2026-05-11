using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MemoEngine.Models;
using Newtonsoft.Json;


namespace MemoUploader.Api;

public static class ApiClient
{
    private static readonly HttpClient Client;

    private static readonly string[] ApiUrls =
    [
        "https://api.sumemo.dev",
        "https://sumemo.diemoe.net"
    ];

    private const string AuthKey = ApiSecrets.AuthKey;

    // chosen by the /status race; volatile so cross-thread reads see the latest write.
    private static volatile string preferredUrl = ApiUrls[0];

    static ApiClient()
    {
        Client = new HttpClient
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrLower
        };

        Client.DefaultRequestHeaders.Add("X-Auth-Key", AuthKey);
        Client.DefaultRequestHeaders.Add("X-Client-Name", "SuMemo.Uploader");
        Client.DefaultRequestHeaders.Add("X-Client-Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0");
        Client.Timeout = TimeSpan.FromSeconds(5);

        _ = Task.Run(RaceForFastestAsync);
    }

    public static async Task<bool> UploadFight(FightRecordPayload payload)
    {
        var json = JsonConvert.SerializeObject(payload);
        Plugin.Log.Info($"[Upload] body: {json}");

        var primary = preferredUrl;
        if (await UploadFightToUrl(primary, json))
            return true;

        var fallback = ApiUrls.FirstOrDefault(u => u != primary);
        if (fallback != null && await UploadFightToUrl(fallback, json))
        {
            // primary failed but fallback worked — re-race to refresh the preference.
            _ = Task.Run(RaceForFastestAsync);
            return true;
        }
        return false;
    }

    private static async Task RaceForFastestAsync()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var tasks = ApiUrls.Select(url => ProbeStatusAsync(url, cts.Token)).ToList();
        while (tasks.Count > 0)
        {
            var done   = await Task.WhenAny(tasks);
            var winner = await done;
            if (winner != null)
            {
                if (preferredUrl != winner)
                {
                    Plugin.Log.Info($"[ApiClient] preferred URL: {winner}");
                    preferredUrl = winner;
                }
                cts.Cancel();
                return;
            }
            tasks.Remove(done);
        }
        Plugin.Log.Warning($"[ApiClient] /status race: no endpoint reachable; keeping {preferredUrl}");
    }

    private static async Task<string?> ProbeStatusAsync(string url, CancellationToken ct)
    {
        try
        {
            using var resp = await Client.GetAsync($"{url}/status", ct);
            return resp.IsSuccessStatusCode ? url : null;
        }
        catch
        {
            return null;
        }
    }

    private static async Task<bool> UploadFightToUrl(string apiUrl, string json)
    {
        var       url     = $"{apiUrl}/fight";
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        using var cts     = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        try
        {
            var resp = await Client.PostAsync(url, content, cts.Token);
            if (resp.StatusCode is HttpStatusCode.Created or HttpStatusCode.OK)
            {
                Plugin.Log.Info($"[Upload] success: url={apiUrl}");
                return true;
            }
            var err = await resp.Content.ReadAsStringAsync(cts.Token);
            Plugin.Log.Warning($"[Upload] failure: url={apiUrl} status={(int)resp.StatusCode} reason={err}");
            return false;
        }
        catch (Exception e)
        {
            Plugin.Log.Warning($"[Upload] failure: url={apiUrl} reason=exception message={e.Message}");
            return false;
        }
    }
}
