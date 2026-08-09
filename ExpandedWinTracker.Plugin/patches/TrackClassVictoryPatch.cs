using HarmonyLib;

namespace ExpandedWinTracker.Plugin.patches
{
    [HarmonyPatch(typeof(SaveManager), "TrackClassVictory")]
    public class TrackClassVictoryPatch
    {
        public static void Postfix(SaveManager __instance)
        {
            var runType = __instance.GetRunType();
            var mainClassId = __instance.GetMainClass().GetID();
            var subClassId = __instance.GetSubClass().GetID();
            var mainChampionIndex = __instance.GetMainChampionIndex();
            var regionRunDifficultyTier = __instance.GetStartingConditions().GetRegionRunDifficultyTier();
            
            if (runType == RunType.RegionRun)
            {
                Plugin.AddOrUpdateSoulSaviorWin(mainClassId, subClassId, mainChampionIndex, regionRunDifficultyTier);
                var souls = __instance.GetSelectedSouls();
                foreach (var soul in souls)
                {
                    string baseName = soul.GetSoulData()!.name;
                    string name = baseName[..^2];
                    Plugin.AddOrUpdateSoulWin(name, soul.TierLevel, regionRunDifficultyTier);
                }
            }

            Plugin.WriteSaveData();
        }
    }
}
