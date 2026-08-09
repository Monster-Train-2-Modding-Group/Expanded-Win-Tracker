using HarmonyLib;
using static RunHistoryDataJson;

namespace ExpandedWinTracker.Plugin.patches
{
    [HarmonyPatch(typeof(RunHistoryDataJson), nameof(RunHistoryDataJson.InitUserAsync))]
    public static class RunHistoryDataJson_InitUserAsync_Patch
    {
        [HarmonyPostfix]
        public static void Postfix(RunHistoryDataJson __instance, ref Task __result)
        {
            __result = AfterInit(__result, __instance);
        }

        private static async Task AfterInit(Task originalTask, RunHistoryDataJson instance)
        {
            await originalTask;

            if (!Plugin.scanRunHistory!.Value) return;

            var runEntries = AccessTools.Field(typeof(RunHistoryDataJson), "_runEntries").GetValue(instance) as List<RunHistoryDataJson.RunEntry>;
            if (runEntries == null) return;
            ScanRunHistory(runEntries);
            Plugin.scanRunHistory.Value = false;
        }

        private static void ScanRunHistory(List<RunEntry> runEntries)
        {
            foreach (var entry in runEntries)
            {
                var data = entry.GetRun();
                Plugin.Logger.LogDebug($"Scanning run: {entry.runId}");
                if (data.GetRunTypeEnum() != RunType.RegionRun || data.GetVictory() == false)
                    continue;

                var setup = data.GetStartingConditions();
                var classID = setup.ClassId;
                var subclassID = setup.SubclassId;
                var championIndex = setup.MainChampionIndex;
                var regionLevel = setup.GetRegionRunDifficultyTier();

                Plugin.AddOrUpdateSoulSaviorWin(classID, subclassID, championIndex, regionLevel);

                var souls = data.GetSouls();
                foreach (var soul in souls)
                {
                    int tier = soul.TierLevel;
                    string baseName = soul.GetSoulData()!.name;
                    string name = baseName[..^2];
                    Plugin.AddOrUpdateSoulWin(name, tier, regionLevel);
                }
            }

            Plugin.WriteSaveData();
        }
    }
}
