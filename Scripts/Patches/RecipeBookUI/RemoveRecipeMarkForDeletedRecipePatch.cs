using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    public class RemoveRecipeMarkForDeletedRecipePatch
    {

        [HarmonyPatch(typeof(RecipeBook), "EraseRecipe")]
        public class RemoveRecipeMarkForDeletedRecipe
        {
            static void Prefix(Potion potion)
            {
                Ex.RunSafe(() => RecipeBookUIService.RemoveRecipeMarkForDeletedRecipe(potion));
            }
        }
    }
}
