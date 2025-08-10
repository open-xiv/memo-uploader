using System.Threading.Tasks.Dataflow;
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
    private const string CommandName = "/smm";

    // service
    public readonly RuleEngine Engine;

    // shared across service
    public readonly ActionBlock<IEvent> EventQueue;
    public readonly EventManager        EventService;

    // plugin windows
    public readonly WindowSystem WindowSystem = new("MemoUploader");

    public Plugin(IDalamudPluginInterface pi)
    {
        Config = pi.GetPluginConfig() as Configuration ?? new Configuration();
        DService.Init(pi);

        // engine
        Engine     = new RuleEngine();
        EventQueue = new ActionBlock<IEvent>(Engine.ProcessEventAsync, new ExecutionDataflowBlockOptions { MaxDegreeOfParallelism = 1 });

        // services
        EventService = new EventManager(this);
        EventService.Init();

        // window
        MainWindow = new MainWindow(this);
        WindowSystem.AddWindow(MainWindow);

        // command
        CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand) { HelpMessage = "Open SuMemo Uploader Window" });

        pi.UiBuilder.Draw       += DrawUI;
        pi.UiBuilder.OpenMainUi += ToggleMainUI;
    }

    public Configuration Config { get; init; }

    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    private MainWindow MainWindow { get; init; }

    public void Dispose()
    {
        // command
        CommandManager.RemoveHandler(CommandName);

        // window
        WindowSystem.RemoveAllWindows();
        MainWindow.Dispose();

        // services
        EventService.Uninit();

        // engine
        EventQueue.Complete();
        EventQueue.Completion.Wait();

        DService.Uninit();
    }


    private void OnCommand(string command, string args)
        => ToggleMainUI();

    private void DrawUI() => WindowSystem.Draw();

    public void ToggleMainUI() => MainWindow.Toggle();
}
