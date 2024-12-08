using HarmonyLib;
using PotionCraft.ManagersSystem.Potion;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class SetupInitialInfoForContinueBrewingPatch
    {
        [HarmonyPatch(typeof(RecipeBookContinuePotionBrewingButton), "ContinuePotionBrewing")]
        public class RecipeBookContinuePotionBrewingButton_ContinuePotionBrewing
        {
            static void Prefix()
            {
                Ex.RunSafe(() => ContinueBrewingPressed(null, true));
            }

            static void Postfix(RecipeBookRightPageContent ___rightPageContent)
            {
                Ex.RunSafe(() => ContinueBrewingPressed(___rightPageContent, false));
            }
        }

        [HarmonyPatch(typeof(PotionManager), "OnProgressLoad")]
        public class PotionManager_OnProgressLoad
        {
            static void Prefix()
            {
                Ex.RunSafe(() => OnProgressLoadCalled(true));
            }

            static void Postfix()
            {
                Ex.RunSafe(() => OnProgressLoadCalled(false));
            }
        }

        [HarmonyPatch(typeof(PotionManager), "ApplySerializedPotionRecipeDataToCurrentPotion")]
        public class SerializedPotionFromPanel_ApplyPotionToCurrentPotion
        {
            static void Postfix(SerializedPotionRecipeData serializedPotionRecipeData)
            {
                Ex.RunSafe(() => ApplyPotionToCurrentPotionCalled(serializedPotionRecipeData));
            }
        }

        private static bool IgnoreApplyPotionToCurrentPotion;
        private static void ContinueBrewingPressed(RecipeBookRightPageContent rightPageContent, bool isPrefix)
        {
            IgnoreApplyPotionToCurrentPotion = isPrefix;
            if (isPrefix) return;
            DeltaRecordingService.SetupInitialInfoForRecipe(rightPageContent);
        }

        private static void OnProgressLoadCalled(bool isPrefix)
        {
            IgnoreApplyPotionToCurrentPotion = isPrefix;
        }

        //This allows functionality with Pour Back In
        private static void ApplyPotionToCurrentPotionCalled(SerializedPotionRecipeData potion)
        {
            if (IgnoreApplyPotionToCurrentPotion) return;
            DeltaRecordingService.SetupInitialInfoForRecipe(potion, 0);
        }
    }
}
