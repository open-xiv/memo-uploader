using System;
using System.Linq;
using System.Net;
using System.Net.Http;
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
        Client.Timeout = TimeSpan.FromSeconds(5);
    }

    public static async Task<bool> UploadFight(FightRecordPayload payload)
    {
        var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
        var tasks   = ApiUrls.Select(apiUrl => UploadFightToUrl(apiUrl, content)).ToList();
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

    private static async Task<bool> UploadFightToUrl(string apiUrl, StringContent content)
    {
        var       url = $"{apiUrl}/fight";
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
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
