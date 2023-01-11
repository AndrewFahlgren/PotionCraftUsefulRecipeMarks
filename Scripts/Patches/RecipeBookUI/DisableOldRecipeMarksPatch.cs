using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    public class DisableOldRecipeMarksPatch
    {
        [HarmonyPatch(typeof(RecipeBookRightPageContent), "UpdateRecipeMarks")]
        public class RecipeBookRightPageContent_UpdateRecipeMarks
        {
            static void Postfix(Dictionary<int, List<RecipeBookRecipeMark>> ___visibleMarks)
            {
                Ex.RunSafe(() => RecipeBookUIService.DisableOldRecipeMarks(___visibleMarks));
            }
        }
        
    }
}
