using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Services;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    public class DeleteMarkInfoOnRecipeDeletePatch
    {
        [HarmonyPatch(typeof(RecipeBook), "EraseRecipe")]
        public class RecipeBook_EraseRecipe
        {
            static bool Prefix(IRecipeBookPageContent recipe)
            {
                return Ex.RunSafe(() => DeleteMarkInfoOnRecipeDelete(recipe));
            }
        }

        private static bool DeleteMarkInfoOnRecipeDelete(IRecipeBookPageContent recipe)
        {
            if (recipe is not Potion potion) return true;
            DeltaRecordingService.DeleteMarkInfoForRecipe(potion);
            return true;
        }
    }
}
