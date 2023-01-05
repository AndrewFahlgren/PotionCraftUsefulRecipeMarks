using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Services;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class SetupInitialInfoForContinueBrewingPatch
    {
        [HarmonyPatch(typeof(RecipeBookContinuePotionBrewingButton), "ContinuePotionBrewing")]
        public class RecipeBookContinuePotionBrewingButton_ContinuePotionBrewing
        {
            static void Postfix(RecipeBookRightPageContent ___rightPageContent)
            {
                Ex.RunSafe(() => DeltaRecordingService.SetupInitialInfoForRecipe(___rightPageContent));
            }
        }
    }
}
