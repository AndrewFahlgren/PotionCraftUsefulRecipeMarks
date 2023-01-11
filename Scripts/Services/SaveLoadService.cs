using Newtonsoft.Json;
using PotionCraft.ManagersSystem;
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
            }
        }
        /// <summary>
        /// Clears out any stored static data from a previous game file if this isn't the first load of the session
        /// </summary>
        public static void ClearFileSpecificDataOnFileLoad()
        {
            StaticStorage.CurrentPotionRecipeMarkInfos.Clear();
            StaticStorage.CurrentPotionState.Clear();
            StaticStorage.CurrentRecipeMarkInfo = null;
            StaticStorage.SelectedRecipePotionState.Clear();
            StaticStorage.SelectedRecipeMarkIndex = 0;
            StaticStorage.RecipeMarkInfos.Clear();
        }
    }
}
