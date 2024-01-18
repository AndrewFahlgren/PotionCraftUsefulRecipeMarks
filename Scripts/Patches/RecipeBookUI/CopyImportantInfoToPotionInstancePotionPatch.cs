using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ScriptableObjects.Potion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches.RecipeBookUI
{
    [HarmonyPatch(typeof(Potion), "Clone")]
    public class CopyImportantInfoToPotionInstancePotionPatch
    {
        static void Postfix(Potion __result, Potion __instance)
        {
            Ex.RunSafe(() => CopyImportantInfoToPotionInstance(__result, __instance));
        }

        /// <summary>
        /// This method is copied from Pour Back In
        /// This method copies important information to the potion that is normally lost unless the potion is saved as a recipe
        /// This is important so we can use the potion just like a recipe later
        /// </summary>
        private static void CopyImportantInfoToPotionInstance(Potion copyTo, Potion copyFrom)
        {
            CopyImportantInfoToPotionInstance(copyTo, copyFrom, copyFrom.potionFromPanel);
        }

        /// <summary>
        /// This method is copied from Pour Back In
        /// This method copies important information to the potion that is normally lost unless the potion is saved as a recipe
        /// This is important so we can use the potion just like a recipe later
        /// </summary>
        private static void CopyImportantInfoToPotionInstance(Potion copyTo, Potion copyFromPotion, SerializedPotionFromPanel copyFrom)
        {
            var recipeMarks = copyTo.potionFromPanel.recipeMarks;
            recipeMarks.Clear();
            copyFrom.recipeMarks.ForEach(m => recipeMarks.Add(m.Clone()));
            copyTo.potionFromPanel.collectedPotionEffects.Clear();
            foreach (var collectedPotionEffect in copyFromPotion?.Effects ?? Managers.Potion.collectedPotionEffects)
            {
                if (collectedPotionEffect == null)
                    break;
                copyTo.potionFromPanel.collectedPotionEffects.Add(collectedPotionEffect.name);
            }
            copyTo.potionFromPanel.serializedPath = copyFrom.serializedPath;
            if (!copyTo.usedComponents?.Any() ?? false)
            {
                if (copyTo.usedComponents == null) copyTo.usedComponents = new List<PotionUsedComponent>();
                copyTo.usedComponents = Managers.Potion.usedComponents.Select(component => component.Clone()).ToList();
            }
            if (!copyFrom.potionUsedComponents.Any())
            {
                copyTo.usedComponents.ForEach((component) =>
                {
                    copyFrom.potionUsedComponents.Add(new SerializedUsedComponent
                    {
                        componentName = component.componentObject.name,
                        componentAmount = component.amount,
                        componentType = component.componentType.ToString()
                    });
                });
            }
            copyTo.potionFromPanel.potionUsedComponents = copyFrom.potionUsedComponents;
            copyTo.potionFromPanel.potionSkinSettings = copyFrom.potionSkinSettings;
        }
    }
}
