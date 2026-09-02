namespace NotificationMaster;

internal unsafe partial class ConfigGui
{
    internal void DrawArrivedConfig()
    {
        if(ImGui.Checkbox("Enable".Loc(), ref p.cfg.arrived_Enable))
        {
            Arrived.Setup(p.cfg.arrived_Enable, p);
        }
        if(p.cfg.arrived_Enable)
        {
            ImGui.Text("Triggers when vnavmesh finishes an automatic navigation (Questionable, vnavmesh's own /vnav commands, etc.), not just map flags.".Loc());
            ImGui.TextColored(ImGuiColors.DalamudGrey, "Requires vnavmesh to be installed. If it's not, this module simply never fires.".Loc());
            ImGui.Spacing();
            ImGui.Text("When arriving if FFXIV is running in background:".Loc());
            ImGui.Checkbox("Show tray notification".Loc(), ref p.cfg.arrived_ShowToastNotification);
            ImGui.Checkbox("Flash taskbar icon".Loc(), ref p.cfg.arrived_FlashTrayIcon);
            ImGui.Checkbox("Bring FFXIV to foreground".Loc(), ref p.cfg.arrived_AutoActivateWindow);
            ImGui.Checkbox("Execute actions even if game is active".Loc(), ref p.cfg.arrived_AlwaysExecute);
            ImGui.Checkbox("Ask Tataru to remind you when this triggers (requires TataruPraise)".Loc(), ref p.cfg.arrived_TataruPraise);
            if(ImGui.IsItemHovered()) ImGui.SetTooltip("Plays a TataruPraise voice line through IPC, under the same conditions as the actions above. Silently skipped if TataruPraise is not installed or is turned off.".Loc());
            ForegroundWarning(p.cfg.arrived_AutoActivateWindow);
            ImGui.SetNextItemWidth(100f);
            ImGui.DragFloat("Debounce (seconds)".Loc(), ref p.cfg.arrived_DebounceSeconds, 0.1f, 0.3f, 10f, "%.1f");
            if(ImGui.IsItemHovered()) ImGui.SetTooltip("How long navigation has to stay stopped before this counts as 'arrived' rather than a mid-route recalculation (which briefly stops and restarts the path). Raise this if you get false triggers while still moving.".Loc());
            DrawSoundSettings(ref p.cfg.arrived_SoundSettings);
            DrawHttpMaster(p.cfg.arrived_HttpRequests, ref p.cfg.arrived_HttpRequestsEnable,
                "None available".Loc());
        }
    }
}
