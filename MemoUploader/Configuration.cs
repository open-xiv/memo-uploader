using System;
using Dalamud.Configuration;


namespace MemoUploader;

[Serializable]
public class Configuration : IPluginConfiguration
{
    /// <summary>
    /// upload switch
    /// </summary>
    public bool EnableUpload { get; set; } = true;

    /// <summary>
    /// sumemo api
    /// </summary>
    public string ApiEndpoint { get; set; } = "https://api.sumemo.dev";

    public int Version { get; set; } = 1;

    public void Save()
        => Plugin.PluginInterface.SavePluginConfig(this);
}
