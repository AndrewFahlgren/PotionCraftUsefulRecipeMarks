﻿using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class CommitRecipeMarkInfoForSavedRecipePatch
    {
        [HarmonyPatch(typeof(SavePotionRecipeButton), "OnButtonReleasedPointerInside")]
        public class SaveRecipeButton_OnButtonReleasedPointerInside
        {
            static void Prefix()
            {
                Ex.RunSafe(DeltaRecordingService.CommitRecipeMarkInfoForSavedRecipe);
            }
        }
    }
}
