using Dalamud.Game.ClientState.Conditions;

namespace NotificationMaster;

/// <summary>
/// 偵測 vnavmesh 的自動導航「剛剛停止」，猜測是抵達目的地（不是被打斷／重新規劃）。
/// </summary>
/// <remarks>
/// 🔴 vnavmesh 用 Dalamud 的共享資料快取暴露一個 <c>bool[]</c>（tag: <c>vnav.PathIsRunning</c>），
/// 不需要對它加組件相依：<see cref="IDalamudPluginInterface.GetOrCreateData{T}"/> 名字跟型別對上，
/// 就能拿到同一份陣列。vnavmesh 沒裝的話我們自己創一份恆為 <c>false</c> 的，永遠不會觸發
/// （fail-closed：沒有來源就不通知，不是報錯）。
/// <para>
/// 🔴 <b>單純看 true→false 邊緣會誤判</b>：呼叫端（Questionable 之類）在路徑卡住時常常
/// Stop() 再立刻重新規劃、重新 Move()，這個瞬間也會讓旗標閃過 false 再彈回 true。
/// 所以停下來後要先等一段緩衝時間（預設 1.5 秒，可調），緩衝期間又變回 true 就當作只是
/// 重新規劃，取消這次判定，不要照樣通知。
/// </para>
/// <para>
/// 📌 跟 <see cref="ApproachingMapFlag"/>（「到旗標」）是兩個不同的情境，刻意的：
/// 到旗標需要玩家自己在地圖上插旗標；這個看的是 vnavmesh 本身的導航狀態，不管
/// 目的地是旗標、Questionable 的任務座標，還是其他外掛丟給 vnavmesh 的座標，都算。
/// </para>
/// </remarks>
internal sealed class Arrived : IDisposable
{
    private const string SharedPathRunningTag = "vnav.PathIsRunning";

    private NotificationMaster p;
    private bool[] sharedPathIsRunning;
    private bool wasRunning;
    private DateTime? stoppedAtUtc;

    public Arrived(NotificationMaster plugin)
    {
        p = plugin;
        sharedPathIsRunning = Svc.PluginInterface.GetOrCreateData<bool[]>(SharedPathRunningTag, () => [false]);
        Svc.Framework.Update += Watcher;
    }

    public void Dispose()
    {
        Svc.Framework.Update -= Watcher;
    }

    private void Watcher(object _)
    {
        if(p.PauseUntil > Environment.TickCount64
            || (Utils.IsApplicationActivated && !p.cfg.arrived_AlwaysExecute)
            || Svc.Objects.LocalPlayer == null)
        {
            wasRunning = false;
            stoppedAtUtc = null;
            return;
        }

        var running = sharedPathIsRunning is { Length: > 0 } && sharedPathIsRunning[0];

        if(running)
        {
            wasRunning = true;
            stoppedAtUtc = null;
            return;
        }

        if(!wasRunning) return; // 本來就沒在跑，沒有「剛停下來」這回事

        if(Svc.Condition[ConditionFlag.BetweenAreas] || Svc.Condition[ConditionFlag.BetweenAreas51])
        {
            // 讀秒的載入過場中途：不當作抵達，緩衝也重算，等過場結束後從頭數
            stoppedAtUtc = null;
            return;
        }

        stoppedAtUtc ??= DateTime.UtcNow;

        if((DateTime.UtcNow - stoppedAtUtc.Value).TotalSeconds >= p.cfg.arrived_DebounceSeconds)
        {
            wasRunning = false;
            stoppedAtUtc = null;
            PluginLog.Debug("Path stopped and stayed stopped past debounce, treating as arrival");
            DoNotify();
        }
    }

    private void DoNotify()
    {
        if(p.cfg.arrived_TataruPraise) TataruPraiseBridge.Praise(TataruPraiseBridge.CategoryArrived);
        if(p.cfg.arrived_FlashTrayIcon) Native.Impl.FlashWindow();
        if(p.cfg.arrived_AutoActivateWindow) Native.Impl.Activate();
        if(p.cfg.arrived_ShowToastNotification)
        {
            TrayIconManager.ShowToast("You have arrived at your destination!".Loc(), "");
        }
        if(p.cfg.arrived_HttpRequestsEnable)
        {
            p.httpMaster.DoRequests(p.cfg.arrived_HttpRequests, new string[][] { });
        }
        if(p.cfg.arrived_SoundSettings.PlaySound)
        {
            p.audioPlayer.Play(p.cfg.arrived_SoundSettings);
        }
    }

    internal static void Setup(bool enable, NotificationMaster p)
    {
        if(enable)
        {
            if(p.arrived == null)
            {
                p.arrived = new Arrived(p);
                PluginLog.Information("Enabling arrived module");
            }
            else
            {
                PluginLog.Information("arrived module already enabled");
            }
        }
        else
        {
            if(p.arrived != null)
            {
                p.arrived.Dispose();
                p.arrived = null;
                PluginLog.Information("Disabling arrived module");
            }
            else
            {
                PluginLog.Information("arrived module already disabled");
            }
        }
    }
}
