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

    static ApiClient()
    {
        Client = new HttpClient
        {
            DefaultRequestVersion = HttpVersion.Version30,
            DefaultVersionPolicy  = HttpVersionPolicy.RequestVersionOrLower
        };

        Client.DefaultRequestHeaders.Add("X-Auth-Key", AuthKey);
        Client.DefaultRequestHeaders.Add("X-Client-Name", "MemoUploader");
        Client.DefaultRequestHeaders.Add("X-Client-Version", Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.0.0.0");
        Client.Timeout = TimeSpan.FromSeconds(5);
    }

    public static async Task<bool> UploadFight(FightRecordPayload payload)
    {
        var json  = JsonConvert.SerializeObject(payload);
        var tasks = ApiUrls.Select(apiUrl => UploadFightToUrl(apiUrl, json)).ToList();
        while (tasks.Count > 0)
        {
            var complete = await Task.WhenAny(tasks);
            var result   = await complete;
            if (result)
                return true;
            tasks.Remove(complete);
        }
        return false;
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
                return true;
            await resp.Content.ReadAsStringAsync(cts.Token);
            return false;
        }
        catch (Exception) { return false; }
    }
}
