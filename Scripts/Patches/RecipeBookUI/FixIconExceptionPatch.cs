using HarmonyLib;
using PotionCraft.ScriptableObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    public class FixIconExceptionPatch
    {
        [HarmonyPatch(typeof(Icon), "GetByName")]
        public class Icon_GetByName
        {
            static bool Prefix(ref Icon __result, string iconName)
            {
                return FixPotionIconException(ref __result, iconName);
            }
        }

        /// <summary>
        /// This method is copied from Alchemy Machine Recipes
        /// </summary>
        private static bool FixPotionIconException(ref Icon result, string iconName)
        {
            Icon tempResult = null;
            var returnValue = Ex.RunSafe(() =>
            {
                if (string.IsNullOrEmpty(iconName))
                {
                    tempResult = Icon.allIcons.FirstOrDefault();
                    return false;
                }
                return true;
            });
            result = tempResult;
            return returnValue;
        }
    }
}
