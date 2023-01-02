using HarmonyLib;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraft.ScriptableObjects.Salts;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static PotionCraft.ManagersSystem.Potion.PotionManager;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.BrewTracking
{
    public class RecordRecipeMarks
    {
        [HarmonyPatch(typeof(RecipeMarksSubManager), "AddStringMark")]
        public class RecipeMarksSubManager_AddStringMark
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaService.RecordRecipeMarkInfo());
            }
        }

        [HarmonyPatch(typeof(RecipeMarksSubManager), "AddFloatMark")]
        public class RecipeMarksSubManager_AddFloatMark
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaService.RecordRecipeMarkInfo());
            }
        }

        [HarmonyPatch(typeof(PotionUsedComponent), "AddToList")]
        public class RecipeMarksSubManager_AddIngredientMark
        {
            static void Postfix(ScriptableObject componentObject)
            {
                Ex.RunSafe(() => RecordIngredientMark(componentObject));
            }
        }

        [HarmonyPatch(typeof(RecipeMarksSubManager), "AddSaltMark")]
        public class RecipeMarksSubManager_AddSaltMark
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaService.RecordRecipeMarkInfo());
            }
        }

        private static void RecordIngredientMark(ScriptableObject componentObject)
        {
            //Potion bases and salt are handled differently
            if (componentObject is PotionBase || componentObject is Salt) return;
            DeltaService.RecordRecipeMarkInfo();
        }
    }
}
