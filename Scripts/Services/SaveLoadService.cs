using Newtonsoft.Json;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    /// <summary>
    /// Service responsible for 
    /// </summary>
    public static class SaveLoadService
    {
        public static void SetupListeners()
        {
            if (!StaticStorage.AddedListeners)
            {
                StaticStorage.AddedListeners = true;
                Managers.Potion.gameObject.AddComponent<UsefulRecipeMarksManager>();
                RecipeBook.Instance.bookmarkControllersGroupController.onBookmarksRearranged.AddListener(RecipeBookUIService.BookmarksRearranged);
                RecipeBook.Instance.onPageChanged.AddListener(RecipeBookUIService.UpdateRecipeMarksForPageChange);
                RecipeBook.Instance.onActiveStateChanged.AddListener(RecipeBookUIService.UpdateRecipeMarksForStateChanged);
                Managers.Potion.potionCraftPanel.onPotionUpdated.AddListener(DeltaRecordingService.ClearPreviousSavedPotionRecipeMarkInfo);
            }
        }

        /// <summary>
        /// Clears out any stored static data from a previous game file if this isn't the first load of the session
        /// </summary>
        public static void ClearFileSpecificDataOnFileLoad()
        {
            SetupListeners();
            StaticStorage.CurrentPotionRecipeMarkInfos.Clear();
            StaticStorage.CurrentPotionState.Clear();
            StaticStorage.CurrentRecipeMarkInfo = null;
            StaticStorage.PreviousPotionRecipeMarkInfo = null;
            StaticStorage.SelectedRecipePotionState?.Clear();
            StaticStorage.SelectedRecipeMarkIndex = 0;
            StaticStorage.SelectedRecipeIndex = 0;
            StaticStorage.RecipeMarkInfos.Clear();
        }

        public static void UpdateFailsafeListOnLoad()
        {
            if (!StaticStorage.BookmarkOrganizerOldVersionInstalled) return;
            StaticStorage.RecipeIndexes = [.. RecipeBook.Instance.savedRecipes];
        }
    }
}
