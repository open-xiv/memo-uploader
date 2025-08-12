using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using MemoUploader.Api;
using MemoUploader.Engine;
using MemoUploader.Models;


namespace MemoUploader.Windows;

public class MainWindow : Window, IDisposable
{
    // props
    private readonly Configuration config;
    private readonly RuleEngine    engine;

    // window
    private Tab currentTab = Tab.Status;

    // event recorder
    private string eventFilter = string.Empty;

    public MainWindow(Configuration config, RuleEngine engine) : base("SuMemo Uploader##sumemo-uploader-main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        // props
        this.config = config;
        this.engine = engine;

        // window
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    public override void Draw()
    {
        var slate = new Vector4(0.15f, 0.15f, 0.15f, 1.0f);

        using var rounding    = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 8.0f);
        using var borderSize  = ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1.4f);
        using var childColor  = ImRaii.PushColor(ImGuiCol.ChildBg, slate);
        using var borderColor = ImRaii.PushColor(ImGuiCol.Border, Wheat3);

        using var table = ImRaii.Table("MainWindowTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.NoBordersInBody);
        if (!table)
            return;

        ImGui.TableSetupColumn("Sidebar", ImGuiTableColumnFlags.WidthFixed, 150f);
        ImGui.TableSetupColumn("Content");

        ImGui.TableNextRow();
        ImGui.TableNextColumn();

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(16, 16)))
        using (ImRaii.Child("SidebarChild", Vector2.Zero, true, ImGuiWindowFlags.NoScrollbar))
            DrawSidebar();

        ImGui.TableNextColumn();

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(24, 24)))
        using (ImRaii.Child("ContentChild", Vector2.Zero, true))
            DrawContent();
    }

    private void DrawSidebar()
    {
        if (ImGuiOm.SelectableTextCentered("状态", currentTab == Tab.Status))
            currentTab = Tab.Status;

        if (ImGuiOm.SelectableTextCentered("设置", currentTab == Tab.Settings))
            currentTab = Tab.Settings;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGuiOm.SelectableTextCentered("事件", currentTab == Tab.EventQueue))
            currentTab = Tab.EventQueue;

        if (ImGuiOm.SelectableTextCentered("挂钩", currentTab == Tab.Listeners))
            currentTab = Tab.Listeners;
    }

    private void DrawContent()
    {
        switch (currentTab)
        {
            case Tab.Status:
                DrawStatusTab();
                break;
            case Tab.Settings:
                DrawSettingsTab();
                break;
            case Tab.EventQueue:
                DrawEventQueueTab();
                break;
            case Tab.Listeners:
                DrawListenersTab();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawSettingsTab()
    {
        ImGui.Text("设置");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        var enableUpload = config.EnableUpload;
        if (ImGuiOm.CheckboxColored("进度上传", ref enableUpload))
        {
            config.EnableUpload    = enableUpload;
            ApiClient.EnableUpload = enableUpload;
            config.Save();
        }
    }

    private void DrawStatusTab()
    {
        // engine status
        ImGui.TextColored(LightSkyBlue, "解析引擎");
        ImGui.SameLine();
        var color = engine.State switch
        {
            EngineState.Ready => Gold,
            EngineState.InProgress => LimeGreen,
            EngineState.Completed => DeepSkyBlue,
            _ => Tomato
        };
        var slug = engine.State switch
        {
            EngineState.Ready => "准备就绪",
            EngineState.InProgress => "解析中",
            EngineState.Completed => "完成解析",
            _ => "关闭"
        };
        ImGui.TextColored(color, $"{slug}");
    }

    private void DrawEventQueueTab()
    {
        ImGui.InputTextWithHint("##sumemo-event-filter", "筛选事件", ref eventFilter, 100);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.BeginChild("EventHistoryChild", Vector2.Zero, false);
        {
            if (ImGui.BeginTable("EventLogTable", 3, ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY | ImGuiTableFlags.SizingFixedFit))
            {
                ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 90);
                ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);

                // filter events
                var events = engine.EventHistory;
                var showEvents = string.IsNullOrWhiteSpace(eventFilter)
                                     ? events
                                     : events.Where(log =>
                                                        log.Category.Contains(eventFilter, StringComparison.OrdinalIgnoreCase) ||
                                                        log.Message.Contains(eventFilter, StringComparison.OrdinalIgnoreCase)
                                     ).ToList();

                foreach (var evt in showEvents)
                {
                    ImGui.TableNextRow();

                    // time
                    ImGui.TableNextColumn();
                    ImGui.Text(evt.Time.ToLocalTime().ToString("HH:mm:ss.fff"));

                    // category
                    ImGui.TableNextColumn();
                    ImGui.Text(evt.Category);

                    // message
                    ImGui.TableNextColumn();
                    ImGui.Text(evt.Message);
                }
                ImGui.EndTable();
            }
        }
        ImGui.EndChild();
    }

    private void DrawListenersTab() { }

    private enum Tab
    {
        Status,
        Settings,
        EventQueue,
        Listeners
    }
}
