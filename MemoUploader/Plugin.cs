using Dalamud.Game.Command;
using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using MemoUploader.Engine;
using MemoUploader.Events;
using MemoUploader.Windows;


namespace MemoUploader;

public sealed class Plugin : IDalamudPlugin
{
    private const string CommandName = "/memo";

    // service
    private readonly RuleEngine   engine;
    private readonly EventManager eventService;

    // plugin windows
    public readonly WindowSystem WindowSystem = new("酥卷");

    public Plugin(IDalamudPluginInterface pi)
    {
        Config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        DService.Init(pi);

        // engine
        engine = new RuleEngine();

        // services
        eventService = new EventManager();
        eventService.Init();

        // link engine and services
        eventService.OnEvent += engine.PostEvent;

        // window
        MainWindow = new MainWindow();
        WindowSystem.AddWindow(MainWindow);

        // command
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "打开当前进度窗口" });

        pi.UiBuilder.Draw       += DrawUI;
        pi.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public void Dispose()
    {
        // command
        CommandManager.RemoveHandler(CommandName);

        // window
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();

        // unlink engine and services
        eventService.OnEvent -= engine.PostEvent;

        // services
        eventService.Uninit();

        DService.Uninit();
    }

    #region props

    private Configuration Config { get; init; }

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    private MainWindow MainWindow { get; init; }

    #endregion

    private void OnCommand(string command, string args)
        => ToggleMainUI();

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleMainUI() => MainWindow.Toggle();
}
