using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public class RecipeBookUIService
    {
        private static Color? MarkDefaultColor;
        private static Color? TextDefaultColor;

        public static void UpdateRecipeBookPageForSelectedRecipeMark(Button __instance)
        {
            if (__instance is not RecipeBookRecipeMark recipeMark) return;

            var recipeIndex = Managers.Potion.recipeBook.currentPageIndex;

            var markIndex = recipeMark.currentMarkIndex;
            UpdateRecipeBookPageForSelectedRecipeMark(recipeIndex, markIndex);
        }

        public static void UpdateRecipeBookPageForSelectedRecipeMark(int recipeIndex, int markIndex)
        {
            //Disable reconstruction for alchemy machine recipes
            if (IsAlchemyMachineRecipe(Managers.Potion.recipeBook.savedRecipes[recipeIndex])) return;

            //Do nothing for potion base marks
            if (markIndex == 0) return;

            if (!RecipeReconstructionService.MarkHasSavedData(recipeIndex, markIndex))
            {
                Notification.ShowText("This recipe mark is missing critical information", "You can only continue brewing from new recipe marks", Notification.TextType.EventText);
                return;
            }

            var recipe = RecipeReconstructionService.GetPotionForRecipeMark(recipeIndex, markIndex);

            if (recipe == null) return;

            //Update page with reconstructed recipe
            var leftPage = Managers.Potion.recipeBook.curlPageController.frontLeftPage;
            var rightPage = Managers.Potion.recipeBook.curlPageController.frontRightPage;
            leftPage.UpdatePageContent(PageContent.State.Filled, recipe);
            rightPage.UpdatePageContent(PageContent.State.Filled, recipe);

            //Update recipe marks to show selection
            var rightPageContent = (RecipeBookRightPageContent)rightPage.pageContent;
            var visibleMarksDict = (Dictionary<int, List<RecipeBookRecipeMark>>)Traverse.Create(rightPageContent).Field("visibleMarks").GetValue();
            var allMarks = visibleMarksDict.Values.SelectMany(v => v).ToList();
            allMarks.ForEach(mark => EnableDisableMark(mark, mark.currentMarkIndex <= markIndex));

            var isReconstructed = recipe.potionFromPanel.recipeMarks.Count - 1 != markIndex;

            //Lock down the waypoint toggle button for reconstructed recipes if Recipe Waypoints is installed
            //Call update visual here to let recipe waypoints add the waypoint toggle button back in if needed
            var brewPotionButton = (RecipeBookBrewPotionButton)Traverse.Create(rightPageContent).Field("brewPotionButton").GetValue();
            brewPotionButton.UpdateVisual();
            if (isReconstructed)
            {
                DisableRecipeWaypointsUIElements(rightPageContent);
                //Make sure the brew potion button is active (this can be inactive if recipe waypoints is installed)
                brewPotionButton.gameObject.SetActive(true);
            }

            //Lock down the brew potion button for reconstructed recipes
            brewPotionButton.Locked = isReconstructed;

            //Set the selected recipe mark index for later use if the player decides to continue brewing
            StaticStorage.SelectedRecipeMarkIndex = markIndex;
        }

        private static void EnableDisableMark(RecipeBookRecipeMark mark, bool enabled)
        {
            var iconSpriteRenderers = mark.GetComponentsInChildren<SpriteRenderer>().ToList();
            iconSpriteRenderers.ForEach(r =>
            {
                if (MarkDefaultColor == null) MarkDefaultColor = r.color;
                var baseColor = MarkDefaultColor.Value;
                r.color = enabled ? baseColor : GetDisabledColor(baseColor);
            });
            var text = mark.GetComponentInChildren<TextMeshPro>();
            if (text == null) return;
            if (TextDefaultColor == null) TextDefaultColor = text.color;
            var baseColor = TextDefaultColor.Value;
            text.color = enabled ? baseColor : GetDisabledColor(baseColor);
        }

        private static Color GetDisabledColor(Color baseColor)
        {
            return new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
        }

        public static void UpdateRecipeMarksForStateChanged(bool shown)
        {
            if (!shown) return;
            DisableOldRecipeMarks(Managers.Potion.recipeBook);
        }

        public static void UpdateRecipeMarksForPageChange(int _, int _0)
        {
            DisableOldRecipeMarks(Managers.Potion.recipeBook);
        }

        public static void DisableOldRecipeMarks(Book book)
        {
            if (book is not RecipeBook recipeBook) return;
            var rightPage = recipeBook.curlPageController.frontRightPage;
            var rightPageContent = (RecipeBookRightPageContent)rightPage.pageContent;
            var visibleMarks = (Dictionary<int, List<RecipeBookRecipeMark>>)Traverse.Create(rightPageContent).Field("visibleMarks").GetValue();
            DisableOldRecipeMarks(visibleMarks);
        }

        public static void DisableOldRecipeMarks(Dictionary<int, List<RecipeBookRecipeMark>> visibleMarks)
        {
            var recipeIndex = Managers.Potion.recipeBook.currentPageIndex;
            var allMarks = visibleMarks.Values.SelectMany(v => v).ToList();
            var recipeHasSavedData = StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex);
            StaticStorage.SelectedRecipeMarkIndex = allMarks.Count - 1;
            allMarks.ForEach(mark =>
            {
                //Only grey out recipe marks if there are any recipe marks with good data
                var enabled = !recipeHasSavedData || RecipeReconstructionService.MarkHasSavedData(recipeIndex, mark.currentMarkIndex);
                EnableDisableMark(mark, enabled);
            });
        }

        private static void DisableRecipeWaypointsUIElements(RecipeBookRightPageContent rightPageContent)
        {
            var waypointToggleButton = rightPageContent.transform.Find("WaypointToggleButton");
            waypointToggleButton?.gameObject?.SetActive(false);
            var viewWaypointButton = rightPageContent.transform.Find("BrewPotionButton(Clone)");
            viewWaypointButton?.gameObject?.SetActive(false);
        }

        public static bool IsAlchemyMachineRecipe(Potion recipe)
        {
            return recipe.potionFromPanel.recipeMarks.Count(m => m.type == SerializedRecipeMark.Type.PotionBase) > 1;
        }

        public static void BookmarksRearranged(BookmarkController _, List<int> intList)
        {
            var oldRecipeMarkInfos = StaticStorage.RecipeMarkInfos.ToList();
            var newRecipeMarkInfos = new List<KeyValuePair<int, Dictionary<int, RecipeMarkInfo>>>();
            for (var newIndex = 0; newIndex < intList.Count; newIndex++)
            {
                var oldIndex = intList[newIndex];
                //This will recreate the old ignored list making sure to update any indexes along the way
                var oldRecipeMarkInfo = oldRecipeMarkInfos.FirstOrDefault(rmi => rmi.Key == oldIndex);
                if (!oldRecipeMarkInfo.Equals(default(KeyValuePair<int, Dictionary<int, RecipeMarkInfo>>)))
                {
                    newRecipeMarkInfos.Add(new (oldRecipeMarkInfo.Key, oldRecipeMarkInfo.Value));
                }
            }
            StaticStorage.RecipeMarkInfos = newRecipeMarkInfos.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }
}
