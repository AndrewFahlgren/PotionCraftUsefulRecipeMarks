using HarmonyLib;
using PotionCraft.ManagersSystem.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Services;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class ResetTrackingForPotionResetPatch
    {
        [HarmonyPatch(typeof(PotionManager), "ResetPotion")]
        public class PotionManager_ResetPotion
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaRecordingService.SetupInitialInfo());
            }
        }
    }
}
