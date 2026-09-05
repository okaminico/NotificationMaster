using Dalamud.Interface.Utility;
using ECommons.Funding;

namespace NotificationMaster;

internal partial class ConfigGui : IDisposable
{
    internal bool open = false;
    internal NotificationMaster p;
    internal ConfigGui(NotificationMaster p)
    {
        this.p = p;
        Svc.PluginInterface.UiBuilder.Draw += Draw;
        PatreonBanner.IsOfficialPlugin = () => true;
    }

    public void Dispose()
    {
        Svc.PluginInterface.UiBuilder.Draw -= Draw;
    }

    internal void Draw()
    {
        if(p.PauseUntil > Environment.TickCount64)
        {
            ImGuiHelpers.ForceNextWindowMainViewport();
            var sb = new StringBuilder("NotificationMaster is paused".Loc());
            if(p.PauseUntil != long.MaxValue)
            {
                var ts = TimeSpan.FromMilliseconds(p.PauseUntil - Environment.TickCount64);
                sb.Append(" for ??".Loc($"{(ts.Days * 60 + ts.Hours):D2}:{ts.Minutes:D2}:{ts.Seconds:D2}"));
            }
            var text = sb.ToString();
            var dims = ImGui.CalcTextSize(text);
            ImGuiHelpers.SetNextWindowPosRelativeMainViewport(new Vector2(ImGuiHelpers.MainViewport.Size.X / 2 - dims.X / 2, 10f));
            ImGui.Begin("NotificationMasterPauseWarning", ImGuiWindowFlags.NoNav
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoBackground
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoInputs);
            ImGui.TextColored(ImGuiColors.DalamudOrange, text);
            ImGui.End();
        }
        if(open)
        {

            ImGui.PushStyleVar(ImGuiStyleVar.WindowMinSize, new Vector2(650f, 200f));
            if(ImGui.Begin("NotificationMaster configuration".Loc(), ref open))
            {
                if(p.fileSelector.IsSelecting())
                {
                    ImGui.Text("Awaiting file selection...".Loc());
                }
                else
                {
                    PatreonBanner.DrawRight();
                    ImGui.BeginTabBar("##NMtabs");
                    DrawTab("GP replenish".Loc(), DrawGpNotify, p.cfg.gp_Enable);
                    DrawTab("Cutscene ending".Loc(), DrawCutsceneConfig, p.cfg.cutscene_Enable);
                    DrawTab("Chat message".Loc(), DrawChatMessageGui, p.cfg.chatMessage_Enable);
                    DrawTab("Duty pop".Loc(), DrawCfPopConfig, p.cfg.cfPop_Enable);
                    DrawTab("Connection error".Loc(), DrawLoginErrorConfig, p.cfg.loginError_Enable);
                    DrawTab("Approaching map flag".Loc(), DrawMapFlagConfig, p.cfg.mapFlag_Enable);
                    DrawTab("Arrived (vnavmesh)".Loc(), DrawArrivedConfig, p.cfg.arrived_Enable);
                    DrawTab("Mob pulled".Loc(), DrawMobPulledConfig, p.cfg.mobPulled_Enable);
                    DrawTab("PartyFinder".Loc(), DrawPartyFinderConfig, p.cfg.partyFinder_Enable);
                    DrawTab("Fish Notify".Loc(), DrawFishBiteConfig, p.cfg.fishBite_Enable);
                    DrawTab("Duty start".Loc(), DrawDutyStartedConfig, p.cfg.dutyStart_Enable);
                    DrawTab("Ready check".Loc(), DrawReadyCheckConfig, p.cfg.readyCheck_Enable);
                    DrawTab("Party cutscene".Loc(), DrawPartyCutsceneConfig, p.cfg.partyCutscene_Enable);
                    DrawTab("Battle countdown".Loc(), DrawBattleCountdownConfig, p.cfg.countdown_Enable);
                    // 併進既有分頁列，不另開視窗：樞紐是「事件 x 管道」的一張表，
                    // 跟上面那些逐模組分頁是同一層級的東西。
                    DrawTab("通知樞紐", DrawHubConfig, p.cfg.hub_Enable && p.cfg.hub_Routes.Count > 0);
                    PatreonBanner.RightTransparentTab();
                    ImGui.EndTabBar();
                }
            }
            ImGui.End();
            if(!open)
            {
                p.cfg.Save();
                Notify.Success("Configuration saved".Loc());
            }
            ImGui.PopStyleVar();
        }
    }

    private void DrawTab(string name, Action function, bool enabled)
    {
        var colored = false;
        if(enabled)
        {
            colored = true;
            ImGui.PushStyleColor(ImGuiCol.Text, 0xff00ff00);
        }
        if(ImGui.BeginTabItem($"{name}"))
        {
            if(colored) ImGui.PopStyleColor();
            ImGui.BeginChild($"##{name}-child");
            function();
            ImGui.EndChild();
            ImGui.EndTabItem();
        }
        else
        {
            if(colored) ImGui.PopStyleColor();
        }
    }

    private void ForegroundWarning(bool display)
    {
        if(display)
        {
            ImGui.TextColored(ImGuiColors.DalamudRed, "Unfortunately bringing FFXIV to foreground isn't very reliable function.\nIf it fails to work for you - not much can be done.".Loc());
        }
    }
}
