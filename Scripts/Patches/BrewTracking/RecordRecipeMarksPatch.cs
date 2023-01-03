using HarmonyLib;
using PotionCraft.ManagersSystem.Potion;
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
    public class RecordRecipeMarksPatch
    {
        [HarmonyPatch(typeof(RecipeMarksSubManager), "AddLadleMark")]
        public class RecipeMarksSubManager_AddLadleMark
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaRecordingService.RecordRecipeMarkInfo());
            }
        }

        [HarmonyPatch(typeof(RecipeMarksSubManager), "AddSpoonMark")]
        public class RecipeMarksSubManager_AddSpoonMark
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaRecordingService.RecordRecipeMarkInfo());
            }
        }

        [HarmonyPatch(typeof(PotionManager), "ApplyEffectToPotion")]
        public class PotionManager_ApplyEffectToPotion
        {
            static void Postfix()
            {
                Ex.RunSafe(() => DeltaRecordingService.RecordRecipeMarkInfo());
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
                Ex.RunSafe(() => DeltaRecordingService.RecordRecipeMarkInfo());
            }
        }

        private static void RecordIngredientMark(ScriptableObject componentObject)
        {
            //Potion bases and salt are handled differently
            if (componentObject is PotionBase || componentObject is Salt) return;
            DeltaRecordingService.RecordRecipeMarkInfo();
        }
    }
}
