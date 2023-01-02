using HarmonyLib;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class SetupInitialInfo
    {
        [HarmonyPatch(typeof(SerializedPotionFromPanel), "ApplyPotionToCurrentPotion")]
        public class SerializedPotionFromPanel_ApplyPotionToCurrentPotion
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaService.SetupInitialInfo());
            }
        }
    }
}
