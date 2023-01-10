using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

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

            //Update page with reconstructed recipe
            var leftPage = Managers.Potion.recipeBook.curlPageController.frontLeftPage;
            var rightPage = Managers.Potion.recipeBook.curlPageController.frontRightPage;
            leftPage.UpdatePageContent(PageContent.State.Filled, recipe);
            rightPage.UpdatePageContent(PageContent.State.Filled, recipe);

            //Update recipe marks to show selection
            var rightPageContent = (RecipeBookRightPageContent)rightPage.pageContent;
            var visibleMarksDict = (Dictionary<int, List<RecipeBookRecipeMark>>)Traverse.Create(rightPageContent).Field("visibleMarks").GetValue();
            var allMarks = visibleMarksDict.Values.SelectMany(v => v).ToList();
            var markDisabledColor = new Color(1, 1, 1, 0.5f);
            var markEnabledColor = new Color(1, 1, 1, 1);
            allMarks.ForEach(mark =>
            {
                var isDisabled = mark.currentMarkIndex > markIndex;
                var iconSpriteRenderer = mark.GetComponentInChildren<SpriteRenderer>();
                iconSpriteRenderer.color = isDisabled ? markDisabledColor : markEnabledColor;
                var text = mark.GetComponentInChildren<TextMeshPro>();
                text.color = isDisabled ? markDisabledColor : markEnabledColor;

            });
        }
    }
}
