using HarmonyLib;
using PotionCraft.ObjectBased.UIElements.Books;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    public class DisableOldRecipeMarksPatch
    {
        //[HarmonyPatch(typeof(Book), "UpdateCurrentPageIndex")]
        //public class Book_UpdateCurrentPageIndex
        //{
        //    static void Postfix(Book __instance)
        //    {
        //        Ex.RunSafe(() => RecipeBookUIService.DisableOldRecipeMarks(__instance));
        //    }
        //}

        //[HarmonyPatch(typeof(Book), "OpenPageAt")]
        //public class Book_OpenPageAt
        //{
        //    static void Postfix(Book __instance)
        //    {
        //        Ex.RunSafe(() => RecipeBookUIService.DisableOldRecipeMarks(__instance));
        //    }
        //}

        //[HarmonyPatch(typeof(CurlPageController), "OnPageFlippingEnd")]
        //public class CurlPageController_OnPageFlippingEnd
        //{
        //    static void Postfix(Book ___book)
        //    {
        //        Ex.RunSafe(() => RecipeBookUIService.DisableOldRecipeMarks(___book));
        //    }
        //}

    }
}
