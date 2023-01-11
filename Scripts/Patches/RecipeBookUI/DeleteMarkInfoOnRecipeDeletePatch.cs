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
            static bool Prefix(Potion potion)
            {
                return Ex.RunSafe(() => DeleteMarkInfoOnRecipeDelete(potion));
            }
        }

        private static bool DeleteMarkInfoOnRecipeDelete(Potion recipe)
        {
            DeltaRecordingService.DeleteMarkInfoForRecipe(recipe);
            return true;
        }
    }
}
