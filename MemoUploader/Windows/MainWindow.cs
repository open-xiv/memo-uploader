using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using MemoUploader.Models;


namespace MemoUploader.Windows;

public class MainWindow : Window, IDisposable
{
    // window
    private Tab currentTab = Tab.Status;

    // event recorder
    private string eventFilter = string.Empty;

    public MainWindow() : base("SuMemo Uploader##sumemo-uploader-main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
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
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void DrawSettingsTab() { }

    private void DrawStatusTab()
    {
        // engine status
        if (ImGui.BeginTable("StatusTable", 2, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextColored(LightSkyBlue, "解析引擎");

            ImGui.TableSetColumnIndex(1);
            var state = PluginContext.Lifecycle;
            var (color, slug) = state switch
            {
                EngineState.Ready => (Gold, "准备就绪"),
                EngineState.InProgress => (LimeGreen, "解析中"),
                EngineState.Completed => (DeepSkyBlue, "完成解析"),
                _ => (Tomato, "关闭")
            };
            ImGui.TextColored(color, slug);

            ImGui.EndTable();
        }

        if (PluginContext.Lifecycle is not EngineState.InProgress)
            return;

        ImGui.Separator();

        // fight progress
        if (ImGui.BeginTable("ProgressTable", 2, ImGuiTableFlags.None))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100f);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.TextColored(LightSkyBlue, "战斗进度");

            ImGui.TableSetColumnIndex(1);
            ImGui.TextColored(Gold, PluginContext.CurrentPhase);
            if (!string.IsNullOrEmpty(PluginContext.CurrentSubphase))
            {
                ImGui.SameLine();
                ImGui.TextColored(LightSkyBlue, $"{PluginContext.CurrentSubphase}");
            }
            ImGui.EndTable();
        }

        ImGui.Spacing();

        // checkpoints
        if (ImGui.BeginTable("CheckpointsTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            // 表头
            ImGui.TableSetupColumn("检查点", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("状态", ImGuiTableColumnFlags.WidthFixed, 80f);
            ImGui.TableHeadersRow();

            // 表格内容
            foreach (var (name, completed) in PluginContext.Checkpoints)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(name);

                ImGui.TableSetColumnIndex(1);
                var statusText  = completed ? "已完成" : "未完成";
                var statusColor = completed ? LightGreen : LightSkyBlue;
                ImGui.TextColored(statusColor, statusText);
            }
            ImGui.EndTable();
        }

        if (PluginContext.Variables.Count <= 0)
            return;

        ImGui.Spacing();

        if (ImGui.BeginTable("VariablesTable", 2, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("变量", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("当前值", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var (key, value) in PluginContext.Variables)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text(key);

                ImGui.TableSetColumnIndex(1);
                ImGui.TextColored(Gold, value?.ToString() ?? "null");
            }
            ImGui.EndTable();
        }
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
                var events = PluginContext.EventHistory;
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

    private enum Tab
    {
        Status,
        Settings,
        EventQueue
    }
}
