using Dalamud.Game.Command;
using ECommons.Configuration;
using ECommons.EzIpcManager;
using NotificationMaster.Hub;
using NotificationMaster.Notificators;
using NotificationMasterAPI;

namespace NotificationMaster;

public class NotificationMaster : IDalamudPlugin
{
    internal bool IsDisposed = false;
    internal Configuration cfg;
    internal ConfigGui configGui;

    internal GpNotify gpNotify = null;
    internal CutsceneEnded cutsceneEnded = null;
    internal ChatMessage chatMessage = null;
    internal CfPop cfPop = null;
    internal LoginError loginError = null;
    internal ApproachingMapFlag mapFlag = null;
    internal Arrived arrived = null;
    internal MobPulled mobPulled = null;
    internal PartyFinder partyFinder = null;
    internal FishBite fishBite = null;
    internal DutyStarted dutyStarted = null;
    internal ReadyCheck readyCheck = null;
    internal PartyCutsceneEnded partyCutsceneEnded = null;
    internal BattleCountdown battleCountdown = null;

    internal HttpMaster httpMaster;
    public ThreadUpdateActivatedState ThreadUpdActivated;
    internal AudioSelector fileSelector = new();
    internal AudioPlayer audioPlayer;

    internal long PauseUntil = 0;
    internal static NotificationMaster P;

    internal IPC IPC;
    internal NotificationMasterApi NotificationMasterApi;

    /// <summary>分類感知的通知樞紐（給<b>別的外掛</b>送通知進來用）。</summary>
    /// <remarks>📌 本外掛自己的 13 個 notificator 刻意不走這裡，見 <see cref="NotificationHub"/>。</remarks>
    internal NotificationHub Hub;

    [EzIPC("AutoDuty.IsStopped", false)] internal Func<bool> AutoDutyIsStopped;

    public string Name => "NotificationMaster";

    public NotificationMaster(IDalamudPluginInterface pluginInterface)
    {
        P = this;
        ECommonsMain.Init(pluginInterface, this, Module.DalamudReflector);
        ECommons.LanguageHelpers.Localization.Init("ChineseTraditional");
        EzConfig.PluginConfigDirectoryOverride = "NotificationMaster";
        new TickScheduler(() =>
        {
            EzConfig.Migrate<Configuration>();
            cfg = EzConfig.Init<Configuration>();
            cfg.Initialize(Svc.PluginInterface);
            httpMaster = new();
            ThreadUpdActivated = new();
            audioPlayer = new(this);

            configGui = new(this);
            Svc.PluginInterface.UiBuilder.OpenConfigUi += delegate { configGui.open = true; };

            if(cfg.gp_Enable) GpNotify.Setup(true, this);
            if(cfg.cutscene_Enable) CutsceneEnded.Setup(true, this);
            if(cfg.chatMessage_Enable) ChatMessage.Setup(true, this);
            if(cfg.cfPop_Enable) CfPop.Setup(true, this);
            if(cfg.loginError_Enable) LoginError.Setup(true, this);
            if(cfg.mapFlag_Enable) ApproachingMapFlag.Setup(true, this);
            if(cfg.arrived_Enable) Arrived.Setup(true, this);
            if(cfg.mobPulled_Enable) MobPulled.Setup(true, this);
            if(cfg.partyFinder_Enable) PartyFinder.Setup(true, this);
            if(cfg.fishBite_Enable) FishBite.Setup(true, this);
            if(cfg.dutyStart_Enable) DutyStarted.Setup(true, this);
            if(cfg.readyCheck_Enable) ReadyCheck.Setup(true, this);
            if(cfg.partyCutscene_Enable) PartyCutsceneEnded.Setup(true, this);
            if(cfg.countdown_Enable) BattleCountdown.Setup(true, this);

            if(Svc.PluginInterface.Reason == PluginLoadReason.Installer)
            {
                configGui.open = true;
                Notify.Warning(
                    ("You have installed NotificationMaster plugin. By default, it has no modules enabled. \n" +
                    "A settings window has been opened: please configure the plugin.").Loc());
            }
            Svc.Commands.AddHandler("/pnotify", new CommandInfo(OnCommand)
            {
                HelpMessage = ("open/close configuration\n" +
                "/pnotify shutup|s [time in minutes] - pause plugin for specified amount of minutes or until restart if time is not specified\n" +
                "/pnotify resume|r - resume plugin operation").Loc()
            });
            IPC = new();
            Hub = new();
            NotificationMasterApi = new(Svc.PluginInterface);
            EzIPC.Init(this);
        });
    }

    private void OnCommand(string command, string arguments)
    {
        if(arguments == "")
        {
            configGui.open = !configGui.open;
        }
        else
        {
            var args = arguments.Split(' ');
            if(args[0].Equals("shutup", StringComparison.OrdinalIgnoreCase) || args[0].Equals("s", StringComparison.OrdinalIgnoreCase))
            {
                if(args.Length == 1)
                {
                    PauseUntil = long.MaxValue;
                    Notify.Success("Plugin paused until restart".Loc());
                }
                else
                {
                    if(uint.TryParse(args[1], out var minutes))
                    {
                        PauseUntil = Environment.TickCount64 + minutes * 60 * 1000;
                        Notify.Success("Plugin paused for ?? minutes".Loc(minutes));
                    }
                    else
                    {
                        Notify.Error("Please enter amount of time in minutes".Loc());
                    }
                }
            }
            else if(args[0].Equals("resume", StringComparison.OrdinalIgnoreCase) || args[0].Equals("r", StringComparison.OrdinalIgnoreCase))
            {
                PauseUntil = 0;
                Notify.Success("Plugin operation resumed".Loc());
            }
            else
            {
                Notify.Error("Invanid command".Loc());
            }
        }
    }

    public void Dispose()
    {
        TrayIconManager.DestroyIcon();
        GpNotify.Setup(false, this);
        CutsceneEnded.Setup(false, this);
        ChatMessage.Setup(false, this);
        CfPop.Setup(false, this);
        LoginError.Setup(false, this);
        ApproachingMapFlag.Setup(false, this);
        Arrived.Setup(false, this);
        MobPulled.Setup(false, this);
        PartyFinder.Setup(false, this);
        FishBite.Setup(false, this);
        DutyStarted.Setup(false, this);
        ReadyCheck.Setup(false, this);
        PartyCutsceneEnded.Setup(false, this);
        BattleCountdown.Setup(false, this);
        // 🔴 樞紐先註銷：它註冊的是對外的 IPC 端點，晚一步註銷等於留一扇門通往正在拆的東西。
        //    Hub 可能是 null（TickScheduler 裡的初始化還沒跑完就被卸載），所以要判空。
        Hub?.Dispose();
        ThreadUpdActivated.Dispose();
        audioPlayer.Dispose();
        cfg.Save();
        configGui.Dispose();
        Svc.Commands.RemoveHandler("/pnotify");
        IsDisposed = true;
        ECommonsMain.Dispose();
        P = null;
    }
}
