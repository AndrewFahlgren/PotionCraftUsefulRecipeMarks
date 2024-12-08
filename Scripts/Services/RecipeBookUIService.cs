using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.NotificationSystem;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.AlchemyMachineProducts;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public class RecipeBookUIService
    {
        private static Color? MarkDefaultColor;
        private static Color? TextDefaultColor;

        public static void UpdateRecipeBookPageForSelectedRecipeMark(Button __instance)
        {
            if (__instance is not RecipeBookRecipeMark recipeMark) return;

            var recipeIndex = RecipeBook.Instance.currentPageIndex;

            var markIndex = recipeMark.currentMarkIndex;

            //Disable reconstruction for alchemy machine recipes
            if (IsAlchemyMachineRecipe(RecipeBook.Instance.savedRecipes[recipeIndex])) return;

            //Do nothing for potion base marks
            if (markIndex == 0) return;

            var indexOverride = -1;
            if (!RecipeReconstructionService.MarkHasSavedData(recipeIndex, markIndex))
            {
                //Check if this is the base mark for a half old recipe
                if (RecipeReconstructionService.MarkHasSavedData(recipeIndex, markIndex + 1))
                {
                    indexOverride = 0;
                }
                else
                {
                    Notification.ShowText("This recipe mark is missing critical information", "You can only continue brewing from new recipe marks", Notification.TextType.EventText);
                    return;
                }
            }

            UpdateRecipeBookPageForSelectedRecipeMark(recipeIndex, markIndex, indexOverride);
        }

        public static void UpdateRecipeBookPageForSelectedRecipeMark(int recipeIndex, int markIndex, int markIndexOverride = -1)
        {
            var recipe = RecipeReconstructionService.GetPotionForRecipeMark(recipeIndex, markIndexOverride == -1 ? markIndex : markIndexOverride);

            if (recipe == null) return;

            //Set the selected recipe mark index for later use if the player decides to continue brewing
            StaticStorage.SelectedRecipeMarkIndex = markIndex;
            StaticStorage.SelectedRecipeIndex = recipeIndex;

            //Update page with reconstructed recipe
            var leftPage = RecipeBook.Instance.curlPageController.frontLeftPage;
            var rightPage = RecipeBook.Instance.curlPageController.frontRightPage;
           // recipe.potionFromPanel.potionSkinSettings.currentIconName = Icon.allIcons.First().name;
            leftPage.UpdatePageContent(PageContentState.Filled, recipe, rightPage);
            rightPage.UpdatePageContent(PageContentState.Filled, recipe, leftPage);

            var isReconstructed = recipe.GetRecipeData().recipeMarks.Count - 1 != markIndex;

            //Lock down the waypoint toggle button for reconstructed recipes if Recipe Waypoints is installed
            //Call update visual here to let recipe waypoints add the waypoint toggle button back in if needed
            var rightPageContent = (RecipeBookRightPageContent)rightPage.pageContent;
            var bottomButtonController = Traverse.Create(rightPageContent).Field<RecipeBookRightPageBottomButtonsController>("bottomButtonsController").Value;
            var brewPotionButton = (RecipeBookBrewRecipeButton)Traverse.Create(bottomButtonController).Field("brewRecipeSoloButton").GetValue();
            brewPotionButton.UpdateVisual();
            if (isReconstructed)
            {
                DisableRecipeWaypointsUIElements(rightPageContent);
            }

            //Lock down the brew potion button for reconstructed recipes
            if (isReconstructed) brewPotionButton.Locked = true;
        }

        public static void EnableDisableMark(RecipeBookRecipeMark mark)
        {
            var recipeIndex = RecipeBook.Instance.currentPageIndex;
            if (StaticStorage.SelectedRecipeMarkIndex > 0
                && StaticStorage.SelectedRecipeIndex == recipeIndex
                && StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex))
            {
                EnableDisableMark(mark, mark.currentMarkIndex <= StaticStorage.SelectedRecipeMarkIndex);
            }
            else
            {
                var recipeHasSavedData = StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex);
                var curMarkIndex = mark.currentMarkIndex;
                //Only grey out recipe marks if there are any recipe marks with good data
                var enabled = !recipeHasSavedData
                              || (curMarkIndex != 0
                                  && (RecipeReconstructionService.MarkHasSavedData(recipeIndex, curMarkIndex)
                                      || RecipeReconstructionService.MarkHasSavedData(recipeIndex, curMarkIndex + 1)));
                EnableDisableMark(mark, enabled);
            }
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
            Ex.RunSafe(() =>
            {
                if (!shown) return;
                var currentPageIndex = RecipeBook.Instance.currentPageIndex;

                if (StaticStorage.SelectedRecipeMarkIndex > 0
                    && StaticStorage.SelectedRecipeIndex == currentPageIndex
                    && StaticStorage.RecipeMarkInfos.ContainsKey(currentPageIndex))
                {
                    UpdateRecipeBookPageForSelectedRecipeMark(currentPageIndex, StaticStorage.SelectedRecipeMarkIndex);
                }
                else
                {
                    DisableOldRecipeMarks(RecipeBook.Instance);
                }
            });
        }

        public static void UpdateRecipeMarksForPageChange(int _, int _0)
        {
            Ex.RunSafe(() => DisableOldRecipeMarks(RecipeBook.Instance));
        }

        public static void DisableOldRecipeMarks(Book book)
        {
            if (book is not RecipeBook recipeBook) return;
            var rightPage = recipeBook.curlPageController.frontRightPage;
            var rightPageContent = (RecipeBookRightPageContent)rightPage.pageContent;
            DisableOldRecipeMarks(rightPageContent);
        }

        public static void DisableOldRecipeMarks(RecipeBookRightPageContent rightPageContent)
        {
            var recipeData = (rightPageContent?.GetRecipeBookPageContent() as Potion)?.GetRecipeData();
            if (RecipeBook.Instance == null || recipeData == null) return;
            var recipeIndex = RecipeBook.Instance.currentPageIndex;
            var markCount = recipeData.recipeMarks.Count;
            StaticStorage.SelectedRecipeMarkIndex = markCount - 1;
            StaticStorage.SelectedRecipeIndex = recipeIndex;
            var recipeController = Traverse.Create(rightPageContent).Field<RecipeBookRightPageRecipePanelController>("recipePanelController").Value;
            Traverse.Create(recipeController).Field("previousScrollViewContentYPosition").SetValue(-1f);
            recipeController.OnLateUpdate();
            //Traverse.Create(recipeController).Method("UpdatePotionRecipe").GetValue();
        }

        private static void DisableRecipeWaypointsUIElements(RecipeBookRightPageContent rightPageContent)
        {
            var waypointToggleButton = rightPageContent.transform.Find("WaypointToggleButton");
            waypointToggleButton?.gameObject?.SetActive(false);
        }

        public static bool IsAlchemyMachineRecipe(IRecipeBookPageContent recipe)
        {
            return recipe is not Potion;
        }

        public static void BookmarksRearranged(BookmarkController _, List<int> intList)
        {
            Ex.RunSafe(() =>
            {
                var oldRecipeMarkInfos = StaticStorage.RecipeMarkInfos.ToList();
                var newRecipeMarkInfos = new List<KeyValuePair<int, Dictionary<int, RecipeMarkInfo>>>();
                var newRecipeList = new List<IRecipeBookPageContent>();
                for (var newIndex = 0; newIndex < intList.Count; newIndex++)
                {
                    var oldIndex = intList[newIndex];
                    //This will recreate the old ignored list making sure to update any indexes along the way
                    var oldRecipeMarkInfo = oldRecipeMarkInfos.FirstOrDefault(rmi => rmi.Key == oldIndex);
                    if (!oldRecipeMarkInfo.Equals(default(KeyValuePair<int, Dictionary<int, RecipeMarkInfo>>)))
                    {
                        newRecipeMarkInfos.Add(new(newIndex, oldRecipeMarkInfo.Value));
                    }

                    //Keep an up to date recipe list with indexes to use for the old bookmark organizer failsafe
                    if (StaticStorage.BookmarkOrganizerOldVersionInstalled)
                    {
                        newRecipeList.Add(StaticStorage.RecipeIndexes[oldIndex]);
                    }
                }
                StaticStorage.RecipeMarkInfos = newRecipeMarkInfos.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                StaticStorage.RecipeIndexes = newRecipeList;
            });
        }

        public static void RemoveRecipeMarkForDeletedRecipe(IRecipeBookPageContent recipe)
        {
            if (recipe is not Potion) return;
            var recipeIndex = RecipeBook.Instance.savedRecipes.IndexOf(recipe);
            if (StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex))
            {
                StaticStorage.RecipeMarkInfos.Remove(recipeIndex);
            }
        }

        public static void DoOldBookmarkOrganizerFailsfe()
        {
            if (!StaticStorage.BookmarkOrganizerOldVersionInstalled) return;

            var allRecipesList = RecipeBook.Instance.savedRecipes;
            var intList = new List<int>();
            var shouldRearrange = false;
            for (int i = 0; i < allRecipesList.Count; i++)
            {
                if (StaticStorage.RecipeIndexes[i] == allRecipesList[i])
                {
                    intList.Add(i);
                }
                else
                {
                    shouldRearrange = true;
                    intList.Add(StaticStorage.RecipeIndexes.IndexOf(allRecipesList[i]));
                }
            }
            if (!shouldRearrange) return;
            BookmarksRearranged(null, intList);
        }
    }
}
