﻿using HarmonyLib;
using PotionCraft.ObjectBased.UIElements;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    public class UpdateRecipeBookPageForSelectedRecipeMarkPatch
    {
        [HarmonyPatch(typeof(Button), "OnClick")]
        public class Button_OnClick
        {
            static void Postfix(Button __instance)
            {
                Ex.RunSafe(() => RecipeBookUIService.UpdateRecipeBookPageForSelectedRecipeMark(__instance));
            }
        }

        [HarmonyPatch(typeof(RecipeBookRecipeMark), "AssignMark")]
        public class RecipeBookRecipeMark_AssignMark
        {
            static void Postfix(RecipeBookRecipeMark __instance)
            {
                Ex.RunSafe(() => RecipeBookUIService.EnableDisableMark(__instance));
            }
        }
    }
}
