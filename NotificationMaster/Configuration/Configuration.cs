using ECommons.Configuration;
using NotificationMaster.Hub;

namespace NotificationMaster;

[Serializable]
internal class Configuration : IEzConfig
{
    [NonSerialized]
    private IDalamudPluginInterface pluginInterface;
    public int Version { get; set; } = 1;

    public bool gp_Enable = false;
    public bool gp_ShowToastNotification = true;
    public bool gp_FlashTrayIcon = true;
    public bool gp_AutoActivateWindow = false;
    public int gp_PotionCapacity = 400;
    public int gp_GPTreshold = 800;
    public int gp_Tolerance = 50;
    public bool gp_SuppressIfNoNodes = false;
    public bool gp_HttpRequestsEnable = false;
    public List<HttpRequestElement> gp_HttpRequests = [];
    public SoundSettings gp_SoundSettings = new();
    public bool gp_AlwaysExecute = false;

    public bool cutscene_Enable = false;
    public bool cutscene_ShowToastNotification = true;
    public bool cutscene_FlashTrayIcon = true;
    public bool cutscene_AutoActivateWindow = false;
    public bool cutscene_OnlyMSQ = false;
    public bool cutscene_HttpRequestsEnable = false;
    public List<HttpRequestElement> cutscene_HttpRequests = [];
    public SoundSettings cutscene_SoundSettings = new();
    public bool cutscene_AlwaysExecute = false;

    public bool chatMessage_Enable = false;
    public bool chatMessage_ShowToastNotification = true;
    public bool chatMessage_FlashTrayIcon = true;
    public bool chatMessage_AutoActivateWindow = false;
    public List<ChatMessageElement> chatMessage_Elements = [];
    public bool chatMessage_HttpRequestsEnable = false;
    public List<HttpRequestElement> chatMessage_HttpRequests = [];
    public SoundSettings chatMessage_SoundSettings = new();
    public bool chatMessage_AlwaysExecute = false;
    public bool chatMessage_TataruPraise = true;

    public bool cfPop_Enable = false;
    public bool cfPop_ShowToastNotification = true;
    public bool cfPop_FlashTrayIcon = true;
    public bool cfPop_AutoActivateWindow = false;
    public bool cfPop_NotifyIn30 = false;
    public bool cfPop_NotifyOnlyIn30 = false;
    public bool cfPop_HttpRequestsEnable = false;
    public List<HttpRequestElement> cfPop_HttpRequests = [];
    public SoundSettings cfPop_SoundSettings = new();
    public bool cfPop_AlwaysExecute = false;
    public bool cfPop_TataruPraise = true;

    public bool loginError_Enable = false;
    public bool loginError_AlwaysExecute = true;
    public bool loginError_FlashTrayIcon = true;
    public bool loginError_AutoActivateWindow = false;
    public bool loginError_ShowToastNotification = true;
    public bool loginError_HttpRequestsEnable;
    public List<HttpRequestElement> loginError_HttpRequests = [];
    public SoundSettings loginError_SoundSettings = new();

    public bool mapFlag_Enable = false;
    public bool mapFlag_FlashTrayIcon = true;
    public bool mapFlag_AutoActivateWindow = false;
    public bool mapFlag_ShowToastNotification = true;
    public bool mapFlag_HttpRequestsEnable;
    public int mapFlag_TriggerDistance = 200;
    public bool mapFlag_TriggerOnCross = true;
    public int mapFlag_CrossDelta = 100;
    public List<HttpRequestElement> mapFlag_HttpRequests = [];
    public SoundSettings mapFlag_SoundSettings = new();
    public bool mapFlag_AlwaysExecute = false;
    public bool mapFlag_TataruPraise = true;

    public bool arrived_Enable = false;
    public bool arrived_FlashTrayIcon = true;
    public bool arrived_AutoActivateWindow = false;
    public bool arrived_ShowToastNotification = true;
    public bool arrived_HttpRequestsEnable;
    public List<HttpRequestElement> arrived_HttpRequests = [];
    public SoundSettings arrived_SoundSettings = new();
    public bool arrived_AlwaysExecute = false;
    public bool arrived_TataruPraise = true;
    public float arrived_DebounceSeconds = 1.5f;

    public bool mobPulled_Enable = false;
    public bool mobPulled_FlashTrayIcon = true;
    public bool mobPulled_AutoActivateWindow = false;
    public bool mobPulled_ShowToastNotification = true;
    public bool mobPulled_HttpRequestsEnable;
    public List<HttpRequestElement> mobPulled_HttpRequests = [];
    public SoundSettings mobPulled_SoundSettings = new();
    public HashSet<string> mobPulled_Names = [];
    public HashSet<uint> mobPulled_Territories = [];
    public bool mobPulled_AlwaysExecute = true;
    public bool mobPulled_ChatMessage = true;
    public bool mobPulled_Toast = true;

    public bool partyFinder_Enable = false;
    public bool partyFinder_OnlyWhenFilled = false;
    public bool partyFinder_Delisted = false;
    public bool partyFinder_ShowToastNotification = true;
    public bool partyFinder_FlashTrayIcon = true;
    public bool partyFinder_AutoActivateWindow = false;
    public SoundSettings partyFinder_SoundSettings = new();
    public bool partyFinder_AlwaysExecute = true;

    public bool dutyStart_Enable = false;
    public bool dutyStart_ShowToastNotification = true;
    public bool dutyStart_FlashTrayIcon = true;
    public bool dutyStart_AutoActivateWindow = false;
    public bool dutyStart_NotifyRecommence = false;
    public bool dutyStart_HttpRequestsEnable = false;
    public List<HttpRequestElement> dutyStart_HttpRequests = [];
    public SoundSettings dutyStart_SoundSettings = new();
    public bool dutyStart_AlwaysExecute = false;
    public bool dutyStart_TataruPraise = true;

    public bool readyCheck_Enable = false;
    public bool readyCheck_ShowToastNotification = true;
    public bool readyCheck_FlashTrayIcon = true;
    public bool readyCheck_AutoActivateWindow = false;
    public bool readyCheck_HttpRequestsEnable = false;
    public List<HttpRequestElement> readyCheck_HttpRequests = [];
    public SoundSettings readyCheck_SoundSettings = new();
    public bool readyCheck_AlwaysExecute = false;
    public bool readyCheck_TataruPraise = true;

    public bool partyCutscene_Enable = false;
    public bool partyCutscene_ShowToastNotification = true;
    public bool partyCutscene_FlashTrayIcon = true;
    public bool partyCutscene_AutoActivateWindow = false;
    public bool partyCutscene_ChatMessage = true;
    public int partyCutscene_MinSeconds = 4;
    public bool partyCutscene_HttpRequestsEnable = false;
    public List<HttpRequestElement> partyCutscene_HttpRequests = [];
    public SoundSettings partyCutscene_SoundSettings = new();
    public bool partyCutscene_AlwaysExecute = true;
    public bool partyCutscene_TataruPraise = true;

    public bool countdown_Enable = false;
    public bool countdown_ShowToastNotification = true;
    public bool countdown_FlashTrayIcon = true;
    public bool countdown_AutoActivateWindow = false;
    public bool countdown_HttpRequestsEnable = false;
    public List<HttpRequestElement> countdown_HttpRequests = [];
    public SoundSettings countdown_SoundSettings = new();
    public bool countdown_AlwaysExecute = false;

    public bool fishBite_Enable = false;
    public bool fishBite_ShowToastNotification = false;
    public bool fishBite_FlashTrayIcon = true;
    public bool fishBite_AutoActivateWindow = false;
    public bool fishBite_ChatMessage = true;
    public bool fishBite_AlwaysExecute = true;
    public bool fishBite_LightEnabled = true;
    public bool fishBite_MediumEnabled = true;
    public bool fishBite_HeavyEnabled = true;
    public SoundSettings fishBite_LightSoundSettings = new();
    public SoundSettings fishBite_MediumSoundSettings = new();
    public SoundSettings fishBite_HeavySoundSettings = new();
    public bool fishBite_HttpRequestsEnable = false;
    public List<HttpRequestElement> fishBite_HttpRequests = [];

    /// <summary>通知樞紐（<c>NotificationMaster.Notify</c> IPC）的總開關。</summary>
    /// <remarks>
    /// 🔴 預設<b>開</b>，這是刻意的：樞紐<b>只有別的外掛主動送東西進來才會有動作</b>，
    /// 沒有任何外掛在用的時候它一句話都不會說。預設關的話，
    /// 消費端接上之後使用者會遇到「我開了 AutoRetainer 的通知但沒反應」，
    /// 而真正要開的開關在<b>另一個外掛</b>裡——那是查不出來的。
    /// <para>
    /// ⚠️ 逐事件的開關在 <see cref="hub_Routes"/>，逐外掛的開關在<b>各外掛自己</b>（一律預設關）。
    /// 這個總開關是「我完全不要這個機制」用的。
    /// </para>
    /// </remarks>
    public bool hub_Enable = true;

    /// <summary>
    /// 逐分類的路由表。<b>缺鍵＝<see cref="HubRoute.DefaultFor"/> 的預設路由。</b>
    /// </summary>
    /// <remarks>
    /// 🔴 用字典而不是一堆布林欄位是<b>必要的</b>：EzConfig 會把 <c>false</c> 也寫進 JSON，
    /// 布林欄位一旦寫進使用者的 <c>DefaultConfig.json</c> 就再也改不動預設（而且是靜默的）。
    /// 詳見 <see cref="HubRoute"/> 的說明。
    /// </remarks>
    public Dictionary<string, HubRoute> hub_Routes = [];

    /// <summary>樞紐所有分類共用的音效設定。</summary>
    public SoundSettings hub_SoundSettings = new();

    /// <summary>樞紐所有分類共用的 HTTP webhook 開關。</summary>
    public bool hub_HttpRequestsEnable = false;

    /// <summary>樞紐的 HTTP webhook。
    /// 可用替換符號：<c>&lt;caller&gt;</c>、<c>&lt;category&gt;</c>、<c>&lt;title&gt;</c>、<c>&lt;body&gt;</c>。</summary>
    public List<HttpRequestElement> hub_HttpRequests = [];

    /// <summary>
    /// 同一個「外掛＋分類」組合兩次通知之間的最短間隔（毫秒；0＝不節流）。
    /// </summary>
    /// <remarks>
    /// 🔴 這是<b>地板</b>不是策略：真正的「同一件事只響一次」必須由呼叫端在<b>狀態邊緣</b>上做。
    /// 這裡只保證某個呼叫端把通知寫進輪詢迴圈時，不會把系統匣氣球洗爆。
    /// </remarks>
    public int hub_ThrottleMs = 3000;

    public void Initialize(IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
        foreach(var e in chatMessage_Elements)
        {
            if(e.ChatType != 0)
            {
                e.ChatTypes.Add(e.ChatType);
            }
            e.ChatType = 0;
        }
    }

    public void Save()
    {
        EzConfig.Save();
    }
}
