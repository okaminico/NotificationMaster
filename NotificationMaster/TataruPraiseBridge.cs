using Dalamud.Plugin.Ipc.Exceptions;

namespace NotificationMaster;

/// <summary>
/// 通知事件發生時，順便請 TataruPraise（塔塔露誇獎）用語音念一句。
/// </summary>
/// <remarks>
/// 🔴 契約名與情境名都是<b>純字串比對</b>：Dalamud 的 CallGate 找不到名字時不會報錯，
/// 呼叫端只會拿到「沒有人註冊」；TataruPraise 收到不認得的情境名也只是回 false。
/// 兩種失敗都是<b>靜默</b>的，所以名字逐字寫成常數，不要在呼叫點散落字面值。
/// <para>
/// 📌 這裡刻意<b>不加任何組件相依</b>（不引 TataruPraise 的 dll、不引新的 NuGet）：
/// 只用 Dalamud 原生的 <c>GetIpcSubscriber</c>，對方沒安裝時這個檔完全是死碼。
/// </para>
/// <para>
/// 📌 冷卻由 TataruPraise 那邊做（逐情境 5 秒），這裡不再自己節流；
/// 呼叫點只負責「同一件事只叫一次」。
/// </para>
/// </remarks>
internal static class TataruPraiseBridge
{
    /// <summary>總開關開著而且池裡真的有可播的語音。<c>Func&lt;bool&gt;</c>。</summary>
    internal const string IpcIsAvailable = "TataruPraise.IsAvailable";

    /// <summary>從指定情境的誇獎池挑一句念。<c>Func&lt;string, bool&gt;</c>。</summary>
    internal const string IpcPraise = "TataruPraise.Praise";

    /// <summary>副本／任務正式開始（開場倒數結束）。</summary>
    internal const string CategoryDutyStart = "任務開始";

    /// <summary>隊伍發起準備確認。</summary>
    internal const string CategoryReadyCheck = "準備確認";

    /// <summary>隊友看完過場動畫。</summary>
    internal const string CategoryCutsceneEnded = "過場結束";

    /// <summary>排到副本（Duty Finder 跳出邀請）。</summary>
    internal const string CategoryCfPop = "副本排到";

    /// <summary>走到地圖旗標附近。</summary>
    internal const string CategoryMapFlag = "到旗標";

    /// <summary>命中聊天規則（關鍵字／私訊）。</summary>
    internal const string CategoryChatMessage = "私訊";

    /// <summary>vnavmesh 自動導航剛結束（判定為抵達目的地，非中途被打斷重新規劃）。</summary>
    internal const string CategoryArrived = "抵達";

    /// <summary>
    /// 請塔塔露念一句指定情境的話。對方沒安裝／沒開啟／池是空的都只是靜靜地什麼都不做。
    /// </summary>
    /// <remarks>
    /// 🔴 一律確保在 framework（主）執行緒上呼叫：IPC 的實作是跑在<b>呼叫端執行緒</b>上的，
    /// 而 NotificationMaster 有些通知是從聊天／addon 回呼發出來的。
    /// </remarks>
    internal static void Praise(string category)
    {
        try
        {
            if(Svc.Framework == null) return;
            if(Svc.Framework.IsInFrameworkUpdateThread)
            {
                Invoke(category);
            }
            else
            {
                // Invoke 自己吞掉所有例外，所以這個不被觀察的 Task 不會有 faulted 狀態。
                _ = Svc.Framework.RunOnFrameworkThread(() => Invoke(category));
            }
        }
        catch(Exception e)
        {
            e.Log();
        }
    }

    private static void Invoke(string category)
    {
        // 排程過來的時候外掛可能已經在卸載途中了。
        if(P == null || P.IsDisposed) return;
        try
        {
            // 刻意不快取 subscriber：一次任務也就叫個幾次，省下來的那點開銷不值得
            // 為「TataruPraise 中途重載」留一份可能過期的狀態。
            var available = Svc.PluginInterface.GetIpcSubscriber<bool>(IpcIsAvailable);
            if(!available.InvokeFunc()) return;

            var praise = Svc.PluginInterface.GetIpcSubscriber<string, bool>(IpcPraise);
            var queued = praise.InvokeFunc(category);
            PluginLog.Debug($"[TataruPraise] 情境「{category}」：{(queued ? "已排進播放" : "這次沒出聲")}");
        }
        catch(IpcNotReadyError)
        {
            // 對方沒安裝／還沒載入完——這是常態，不是錯誤，不要寫進記錄檔洗版。
        }
        catch(Exception e)
        {
            // 其他狀況（型別不合、對方內部炸了）只留一行，絕不讓例外往上冒到通知流程裡。
            PluginLog.Information($"[TataruPraise] 呼叫 {IpcPraise}(\"{category}\") 失敗：{e.Message}");
        }
    }
}
