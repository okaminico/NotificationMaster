using NotificationMaster.Hub;

namespace NotificationMaster;

/// <summary>
/// 通知樞紐的設定分頁：一張「事件 × 管道」的勾選矩陣。
/// </summary>
/// <remarks>
/// 🔴 <b>併進既有的設定分頁列，刻意不開新視窗</b>：使用者要找「某某事件會不會響」時，
/// 應該只有一個地方要找。
/// <para>
/// 🔴 所有對 <c>hub_Routes</c> 的讀寫都在 <see cref="NotificationHub.Gate"/> 裡：
/// 這裡跑在主執行緒，而 IPC 呼叫端可能從<b>它自己的背景執行緒</b>讀同一個字典。
/// </para>
/// </remarks>
internal partial class ConfigGui
{
    /// <summary>矩陣的欄。順序即畫面上的欄順序。</summary>
    /// <remarks>
    /// 📌 每一欄是 (標題, 說明, 讀, 寫)。用委派而不是反射：欄位名打錯會是編譯錯誤，不是靜默失效。
    /// </remarks>
    private static readonly (string Head, string Tip, Func<HubRoute, bool> Get, Action<HubRoute, bool> Set)[] HubColumns =
    [
        ("匣", "Windows 系統匣的氣球通知。",
            r => r.Tray, (r, v) => r.Tray = v),
        ("閃", "工作列圖示閃爍，直到你把遊戲視窗點回來為止。",
            r => r.Flash, (r, v) => r.Flash = v),
        ("音", "播放下面設定的樞紐音效。沒設定音檔的話這欄勾了也不會有聲音。",
            r => r.Sound, (r, v) => r.Sound = v),
        ("聊", "在遊戲的聊天視窗印一行。",
            r => r.Chat, (r, v) => r.Chat = v),
        ("網", "送下面設定的 HTTP webhook（Discord、手機推播之類）。",
            r => r.Http, (r, v) => r.Http = v),
        ("語", "請 TataruPraise 用語音念一句。\n沒安裝 TataruPraise、或它關著、或這個情境沒有語音時會安靜地跳過。\n⚠️ 這一欄不受「前景時也執行」影響——語音本來就是給「人在畫面前但沒在看」用的。",
            r => r.Voice, (r, v) => r.Voice = v),
        ("前", "把遊戲視窗搶到最前面。\n⚠️ 很擾人，而且本來就不太可靠。預設一律關。",
            r => r.Activate, (r, v) => r.Activate = v),
        ("恆", "遊戲視窗在前景時也照樣執行上面那些管道。\n不勾＝只有遊戲在背景時才通知。",
            r => r.AlwaysExecute, (r, v) => r.AlwaysExecute = v),
    ];

    internal void DrawHubConfig()
    {
        ImGui.TextWrapped("通知樞紐讓「別的外掛」把它們的事件送進來，由這張表決定要用哪些管道通知你。");
        ImGui.TextWrapped("這一頁不影響上面那些分頁——NotificationMaster 自己的通知還是各自照原本的設定走。");
        ImGui.Separator();

        ImGui.Checkbox("啟用通知樞紐", ref p.cfg.hub_Enable);
        Static.ImGuiTextTooltip(
            "關掉之後，別的外掛送進來的通知一律不處理（它們自己不會出錯，只是這裡不響）。\n"
            + "只想關掉某個事件的話，用下面的矩陣把那一列全部取消勾選就好。");

        if(!p.cfg.hub_Enable) return;

        ImGui.SetNextItemWidth(140f);
        ImGui.InputInt("同一事件的最短間隔（毫秒）", ref p.cfg.hub_ThrottleMs, 250, 1000);
        if(p.cfg.hub_ThrottleMs < 0) p.cfg.hub_ThrottleMs = 0;
        Static.ImGuiTextTooltip(
            "同一個外掛、同一個事件，兩次通知之間至少要隔這麼久。0＝不限制。\n"
            + "這是防呆用的地板，不是「同一件事只響一次」的機制——後者由送通知的外掛自己負責。");

        ImGui.Separator();
        DrawHubMatrix();

        ImGui.Separator();
        ImGui.TextWrapped("下面兩項是所有事件共用的（在矩陣上勾「音」或「網」才會用到）：");
        DrawSoundSettings(ref p.cfg.hub_SoundSettings);
        DrawHttpMaster(p.cfg.hub_HttpRequests, ref p.cfg.hub_HttpRequestsEnable,
            "<caller> - 送通知的外掛\n<category> - 事件分類\n<title> - 標題\n<body> - 內容");
    }

    private void DrawHubMatrix()
    {
        var categories = NotificationHub.KnownCategories();

        if(ImGui.Button("全部恢復預設"))
        {
            // 🔴 清空字典＝回到「缺鍵＝預設路由」。刻意不是「把每一列寫成預設值」——
            //    那樣會把 33 列全部釘進使用者的設定檔，之後我們再也改不動預設值。
            lock(NotificationHub.Gate) p.cfg.hub_Routes.Clear();
            p.cfg.Save();
        }
        Static.ImGuiTextTooltip(
            "把整張表清回預設。之後我們如果調整了預設路由，你也會跟著拿到新的。\n"
            + "（只要你動過某一列，那一列就會被記進設定檔，以後就固定照你設的走。）");

        ImGui.SameLine();
        var customised = 0;
        lock(NotificationHub.Gate) customised = p.cfg.hub_Routes.Count;
        ImGui.TextDisabled($"（{categories.Count} 個事件，其中 {customised} 個你自訂過）");

        const ImGuiTableFlags flags = ImGuiTableFlags.Borders
            | ImGuiTableFlags.RowBg
            | ImGuiTableFlags.SizingFixedFit
            | ImGuiTableFlags.ScrollY;

        // 高度留給下面的音效/HTTP 區塊；矩陣自己捲。
        var height = Math.Min(ImGui.GetContentRegionAvail().Y - 60f, 420f);
        if(height < 120f) height = 120f;

        if(!ImGui.BeginTable("##NMHubMatrix", HubColumns.Length + 2, flags, new Vector2(0f, height))) return;

        ImGui.TableSetupScrollFreeze(1, 1);
        ImGui.TableSetupColumn("事件", ImGuiTableColumnFlags.WidthStretch);
        foreach(var col in HubColumns) ImGui.TableSetupColumn(col.Head, ImGuiTableColumnFlags.WidthFixed, 28f);
        ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 24f);

        // 自己畫表頭：要在每個欄名上掛 tooltip，TableHeadersRow 做不到。
        ImGui.TableNextRow(ImGuiTableRowFlags.Headers);
        ImGui.TableSetColumnIndex(0);
        ImGui.TableHeader("事件");
        for(var i = 0; i < HubColumns.Length; i++)
        {
            ImGui.TableSetColumnIndex(i + 1);
            ImGui.TableHeader(HubColumns[i].Head);
            Static.ImGuiTextTooltip(HubColumns[i].Tip);
        }
        ImGui.TableSetColumnIndex(HubColumns.Length + 1);
        ImGui.TableHeader("");

        foreach(var category in categories)
        {
            DrawHubRow(category);
        }

        ImGui.EndTable();
    }

    private void DrawHubRow(string category)
    {
        // 🔴 讀取與「這一列是不是自訂的」必須是同一次鎖內的觀察，
        //    否則畫出來的勾勾可能來自預設值、旁邊的標記卻說是自訂。
        HubRoute shown;
        bool custom;
        lock(NotificationHub.Gate)
        {
            custom = p.cfg.hub_Routes.TryGetValue(category, out var stored) && stored != null;
            shown = custom ? stored.Clone() : HubRoute.DefaultFor(HubContract.Urgency.Normal);
        }

        ImGui.TableNextRow();
        ImGui.TableSetColumnIndex(0);
        ImGui.TextUnformatted(category);
        if(!custom)
        {
            // 「不知道／還沒設定」要在列上看得見，不能只藏在 tooltip 裡。
            ImGui.SameLine();
            ImGui.TextDisabled("(預設)");
            Static.ImGuiTextTooltip(
                "這一列還沒被你動過，走的是內建預設路由。\n"
                + "送通知的外掛可以把事件標成比較緊急，那時預設路由會不一樣——\n"
                + "但你只要動過這一列，就完全照你設的走。");
        }

        var changed = false;
        for(var i = 0; i < HubColumns.Length; i++)
        {
            ImGui.TableSetColumnIndex(i + 1);
            var value = HubColumns[i].Get(shown);
            // id 要帶分類名，否則同一欄的 33 個核取方塊會共用 id 而互相干擾。
            if(ImGui.Checkbox($"##{category}-{i}", ref value))
            {
                HubColumns[i].Set(shown, value);
                changed = true;
            }
            Static.ImGuiTextTooltip($"{category} - {HubColumns[i].Tip}");
        }

        ImGui.TableSetColumnIndex(HubColumns.Length + 1);
        if(custom)
        {
            ImGui.PushID($"##reset-{category}");
            if(Static.ImGuiIconButton(FontAwesomeIcon.UndoAlt, $"把「{category}」清回預設。"))
            {
                lock(NotificationHub.Gate) p.cfg.hub_Routes.Remove(category);
                p.cfg.Save();
            }
            ImGui.PopID();
        }

        if(changed)
        {
            // 🔴 一動就把整列寫進字典（含沒被改到的欄），因為「缺鍵」的語意是「用預設」，
            //    沒辦法只記住其中一格。
            lock(NotificationHub.Gate) p.cfg.hub_Routes[category] = shown;
            p.cfg.Save();
        }
    }
}
