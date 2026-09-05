using Dalamud.Plugin.Ipc;

namespace NotificationMaster.Hub;

/// <summary>
/// 分類感知的通知樞紐：別的外掛送一則「分類 ＋ 標題 ＋ 內容」進來，
/// 由<b>使用者在 NotificationMaster 一處設定的路由表</b>決定扇出到哪些管道。
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>樞紐只服務外部呼叫端。</b> NotificationMaster 自己那 13 個 notificator
/// （GP／過場／聊天／副本排到／連線錯誤／地圖旗標／小怪／招募／釣魚／任務開始／準備確認／
/// 隊友過場／戰鬥倒數）<b>刻意不改走這裡</b>：它們各自的設定是使用者已經調好的，
/// 改走樞紐等於把那些設定靜默作廢。
/// </para>
/// <para>
/// 🔴 <b>零自動化。</b> 這裡只會：畫氣球、閃工作列、播音效、印聊天、送 HTTP、叫語音、
/// 帶視窗到前景。<b>不會</b>觸發任何遊戲內動作，也不提供任何「按一下就做某件事」的路徑。
/// </para>
/// <para>
/// 🔴 IPC 的實作跑在<b>呼叫端的執行緒</b>上。路由查詢在鎖裡同步做完（要拿來當回傳值），
/// 真正的扇出一律丟到 framework 執行緒——系統匣氣球（WinForms ＋ <c>TickScheduler</c>）
/// 與 <c>Svc.Chat.Print</c> 都不能在別人的背景執行緒上跑。
/// </para>
/// </remarks>
internal sealed class NotificationHub : IDisposable
{
    private readonly ICallGateProvider<string, string, string, string, int, bool> notify;
    private readonly ICallGateProvider<string, bool> isRouted;

    /// <summary>
    /// 保護 <see cref="Configuration.hub_Routes"/> 與 <see cref="seenCategories"/>。
    /// </summary>
    /// <remarks>
    /// 🔴 必要：設定視窗在<b>主執行緒</b>改字典，而 IPC 呼叫端可能從<b>背景執行緒</b>讀它。
    /// <c>Dictionary</c> 在讀寫並行下不是「拿到舊值」而是可能整個壞掉。
    /// 設定視窗那邊也走同一把鎖（見 <c>HubGui</c>）。
    /// </remarks>
    internal static readonly object Gate = new();

    /// <summary>
    /// 這次遊戲執行期間<b>實際被呼叫過</b>的分類。
    /// </summary>
    /// <remarks>
    /// 📌 用途：讓設定矩陣顯示出 <see cref="HubCategories.All"/> 以外的分類。
    /// 樞紐<b>不拒絕</b>未知分類——拒絕的話，新外掛送新分類會靜默不通知，
    /// 而使用者在 UI 上連「有這個東西」都看不到。
    /// </remarks>
    private static readonly SortedSet<string> seenCategories = new(StringComparer.Ordinal);

    /// <summary>（外掛, 分類）→ 這個時間點（<c>Environment.TickCount64</c>）之前不再通知。</summary>
    /// <remarks>
    /// 🔴 <b>刻意不用 ECommons 的 <c>EzThrottler</c>。</b> 它背後是一個<b>沒有上鎖</b>的
    /// <c>Dictionary&lt;string, long&gt;</c>，而且是整個外掛<b>共用</b>的靜態實例——
    /// NotificationMaster 自己那些 notificator 都在 framework 執行緒上用它。
    /// 樞紐是<b>公開的 IPC 端點</b>，會被呼叫端從它自己的背景執行緒叫進來；
    /// 兩邊同時對同一個字典做插入的失敗形式不是「拿到舊值」，而是<b>字典本身壞掉</b>
    /// （擴容期間的競態可以讓後續查詢無限迴圈）。
    /// <para>
    /// 📌 順帶一提，<c>EzThrottler</c> 的 key 是<b>全域且永久</b>的：
    /// 用它的話這裡的 key 還會一直留在那個共用字典裡。
    /// </para>
    /// <para>
    /// 📌 key 用 ValueTuple 而不是把兩個字串接起來：接字串就要選一個分隔字元，
    /// 而任何分隔字元都可能出現在外掛名或分類名裡（撞到就是兩個不同事件共用一個節流）。
    /// </para>
    /// </remarks>
    private static readonly Dictionary<(string Caller, string Category), long> throttleUntil = [];

    internal NotificationHub()
    {
        notify = Svc.PluginInterface.GetIpcProvider<string, string, string, string, int, bool>(HubContract.Notify);
        isRouted = Svc.PluginInterface.GetIpcProvider<string, bool>(HubContract.IsRouted);

        notify.RegisterFunc((caller, category, title, body, urgency) =>
        {
            try
            {
                return Dispatch(caller, category, title, body, urgency);
            }
            catch(Exception e)
            {
                // 🔴 絕不讓例外冒回呼叫端：這是<b>對方的執行緒</b>，多半還是對方的 framework tick。
                PluginLog.Information($"[通知樞紐] 處理 {caller} 的「{category}」時失敗：{e.Message}");
                return false;
            }
        });

        isRouted.RegisterFunc(category =>
        {
            try
            {
                if(P?.cfg == null) return false;
                Note(category);
                return RouteOf(category, HubContract.Urgency.Normal).Any;
            }
            catch(Exception e)
            {
                PluginLog.Information($"[通知樞紐] 查詢「{category}」路由時失敗：{e.Message}");
                return false;
            }
        });

        PluginLog.Information($"[通知樞紐] IPC 已註冊：{HubContract.Notify}、{HubContract.IsRouted}");
    }

    /// <summary>設定矩陣要畫哪些列（已知分類 ＋ 這次跑起來被叫過的 ＋ 使用者設定過的）。</summary>
    internal static List<string> KnownCategories()
    {
        var result = new List<string>(HubCategories.All);
        var have = new HashSet<string>(result, StringComparer.Ordinal);
        lock(Gate)
        {
            foreach(var c in seenCategories)
            {
                if(have.Add(c)) result.Add(c);
            }
            if(P?.cfg?.hub_Routes != null)
            {
                foreach(var c in P.cfg.hub_Routes.Keys)
                {
                    if(have.Add(c)) result.Add(c);
                }
            }
        }
        return result;
    }

    private static void Note(string category)
    {
        if(string.IsNullOrEmpty(category)) return;
        lock(Gate) seenCategories.Add(category);
    }

    /// <summary>
    /// 這個分類現在的路由。<b>缺鍵＝預設路由</b>（見 <see cref="HubRoute"/> 的說明）。
    /// </summary>
    /// <remarks>🔴 呼叫端必須自己在 <see cref="Gate"/> 裡呼叫，或接受它就是在鎖裡被呼叫的。</remarks>
    internal static HubRoute RouteOf(string category, int urgency)
    {
        var routes = P?.cfg?.hub_Routes;
        if(routes != null && category != null && routes.TryGetValue(category, out var route) && route != null)
        {
            return route;
        }
        return HubRoute.DefaultFor(urgency);
    }

    private static bool Dispatch(string caller, string category, string title, string body, int urgency)
    {
        if(P == null || P.IsDisposed || P.cfg == null) return false;

        caller = caller.NotNull();
        category = category.NotNull();
        title = title.NotNull();
        body = body.NotNull();

        if(category.Length == 0)
        {
            PluginLog.Information($"[通知樞紐] {caller} 送了空的分類，忽略。");
            return false;
        }

        Note(category);

        // /pnotify shutup 是使用者明確說「現在別吵我」，樞紐一樣要聽。
        if(P.PauseUntil > Environment.TickCount64) return false;

        if(!P.cfg.hub_Enable) return false;

        HubRoute route;
        lock(Gate) route = RouteOf(category, urgency).Clone();

        if(!route.Any) return false;

        // 🔴 節流是<b>地板</b>不是策略：真正「同一件事只叫一次」要由呼叫端在狀態邊緣上做。
        //    這裡只保證呼叫端寫錯（放進輪詢迴圈）時不會把系統匣氣球洗爆。
        //    📌 第一次一定放行，見 PassesThrottle。
        var throttle = P.cfg.hub_ThrottleMs;
        if(throttle > 0 && !PassesThrottle(caller, category, throttle))
        {
            PluginLog.Debug($"[通知樞紐] {caller} 的「{category}」在 {throttle}ms 節流內，略過。");
            return false;
        }

        // 語音不跟前景抑制走：人在畫面前但沒在看，正是語音存在的理由。
        // （這跟 NotificationMaster 既有模組的 *_TataruPraise 行為一致。）
        var voiced = false;
        if(route.Voice)
        {
            TataruPraiseBridge.Praise(category);
            voiced = true;
        }

        var foreground = Utils.IsApplicationActivated;
        if(foreground && !route.AlwaysExecute)
        {
            PluginLog.Debug($"[通知樞紐] {caller} 的「{category}」：遊戲在前景且未勾「前景時也執行」，只做語音。");
            return voiced;
        }

        // 🔴 扇出一律回主執行緒：系統匣氣球走 WinForms ＋ TickScheduler，Svc.Chat.Print 也要主執行緒。
        //    Task 自己吞掉例外，所以不會有 unobserved faulted task。
        _ = Svc.Framework.RunOnFrameworkThread(() => FanOut(caller, category, title, body, route));

        PluginLog.Information(
            $"[通知樞紐] {caller} 的「{category}」已排出："
            + $"匣={route.Tray} 閃={route.Flash} 音={route.Sound} 聊={route.Chat} "
            + $"HTTP={route.Http} 語音={route.Voice} 前景={route.Activate}");
        return true;
    }

    private static void FanOut(string caller, string category, string title, string body, HubRoute route)
    {
        // 排程過來的時候外掛可能已經在卸載途中了。
        if(P == null || P.IsDisposed || P.cfg == null) return;

        // 🔴 每個管道各自 try/catch：一個管道壞掉不可以讓後面的管道全部不執行。
        if(route.Flash) Try(caller, "工作列閃爍", static () => Native.Impl.FlashWindow());

        if(route.Tray)
        {
            Try(caller, "系統匣通知", () => TrayIconManager.ShowToast(
                body.Length > 0 ? body : category,
                title.Length > 0 ? title : $"NotificationMaster - {category}"));
        }

        if(route.Activate) Try(caller, "帶到前景", static () => Native.Impl.Activate());

        if(route.Chat)
        {
            Try(caller, "聊天訊息", () =>
            {
                var line = title.Length > 0 && body.Length > 0 ? $"{title}：{body}"
                    : title.Length > 0 ? title
                    : body.Length > 0 ? body
                    : category;
                Svc.Chat.Print($"[{(caller.Length > 0 ? caller : "通知")}] {line}");
            });
        }

        if(route.Sound && P.cfg.hub_SoundSettings.PlaySound)
        {
            Try(caller, "音效", () => P.audioPlayer.Play(P.cfg.hub_SoundSettings));
        }

        if(route.Http && P.cfg.hub_HttpRequestsEnable && P.cfg.hub_HttpRequests.Count > 0)
        {
            Try(caller, "HTTP", () => P.httpMaster.DoRequests(P.cfg.hub_HttpRequests,
            [
                ["<caller>", caller],
                ["<category>", category],
                ["<title>", title],
                ["<body>", body],
            ]));
        }
    }

    /// <summary>這個「外掛＋分類」現在放不放行；放行的話同時把下次可通知的時間往後推。</summary>
    private static bool PassesThrottle(string caller, string category, int throttleMs)
    {
        var now = Environment.TickCount64;
        lock(Gate)
        {
            // 📌 第一次一定放行（字典裡還沒有這個鍵）——通知要的正是這個行為。
            if(throttleUntil.TryGetValue((caller, category), out var until) && now < until) return false;
            throttleUntil[(caller, category)] = now + throttleMs;
            return true;
        }
    }

    private static void Try(string caller, string what, Action action)
    {
        try
        {
            action();
        }
        catch(Exception e)
        {
            PluginLog.Information($"[通知樞紐] {caller} 的「{what}」管道失敗：{e.Message}");
        }
    }

    public void Dispose()
    {
        // 🔴 註銷不做任何前置條件判斷（艦隊踩過「Dispose 裡的 IPC 沒防護」的坑）；
        //    兩個欄位都在建構子裡直接指派，不會是 null，例外照樣個別吞掉。
        try { notify.UnregisterFunc(); }
        catch(Exception e) { PluginLog.Information($"[通知樞紐] 註銷 {HubContract.Notify} 失敗：{e.Message}"); }
        try { isRouted.UnregisterFunc(); }
        catch(Exception e) { PluginLog.Information($"[通知樞紐] 註銷 {HubContract.IsRouted} 失敗：{e.Message}"); }
    }
}
