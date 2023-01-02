using HarmonyLib;
using PotionCraft.SaveFileSystem;
using PotionCraftUsefulRecipeMarks.Scripts.Services;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches
{
    public class RetrieveStateJsonStringPatch
    {
        //[HarmonyPatch(typeof(File), "Load")]
        //public class File_Load
        //{
        //    static bool Prefix(File __instance)
        //    {
        //        return Ex.RunSafe(() => SaveLoadService.RetrieveStateJsonString(__instance));
        //    }
        //}
    }
}
