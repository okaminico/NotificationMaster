namespace NotificationMaster.Hub;

/// <summary>
/// 「分類感知通知樞紐」的對外 IPC 契約。
/// </summary>
/// <remarks>
/// <para>
/// 🔴 <b>這些字串一旦發版就不能改，也不能同名改型別。</b> Dalamud 的 CallGate 是純字串比對：
/// 改名＝呼叫端靜默拿到「沒有人註冊」；<b>同名改型別更兇</b>——實測過
/// <c>Guid</c>→<c>string</c> 這種方向的轉換會<b>靜默成功</b>，而
/// <c>SafeWrapper.IPCException</c> 只攔 <c>IpcNotReadyError</c>、攔不住 <c>IpcTypeMismatchError</c>。
/// 要改形狀就<b>開新名字讓舊名字消失</b>。
/// </para>
/// <para>
/// 📌 這裡刻意<b>不</b>寫進 <c>NotificationMasterAPI</c>：那個組件對
/// AutoRetainer／Lifestream／Splatoon／DailyDuty／SubmarineTracker 而言是
/// <b>nuget.org 上游的 1.0.0.1</b>，不是本 org 的 fork。改我們這份 submodule 的話，
/// 那五個消費端拿不到（而且「只把方法改 public」這種改法是<b>靜默無效</b>的）。
/// ⇒ 消費端一律用裸 <c>GetIpcSubscriber</c> ＋ 逐字複製的常數，
/// 跟艦隊既有的 TataruPraise wrapper 同形狀：<b>裝／移除任一方永遠不會弄壞另一邊</b>。
/// </para>
/// </remarks>
internal static class HubContract
{
    /// <summary>
    /// 送一則分類通知進樞紐。
    /// <c>Func&lt;string caller, string category, string title, string body, int urgency, bool&gt;</c>
    /// </summary>
    /// <remarks>
    /// 回傳 <c>true</c>＝<b>至少有一個管道被排程出去</b>（不保證那個管道自己不出錯）；
    /// <c>false</c>＝這次什麼都沒做（外掛暫停中、路由全關、被節流、或前景抑制擋下）。
    /// <para>
    /// 🔴 回傳值<b>不可以</b>被呼叫端拿來決定要不要重試。它是給記錄檔用的。
    /// </para>
    /// <para>
    /// 🔴🔴 <b>接這個端點之前先確認自己沒有另一條 TataruPraise 的路。</b>
    /// 「語音」是路由表上的一個管道，而且預設是<b>開</b>的。
    /// 呼叫端如果已經有自己的 <c>TataruPraise.Praise</c> wrapper 在同一個事件上叫，
    /// 接上樞紐之後同一件事會<b>念兩次</b>——而且兩邊的開關長在不同的外掛裡，
    /// 使用者很難自己推出來是怎麼回事。
    /// 二選一：①把自己那條語音路拆掉、改由樞紐統一發
    /// ②不要用樞紐，只用 <c>NotificationMasterAPI</c> 碰系統匣與工作列
    /// （AutoRetainer 的僱員通知選的是②，理由寫在它的 <c>Modules/RetainerTrayNotify.cs</c>）。
    /// </para>
    /// </remarks>
    internal const string Notify = "NotificationMaster.Notify";

    /// <summary>
    /// 某個分類現在有沒有路到任何管道。<c>Func&lt;string category, bool&gt;</c>。
    /// </summary>
    /// <remarks>
    /// 給呼叫端在「組通知內容本身要花力氣」時先問一句用的。
    /// ⚠️ 這是<b>純設定查詢</b>：不看節流、不看前景抑制，所以回 true 之後
    /// <see cref="Notify"/> 仍然可能回 false。
    /// </remarks>
    internal const string IsRouted = "NotificationMaster.IsRouted";

    /// <summary>建議的緊急度。</summary>
    /// <remarks>
    /// 🔴 走 CallGate 的是 <c>int</c> 不是列舉：列舉跨 IPC 會被 JSON 來回轉，
    /// 而且呼叫端沒有我們的組件、只能自己寫字面數字。這裡的常數是<b>給我們自己看的</b>。
    /// <para>
    /// 📌 緊急度<b>只用來決定「使用者還沒設定過這個分類時的預設路由」</b>。
    /// 使用者一旦在矩陣上動過那一列，緊急度就完全不參與判斷——
    /// 呼叫端不能靠調高緊急度去蓋過使用者的選擇。
    /// </para>
    /// </remarks>
    internal static class Urgency
    {
        /// <summary>順帶一提。預設只出語音＋系統匣，不閃工作列。</summary>
        internal const int Low = 0;

        /// <summary>一般通知。預設系統匣＋工作列閃爍＋語音。</summary>
        internal const int Normal = 1;

        /// <summary>需要人過來處理。預設同上，而且<b>遊戲在前景時照樣通知</b>。</summary>
        internal const int High = 2;
    }
}
