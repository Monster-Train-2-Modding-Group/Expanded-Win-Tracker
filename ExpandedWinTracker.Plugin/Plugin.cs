using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using System.Text;

namespace ExpandedWinTracker.Plugin
{
    class ConfigDescriptionBuilder
    {
        public string English { get; set; } = "";
        public string French { get; set; } = "";
        public string German { get; set; } = "";
        public string Russian { get; set; } = "";
        public string Portuguese { get; set; } = "";
        public string Chinese { get; set; } = "";
        public string Spanish { get; set; } = "";
        public string ChineseTraditional { get; set; } = "";
        public string Korean { get; set; } = "";
        public string Japanese { get; set; } = "";

        public override string ToString()
        {
            StringBuilder builder = new();
            if (!string.IsNullOrEmpty(English)) builder.AppendLine(English);
            if (!string.IsNullOrEmpty(French)) builder.AppendLine(French);
            if (!string.IsNullOrEmpty(German)) builder.AppendLine(German);
            if (!string.IsNullOrEmpty(Russian)) builder.AppendLine(Russian);
            if (!string.IsNullOrEmpty(Portuguese)) builder.AppendLine(Portuguese);
            if (!string.IsNullOrEmpty(Chinese)) builder.AppendLine(Chinese);
            if (!string.IsNullOrEmpty(Spanish)) builder.AppendLine(Spanish);
            if (!string.IsNullOrEmpty(ChineseTraditional)) builder.AppendLine(ChineseTraditional);
            if (!string.IsNullOrEmpty(Korean)) builder.AppendLine(Korean);
            if (!string.IsNullOrEmpty(Japanese)) builder.AppendLine(Japanese);
            return builder.ToString().TrimEnd();
        }
    }

    [Serializable]
    public sealed class SoulSaviorClassCombination
    {
        [JsonProperty("classID")]
        public string ClassID { get; set; } = "";
        [JsonProperty("subclassID")]
        public string SubclassID { get; set; } = "";
        [JsonProperty("championIndex")]
        public int ChampionIndex { get; set; } = 0;
        [JsonProperty("highestGameLevel")]
        public int HighestGameLevel { get; set; } = 0;
    }

    [Serializable]
    public class SoulSaviorSoulWin
    {
        [JsonProperty("name")]
        public string Name { get; set; } = "";
        [JsonProperty("tier")]
        public int Tier {  get; set; } = 0;
        [JsonProperty("highestGameLevel")]
        public int HighestGameLevel { get; set; } = 0;
    }

    [Serializable]
    public class ExtraMetagameSaveData
    {
        [JsonProperty("soulSaviorClassWins")]
        public List<SoulSaviorClassCombination> SoulSaviorClassWins { get; set; } = [];
        [JsonProperty("soulWins")]
        public List<SoulSaviorSoulWin> SoulWins { get; set; } = [];
    }

    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        internal static new ManualLogSource Logger = new(MyPluginInfo.PLUGIN_GUID);

        public static ConfigEntry<bool>? soulSaviorWinTracker;
        public static ConfigEntry<bool>? scanRunHistory;

        public static ExtraMetagameSaveData SaveData = new();
        public static string SaveFilePath = string.Empty;
        public static string ModConfigDir = string.Empty;

        // Plugin startup logic. This function is automatically called when your plugin initializes
        public void Awake()
        {
            Logger = base.Logger;

            ModConfigDir = Path.Combine(Paths.ConfigPath, MyPluginInfo.PLUGIN_GUID);
            SaveFilePath = Path.Combine(ModConfigDir, "extraMetagameSave.json");
            ReadSaveData();

            /*soulSaviorWinTracker = Config.Bind<bool>("Win Trackers", "Soul Savior Win Tracking", false,
                new ConfigDescription(new ConfigDescriptionBuilder
                {
                    English = "Enable Soul Savior win tracking.",
                    Chinese = ""
                }.ToString()));*/

            scanRunHistory = Config.Bind<bool>("Options", "Scan Run History", true,
                new ConfigDescription(new ConfigDescriptionBuilder
                {
                    English = "Scan the Run History JSON to populate unrecorded wins. This option will re-set itself to false the next time the game is ran.",
                    Chinese = ""
                }.ToString()));

            Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            harmony.PatchAll();
        }

        public static void ReadSaveData()
        {
            CreateSaveDir();
            if (!File.Exists(SaveFilePath))
            {
                Logger.LogWarning($"No save data found at {SaveFilePath}");
                WriteSaveData();
                return;
            }
            var json = File.ReadAllText(SaveFilePath);
            var data = JsonConvert.DeserializeObject<ExtraMetagameSaveData>(json);
            if (data == null)
            {
                Logger.LogWarning($"Failed to read save data at {SaveFilePath}");
                data = new ExtraMetagameSaveData();
            }
            SaveData = data;
        }

        public static void CreateSaveDir()
        {
            if (!Directory.Exists(ModConfigDir))
            {
                Directory.CreateDirectory(ModConfigDir);
            }
        }

        public static void WriteSaveData()
        {
            CreateSaveDir();
            string json = JsonConvert.SerializeObject(Plugin.SaveData, Formatting.Indented);
            Logger.LogDebug($"Writing to {SaveFilePath}");
            File.WriteAllText(Plugin.SaveFilePath, json);
        }

        internal static SoulSaviorSoulWin FindSoulWin(string name)
        {
            return SaveData.SoulWins.Find(x => x.Name == name);
        }

        internal static void AddOrUpdateSoulWin(string name, int tier, int regionLevel)
        {
            var soulWin = FindSoulWin(name);
            if (soulWin != null)
            {
                if (soulWin.HighestGameLevel < regionLevel)
                {
                    soulWin.HighestGameLevel = regionLevel;
                    Logger.LogInfo($"Updated {name}({tier}) win to {regionLevel}");
                }
                if (soulWin.Tier < tier)
                {
                    soulWin.Tier = tier;
                    Logger.LogInfo($"Updated {name} win to {tier} level");
                }
            }
            else
            {
                SaveData.SoulWins.Add(new SoulSaviorSoulWin
                {
                    Name = name,
                    Tier = tier,
                    HighestGameLevel = regionLevel
                });
                Logger.LogInfo($"Added {name}({tier}) win at {regionLevel}");
            }
        }

        internal static SoulSaviorClassCombination FindSoulSaviorWin(string classID, string subclassID, int championIndex)
        {
            return SaveData.SoulSaviorClassWins.Find(x => x.ClassID == classID && x.SubclassID == subclassID && x.ChampionIndex == championIndex);
        }

        internal static void AddOrUpdateSoulSaviorWin(string classID, string subclassID, int championIndex, int regionLevel)
        {
            var win = FindSoulSaviorWin(classID, subclassID, championIndex);
            if (win != null)
            {
                if (win.HighestGameLevel < regionLevel)
                {
                    win.HighestGameLevel = regionLevel;
                    Logger.LogInfo($"Updated {classID}-({championIndex})/{subclassID} win to {regionLevel}");
                }
            }
            else
            {
                SaveData.SoulSaviorClassWins.Add(new SoulSaviorClassCombination
                {
                    ClassID = classID,
                    SubclassID = subclassID,
                    ChampionIndex = championIndex,
                    HighestGameLevel = regionLevel,
                });
                Logger.LogInfo($"Added {classID}-({championIndex})/{subclassID} win at {regionLevel}");
            }
        }
    }
}
