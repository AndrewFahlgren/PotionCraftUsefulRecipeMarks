using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public class RecipeBookUIService
    {
        public static void UpdateRecipeBookPageForSelectedRecipeMark(Button __instance)
        {
            if (__instance is not RecipeBookRecipeMark recipeMark) return;

            var recipeIndex = Managers.Potion.recipeBook.currentPageIndex;
            var markIndex = recipeMark.currentMarkIndex;
            var recipe = RecipeReconstructionService.GetPotionForRecipeMark(recipeIndex, markIndex);

            if (recipe == null) return;

            StaticStorage.SelectedRecipeMarkIndex = markIndex;

            var leftPage = Managers.Potion.recipeBook.curlPageController.frontLeftPage;
            var rightPage = Managers.Potion.recipeBook.curlPageController.frontRightPage;
            leftPage.UpdatePageContent(PageContent.State.Filled, recipe);
            rightPage.UpdatePageContent(PageContent.State.Filled, recipe);
        }
    }
}
