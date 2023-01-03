using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class CommitRecipeMarkInfoForSavedRecipePatch
    {
        [HarmonyPatch(typeof(SaveRecipeButton), "OnButtonReleasedPointerInside")]
        public class SaveRecipeButton_OnButtonReleasedPointerInside
        {
            static bool Prefix()
            {
                return Ex.RunSafe(() => DeltaRecordingService.CommitRecipeMarkInfoForSavedRecipe());
            }
        }
    }
}
