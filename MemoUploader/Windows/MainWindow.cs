using System;
using System.Numerics;
using Dalamud.Interface.Windowing;


namespace MemoUploader.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private          Tab    currentTab = Tab.Status;

    public MainWindow(Plugin plugin) : base("SuMemo Uploader##sumemo-uploader-main", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;

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

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 10)))
        using (ImRaii.Child("SidebarChild", Vector2.Zero, true, ImGuiWindowFlags.NoScrollbar))
            DrawSidebar();

        ImGui.TableNextColumn();

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(10, 10)))
        using (ImRaii.Child("ContentChild", Vector2.Zero, true))
            DrawContent();
    }

    private void DrawSidebar()
    {
        if (ImGuiOm.SelectableTextCentered("状态", currentTab == Tab.Status))
            currentTab = Tab.Status;

        if (ImGuiOm.SelectableTextCentered("设置", currentTab == Tab.Settings))
            currentTab = Tab.Settings;

        ImGui.Separator();

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
        }
    }

    private void DrawSettingsTab()
    {
        ImGui.Text("设置");
        ImGui.Separator();
        ImGui.Spacing();

        var enableUpload = plugin.Config.EnableUpload;
        if (ImGuiOm.CheckboxColored("进度上传", ref enableUpload))
        {
            plugin.Config.EnableUpload = enableUpload;
            plugin.Config.Save();
        }
    }

    private void DrawStatusTab() { }

    private void DrawEventQueueTab()
    {
        ImGui.BeginChild("EventHistoryChild", Vector2.Zero, false);
        {
            var recentEvents = plugin.Engine.EventHistory;
            foreach (var evt in recentEvents)
                ImGui.TextUnformatted(evt);
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
