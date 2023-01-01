using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.ObjectBased.RecipeMap;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.ScriptableObjects;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using System;
using System.IO;
using System.Linq;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches
{
    public class LoadBookmarkGroupingDataFromSaveFilePatch
    { 
        [HarmonyPatch(typeof(SaveLoadManager), "LoadSelectedState")]
        public class SaveLoadManager_LoadSelectedState
        {
            static bool Prefix(Type type)
            {
                return Ex.RunSafe(() => SaveLoadService.RetreiveStoredBookmarkGroups(type));
            }
        }
    }
}
