using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.PotionCraftPanel;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System.Linq;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches
{
    public class InjectBookmarkGroupingDataIntoSaveFilePatch
    { 
        [HarmonyPatch(typeof(SavedState), "ToJson")]
        public class SavedState_ToJson
        {
            static void Postfix(ref string __result)
            {
                SaveLoadService.StoreBookmarkGroups(ref __result);
            }
        }
    }
}
