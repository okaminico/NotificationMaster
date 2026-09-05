namespace NotificationMaster.Hub;

/// <summary>
/// 樞紐的分類鍵。
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>權威來源是 TataruPraise 的 <c>Core/PraiseCategory.cs</c>，這裡是逐字複製。</b>
/// 刻意<b>不</b>加組件相依（不引 TataruPraise 的 dll），理由跟艦隊既有的 10 份
/// TataruPraise wrapper 一樣：兩邊裝／移除任一方永遠不會弄壞另一邊。
/// 代價是這份清單要手動跟上——所以樞紐<b>不會</b>拒絕不在這份清單裡的分類
/// （見 <see cref="NotificationHub"/> 的「見過的分類」機制），這份清單只決定
/// <b>設定矩陣一開始長什麼樣</b>。
/// </para>
/// <para>
/// 📌 不另造一套分類鍵是刻意的：艦隊裡已經有 10 個外掛在用這組字串叫 TataruPraise，
/// 樞紐要能把「語音」當成其中一個管道扇出去，兩邊的鍵就必須是同一組。
/// 造第二套的話，同一個事件在兩個地方要設定兩次。
/// </para>
/// <para>
/// ⚠️ 順序即設定矩陣上的顯示順序（與 TataruPraise 的 <c>PraiseCategory.All</c> 一致）。
/// </para>
/// <para>
/// 🔴 <b>驗這份清單有沒有跟權威分岔，用現成的稽核工具，不要用眼睛看</b>
/// （打錯字的失敗形式是「語音那條管道靜默不出聲」，跟「使用者把這個情境關掉了」分不出來）：
/// <code>
/// python C:/Users/lother/.claude/tools/fleet/tataru_category_audit.py
/// </code>
/// 📌 <b>2026-09-04 起 <c>--extra</c> 不必再給。</b> 那支工具原本只用「檔名含 tatarupraise」找消費端，
/// 而這個檔名不含 —— 於是它<b>完全不看這個檔，還回報一切正常</b>（假陰性）。現在改成三條軸取聯集：
/// 檔名、剝註解後含頻道字面前綴、以及<b>宣告的 <c>const string</c> 值命中權威情境 ≥3 個</b>。
/// 本檔是靠第三條被找到的（它宣告的 33 個值全是權威情境）。
/// 🔑 門檻用「≥3 個命中」而不是「全部命中」是刻意的：若本檔有 26 個正確 ＋ 1 個錯字，
/// 「≥3」照樣把它納入、那個錯字才會被報成 UNKNOWN；改成全等比對<b>正好會把要抓的東西濾掉</b>。
/// </para>
/// </remarks>
internal static class HubCategories
{
    internal const string DutyComplete = "副本完成";
    internal const string LevelUp = "升等";
    internal const string Login = "登入";
    internal const string GilMilestone = "Gil里程碑";
    internal const string Submarine = "潛艇";
    internal const string Retainer = "僱員";
    internal const string ExpertDelivery = "稀有品";
    internal const string Market = "市場";
    internal const string Crafting = "製作";
    internal const string Cosmic = "宇宙";
    internal const string LowHp = "血量低";
    internal const string MarkedByMany = "被大量敵人標記";
    internal const string EnemyBehind = "敵人從後面來";
    internal const string DutyStart = "任務開始";
    internal const string ReadyCheck = "準備確認";
    internal const string CutsceneEnd = "過場結束";
    internal const string DutyPop = "副本排到";
    internal const string FlagArrived = "到旗標";
    internal const string Tell = "私訊";
    internal const string Arrived = "抵達";
    internal const string Jackpot = "中獎";
    internal const string NeedHelp = "需要幫忙";
    internal const string PlayerAlert = "玩家警示";
    internal const string BeingWatched = "被盯著";
    internal const string TellReceived = "被密語";
    internal const string PartyInvite = "組隊邀請";
    internal const string TradeRequest = "交易請求";
    internal const string DutyRunStopped = "跑本停止";
    internal const string GatherStopped = "採集停止";
    internal const string RareFish = "稀有魚";
    internal const string HuntFound = "發現魔物";
    internal const string BagAlmostFull = "背包快滿";
    internal const string DailyReset = "每日重置";

    /// <summary>已知分類，順序即矩陣上的顯示順序。</summary>
    internal static readonly string[] All =
    [
        DutyComplete,
        LevelUp,
        Login,
        GilMilestone,
        Submarine,
        Retainer,
        ExpertDelivery,
        Market,
        Crafting,
        Cosmic,
        LowHp,
        MarkedByMany,
        EnemyBehind,
        DutyStart,
        ReadyCheck,
        CutsceneEnd,
        DutyPop,
        FlagArrived,
        Tell,
        Arrived,
        Jackpot,
        NeedHelp,
        PlayerAlert,
        BeingWatched,
        TellReceived,
        PartyInvite,
        TradeRequest,
        DutyRunStopped,
        GatherStopped,
        RareFish,
        HuntFound,
        BagAlmostFull,
        DailyReset,
    ];
}
