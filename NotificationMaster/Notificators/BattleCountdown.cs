using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility;
using ECommons.Throttlers;
using Lumina.Excel.Sheets;
using System.Text.RegularExpressions;

namespace NotificationMaster;

/// <summary>
/// 隊伍中有人發起「戰鬥開始倒數」時通知（遊戲在背景時才會提醒，行為與其他 notificator 一致）。
/// 承接 DailyRoutines 的 AutoNotifyCountdown，但比對方式改成短錨而不是整句模板。
/// </summary>
internal class BattleCountdown : IDisposable
{
    /// <summary>
    /// 倒數開始訊息所在的 <c>LogMessage</c> 列。
    /// 台服 7.20 的內容是「距離戰鬥開始還有&lt;數字&gt;秒！\n（&lt;發起者&gt;）」，LogKind = 57（系統訊息）。
    /// </summary>
    private const uint CountdownMessageRow = 5255;

    private NotificationMaster p;

    /// <summary>
    /// 從 LogMessage 取出的比對錨點。
    /// 🔴 刻意不用「整句模板」比對：訊息中間嵌了秒數與發起者名稱，而且句中的換行在
    /// <c>SeString.TextValue</c> 裡不一定保留得下來，拿整句去比會靜默永不命中。
    /// 這裡改成把模板的純文字片段依換行切開、去頭尾空白之後留下夠長的幾段當短錨，
    /// 要求實際訊息「每一段都包含」才算命中——不依賴數字、名稱與換行的呈現方式。
    /// </summary>
    private readonly string[] anchors;

    /// <summary>模板自己宣告的 LogKind；-1 代表查不到，那就不比對聊天類型。</summary>
    private readonly int expectedLogKind;

    public void Dispose()
    {
        Svc.Chat.ChatMessage -= OnChatMessage;
    }

    public BattleCountdown(NotificationMaster plugin)
    {
        p = plugin;
        (anchors, expectedLogKind) = ResolveAnchors();
        Svc.Chat.ChatMessage += OnChatMessage;
    }

    private static (string[] Anchors, int LogKind) ResolveAnchors()
    {
        try
        {
            if(!Svc.Data.GetExcelSheet<LogMessage>().TryGetRow(CountdownMessageRow, out var row))
            {
                // 這則診斷刻意用 Information：使用者跑 LogLevel 1，Debug 收得到但單檔數十萬行會淹沒。
                PluginLog.Information($"[BattleCountdown] 找不到 LogMessage#{CountdownMessageRow}，倒數通知不會觸發。");
                return ([], -1);
            }

            var fragments = row.Text.ToDalamudString().Payloads
                .OfType<TextPayload>()
                .Select(x => x.Text ?? "")
                .SelectMany(x => x.Split('\n', '\r'))
                .Select(x => x.Trim())
                .Where(x => x.Length > 0)
                .Distinct()
                .ToArray();

            // 一個字的片段（像「（」「）」）當錨太鬆，會被其他系統訊息誤中，所以只留兩個字以上的。
            var picked = fragments.Where(x => x.Length >= 2).ToArray();
            if(picked.Length == 0)
            {
                // 極端情況（某語言的模板每段都只有一個字）才退回「最長的那一段」，
                // 寧可鬆一點也好過整個功能靜默失效。
                picked = fragments.OrderByDescending(x => x.Length).Take(1).ToArray();
            }

            if(picked.Length == 0)
            {
                PluginLog.Information($"[BattleCountdown] LogMessage#{CountdownMessageRow} 沒有可用的文字片段，倒數通知不會觸發。");
                return ([], -1);
            }

            // LogKind 在 Lumina 是 RowRef，要的是它的列號（＝聊天類型的低 7 位元）。
            var logKind = (int)row.LogKind.RowId;
            PluginLog.Information($"[BattleCountdown] 倒數訊息錨點：{picked.Print(" | ")}（LogKind={logKind}）");
            return (picked, logKind);
        }
        catch(Exception e)
        {
            e.Log();
            return ([], -1);
        }
    }

    private void OnChatMessage(XivChatType type, int timestamp, ref SeString sender, ref SeString message, ref bool isHandled)
    {
        if(anchors.Length == 0) return;

        // 聊天類型的低 7 位元才是 LogKind，高位是發話者／對象索引——倒數由誰發起會改變高位，
        // 所以只能比對低 7 位元，比對整個 type 會漏掉別人發起的倒數。
        if(expectedLogKind >= 0 && ((ushort)type & 0x7F) != expectedLogKind) return;

        var text = message.TextValue;
        foreach(var anchor in anchors)
        {
            if(!text.Contains(anchor, StringComparison.Ordinal)) return;
        }

        PluginLog.Debug($"Battle countdown detected: {text}");
        if(p.PauseUntil > Environment.TickCount64) return;
        // 一次倒數只會有一則訊息；這個節流只是擋「取消後立刻重開」造成的連續通知。
        if(!EzThrottler.Throttle("NotificationMaster.BattleCountdown", 2000)) return;
        if(!Utils.IsApplicationActivated || p.cfg.countdown_AlwaysExecute)
        {
            DoNotify(text);
        }
    }

    private void DoNotify(string text)
    {
        // 通知本文用遊戲原句（含秒數與發起者），換行壓成空白免得系統匣通知排版跑掉。
        var oneLine = text.Replace('\n', ' ').Replace('\r', ' ').Trim();
        // 取不到就給空字串，不要硬湊一個數字出來騙人。
        var seconds = Regex.Match(oneLine, "[0-9]+").Value;

        if(p.cfg.countdown_FlashTrayIcon)
        {
            Native.Impl.FlashWindow();
        }
        if(p.cfg.countdown_AutoActivateWindow) Native.Impl.Activate();
        if(p.cfg.countdown_ShowToastNotification)
        {
            TrayIconManager.ShowToast(oneLine, "Battle countdown".Loc());
        }
        if(p.cfg.countdown_HttpRequestsEnable)
        {
            p.httpMaster.DoRequests(p.cfg.countdown_HttpRequests,
                new string[][]
                {
                    new string[] {"$T", seconds},
                    new string[] {"$M", oneLine},
                }
            );
        }
        if(p.cfg.countdown_SoundSettings.PlaySound)
        {
            p.audioPlayer.Play(p.cfg.countdown_SoundSettings);
        }
    }

    internal static void Setup(bool enable, NotificationMaster p)
    {
        if(enable)
        {
            if(p.battleCountdown == null)
            {
                p.battleCountdown = new BattleCountdown(p);
                PluginLog.Information("Enabling battleCountdown module");
            }
            else
            {
                PluginLog.Information("battleCountdown module already enabled");
            }
        }
        else
        {
            if(p.battleCountdown != null)
            {
                p.battleCountdown.Dispose();
                p.battleCountdown = null;
                PluginLog.Information("Disabling battleCountdown module");
            }
            else
            {
                PluginLog.Information("battleCountdown module already disabled");
            }
        }
    }
}
