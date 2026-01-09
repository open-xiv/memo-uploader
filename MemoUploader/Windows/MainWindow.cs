using System;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using MemoUploader.Models;


namespace MemoUploader.Windows;

public class MainWindow : Window, IDisposable
{
    // window
    private readonly Tab currentTab = Tab.Status;

    // event recorder
    private string eventFilter = string.Empty;

    public MainWindow() : base(
        "酥卷记录仪##sumemo-uploader-main",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoBackground
    )
    {
        // window
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(240, 220),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };
    }

    public void Dispose() { }

    // public override void PreDraw()
    // {
    //     ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 20.0f);
    //     ImGui.PushStyleColor(ImGuiCol.WindowBg, KnownColor.LightPink.ToVector4() with { W = 1f });
    // }
    //
    // public override void PostDraw()
    // {
    //     ImGui.PopStyleColor(1);
    //     ImGui.PopStyleVar(1);
    // }

    public override void Draw()
    {
        using var rounding   = ImRaii.PushStyle(ImGuiStyleVar.ChildRounding, 15.0f);
        using var borderSize = ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1.2f);

        using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(16, 16)))
        using (ImRaii.PushColor(ImGuiCol.Border, KnownColor.LightPink.ToVector4()))
        using (ImRaii.PushColor(ImGuiCol.ChildBg, KnownColor.LightPink.ToVector4() with { W = 0.6f }))
        using (ImRaii.Child("SidebarChild", new Vector2(0, 64.0f), true, ImGuiWindowFlags.NoScrollbar))
            DrawHeader();

        ImGui.Spacing();

        if (currentTab != Tab.Status || currentTab == Tab.Status && PluginContext.Lifecycle == EngineState.InProgress)
        {
            using (ImRaii.PushStyle(ImGuiStyleVar.WindowPadding, new Vector2(24, 24)))
            using (ImRaii.PushColor(ImGuiCol.Border, KnownColor.LightSkyBlue.ToVector4()))
            using (ImRaii.PushColor(ImGuiCol.ChildBg, KnownColor.LightSkyBlue.ToVector4() with { W = 0.4f }))
            using (ImRaii.Child("ContentChild", Vector2.Zero, true))
                DrawContent();
        }
    }

    private void DrawHeader()
    {
        DrawEngineState();
        ImGui.SameLine();
        if (DrawButton("关闭窗口", true, fill: false))
            IsOpen = false;
    }

    private void DrawContent()
    {
        switch (currentTab)
        {
            case Tab.Status:
                DrawProgressTab();
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

    private void DrawEngineState()
    {
        string message;
        string detail;
        var    color = KnownColor.Beige;

        switch (PluginContext.Lifecycle)
        {
            case EngineState.InProgress:
                message = "解析中";
                detail  = "正在记录战斗数据";
                color   = KnownColor.DarkSeaGreen;
                break;

            case EngineState.Ready:
            case EngineState.Completed:
                message = "准备就绪";
                detail  = "等待战斗开始";
                color   = KnownColor.DarkGoldenrod;
                break;

            default:
                message = "休眠中";
                detail  = "进入特定战斗自动开启";
                color   = KnownColor.DarkSalmon;
                break;
        }

        DrawText(message, color.ToVector4() with { W = 0.8f }, KnownColor.Beige.ToVector4());
        ImGui.SameLine();
        DrawText(detail, secondaryBtnNormal, KnownColor.Beige.ToVector4());
    }

    private void DrawSettingsTab() { }

    private void DrawProgressTab()
    {
        if (PluginContext.CurrentPhase == "N/A")
        {
            DrawText("当前副本暂无细粒度划分", KnownColor.CornflowerBlue.ToVector4() with { W = 0.8f }, KnownColor.Beige.ToVector4());
            return;
        }

        DrawText(PluginContext.CurrentPhase, KnownColor.RoyalBlue.ToVector4() with { W = 0.8f }, KnownColor.Beige.ToVector4());
        if (!string.IsNullOrEmpty(PluginContext.CurrentSubphase))
        {
            ImGui.SameLine();
            DrawText(PluginContext.CurrentSubphase, KnownColor.CornflowerBlue.ToVector4() with { W = 0.8f }, KnownColor.Beige.ToVector4());
        }

        ImGui.Spacing();

        var first = true;
        foreach (var (name, completed) in PluginContext.Checkpoints)
        {
            var textWidth = ImGui.CalcTextSize(name).X + 16.0f * 2;
            if (!first)
                ImGui.SameLine();
            first = false;
            if (ImGui.GetContentRegionAvail().X < textWidth)
                ImGui.NewLine();
            var statusColor = completed ? KnownColor.DarkSeaGreen : KnownColor.DarkSalmon;
            DrawText(name, statusColor.ToVector4() with { W = 0.8f }, KnownColor.Beige.ToVector4());
        }

        // if (PluginContext.VariableStats.Count <= 0)
        //     return;
        //
        // ImGui.Spacing();
        //
        // first = true;
        // foreach (var (key, value) in PluginContext.VariableStats)
        // {
        //     var textWidth = ImGui.CalcTextSize($"{key}: {value}").X + 16.0f * 2;
        //     if (!first)
        //         ImGui.SameLine();
        //     first = false;
        //     if (ImGui.GetContentRegionAvail().X < textWidth)
        //         ImGui.NewLine();
        //     DrawText($"{key}: {value}", KnownColor.SlateGray.ToVector4() with { W = 0.8f }, KnownColor.Beige.ToVector4());
        // }
    }

    private void DrawEventQueueTab()
    {
        ImGui.InputTextWithHint("##sumemo-event-filter", "筛选事件", ref eventFilter, 100);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.BeginChild("EventHistoryChild", Vector2.Zero);
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

    #region Colors

    private readonly Vector4 primaryBtnNormal   = KnownColor.HotPink.ToVector4() with { W = 0.6f };
    private readonly Vector4 primaryBtnHovered  = KnownColor.HotPink.ToVector4() with { W = 0.8f };
    private readonly Vector4 primaryBtnActive   = KnownColor.DeepPink.ToVector4() with { W = 0.8f };
    private readonly Vector4 primaryBtnSelected = KnownColor.DeepPink.ToVector4() with { W = 0.6f };

    private readonly Vector4 secondaryBtnNormal   = KnownColor.CornflowerBlue.ToVector4() with { W = 0.6f };
    private readonly Vector4 secondaryBtnHovered  = KnownColor.CornflowerBlue.ToVector4() with { W = 0.8f };
    private readonly Vector4 secondaryBtnActive   = KnownColor.RoyalBlue.ToVector4() with { W = 0.8f };
    private readonly Vector4 secondaryBtnSelected = KnownColor.RoyalBlue.ToVector4() with { W = 0.8f };

    #endregion

    #region Components

    private bool DrawButton(string label, bool isSelected, bool primary = true, bool fill = true, float height = 32.0f)
    {
        using var rounding = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 8.0f);

        // theme
        var normalColor   = primary ? primaryBtnNormal : secondaryBtnNormal;
        var hoveredColor  = primary ? primaryBtnHovered : secondaryBtnHovered;
        var activeColor   = primary ? primaryBtnActive : secondaryBtnActive;
        var selectedColor = primary ? primaryBtnSelected : secondaryBtnSelected;

        // color
        var       baseColor   = isSelected ? selectedColor : normalColor;
        using var colorBtn    = ImRaii.PushColor(ImGuiCol.Button, baseColor);
        using var colorHover  = ImRaii.PushColor(ImGuiCol.ButtonHovered, isSelected ? selectedColor * 1.1f : hoveredColor);
        using var colorActive = ImRaii.PushColor(ImGuiCol.ButtonActive, activeColor);

        // size
        var width = fill ? ImGui.GetContentRegionAvail().X : ImGui.CalcTextSize(label).X + 16.0f * 2;

        // draw
        var clicked = ImGui.Button(label, new Vector2(width, height));

        return clicked;
    }

    private void DrawText(string text, Vector4 bgColor, Vector4 textColor, float height = 32.0f)
    {
        using var round    = ImRaii.PushStyle(ImGuiStyleVar.FrameRounding, 8.0f);
        var       textSize = ImGui.CalcTextSize(text).X + 16.0f * 2;

        // color
        using var c1 = ImRaii.PushColor(ImGuiCol.Button, bgColor);
        using var c2 = ImRaii.PushColor(ImGuiCol.ButtonHovered, bgColor);
        using var c3 = ImRaii.PushColor(ImGuiCol.ButtonActive, bgColor);
        using var c4 = ImRaii.PushColor(ImGuiCol.Text, textColor);

        ImGui.Button(text, new Vector2(textSize, height));
    }

    #endregion

    private enum Tab
    {
        Status,
        Settings,
        EventQueue
    }
}
