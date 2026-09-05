namespace NotificationMaster.Hub;

/// <summary>
/// 一個分類要扇出到哪些管道。
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>這個型別只在「使用者真的動過那一列」時才會進 JSON。</b>
/// 路由表是 <c>Dictionary&lt;string, HubRoute&gt;</c> 而且採「<b>缺鍵＝預設路由</b>」語意
/// （<see cref="DefaultFor"/>），跟 TataruPraise 的 <c>IsCategoryEnabled</c> 同一招。
/// </para>
/// <para>
/// 🔴 這一點是<b>必要的</b>，不是風格問題：NotificationMaster 用 ECommons 的 <c>EzConfig</c>，
/// 而 <c>Configuration</c> <b>沒有</b>標 <c>[IgnoreDefaultValue]</c> ⇒
/// <c>DefaultSerializationFactory</c> 走 <c>DefaultValueHandling.Include</c>，
/// <b>連 <c>false</c> 都會寫進 JSON</b>；反序列化又是 <c>ObjectCreationHandling.Replace</c>。
/// 所以如果把路由寫成一堆 <c>hub_XxxTray</c> 布林欄位，
/// 使用者現有的 <c>DefaultConfig.json</c> 會在<b>下一次存檔</b>把它們全部釘死成當下的值，
/// 之後我們再改預設<b>只有全新安裝的人吃得到</b>，而且失敗是靜默的。
/// 空字典寫進 JSON 是 <c>{}</c>，讀回來還是空字典 ⇒ 預設仍然由碼決定。
/// </para>
/// <para>
/// ⚠️ 代價：之後<b>新增</b>管道欄位時，已經自訂過的那幾列會拿到 C# 欄位初值（<c>false</c>），
/// 也就是「新管道對自訂過的人預設關」。這是刻意選的那一邊——寧可少響，不要突然多一種聲音。
/// </para>
/// </remarks>
[Serializable]
internal class HubRoute
{
    /// <summary>Windows 系統匣氣球通知。</summary>
    public bool Tray = false;

    /// <summary>工作列圖示閃爍。</summary>
    public bool Flash = false;

    /// <summary>播放樞紐音效（<see cref="Configuration.hub_SoundSettings"/>）。</summary>
    public bool Sound = false;

    /// <summary>印一行到遊戲聊天視窗。</summary>
    public bool Chat = false;

    /// <summary>送樞紐的 HTTP webhook（<see cref="Configuration.hub_HttpRequests"/>）。</summary>
    public bool Http = false;

    /// <summary>請 TataruPraise 用語音念一句（分類鍵直接轉過去）。</summary>
    public bool Voice = false;

    /// <summary>
    /// 把遊戲視窗帶到前景。
    /// </summary>
    /// <remarks>
    /// ⚠️ 這是<b>視窗</b>動作，不是遊戲動作：不送任何封包、不觸發任何遊戲內行為。
    /// 預設一律關——搶前景很擾人，而且原本就不太可靠。
    /// </remarks>
    public bool Activate = false;

    /// <summary>
    /// 遊戲在<b>前景</b>時也照樣執行上面那些管道。
    /// </summary>
    /// <remarks>
    /// 📌 <c>false</c>（預設）＝只有遊戲在背景時才通知。
    /// ⚠️ <see cref="Voice"/> <b>不受這個開關管</b>：語音的用途本來就包含「人在畫面前但沒在看」，
    /// 這也跟 NotificationMaster 既有模組的 <c>*_TataruPraise</c> 行為一致。
    /// </remarks>
    public bool AlwaysExecute = false;

    /// <summary>至少有一個管道是開的。</summary>
    internal bool Any => Tray || Flash || Sound || Chat || Http || Voice || Activate;

    /// <summary>
    /// 使用者還沒動過這一列時要用的路由。
    /// </summary>
    /// <remarks>
    /// 🔴 <b>預設一律不含 <see cref="Activate"/>、<see cref="Http"/>、<see cref="Chat"/>、
    /// <see cref="Sound"/></b>：前兩個會影響使用者的桌面／對外送出資料，後兩個需要使用者先設定內容
    /// （音檔路徑、webhook）——沒設定就開等於每次都失敗一次寫一行錯誤。
    /// <para>
    /// 📌 緊急度只在這裡起作用，而且<b>只在使用者沒設定過的時候</b>。
    /// </para>
    /// </remarks>
    internal static HubRoute DefaultFor(int urgency) => new()
    {
        Tray = true,
        // 低緊急度不搶工作列：那個閃爍會一直閃到視窗被點開為止（FLASHW_TIMERNOFG）。
        Flash = urgency >= HubContract.Urgency.Normal,
        Voice = true,
        // 高緊急度＝「需要人過來處理」，人就算在畫面前也可能沒在看。
        AlwaysExecute = urgency >= HubContract.Urgency.High,
    };

    /// <summary>複製一份（設定視窗要把預設值具體化成一列時用）。</summary>
    internal HubRoute Clone() => new()
    {
        Tray = Tray,
        Flash = Flash,
        Sound = Sound,
        Chat = Chat,
        Http = Http,
        Voice = Voice,
        Activate = Activate,
        AlwaysExecute = AlwaysExecute,
    };
}
