using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.Potion.Entities;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
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
        static void Postfix(IRecipeBookPageContent __result, Potion __instance)
        {
            Ex.RunSafe(() => CopyImportantInfoToPotionInstance((Potion)__result, __instance));
        }

        /// <summary>
        /// This method is copied from Pour Back In
        /// This method copies important information to the potion that is normally lost unless the potion is saved as a recipe
        /// This is important so we can use the potion just like a recipe later
        /// </summary>
        private static void CopyImportantInfoToPotionInstance(Potion copyTo, Potion copyFrom)
        {
            CopyImportantInfoToPotionInstance(copyTo, copyFrom, copyFrom.GetRecipeData());
        }

        /// <summary>
        /// This method is copied from Pour Back In
        /// This method copies important information to the potion that is normally lost unless the potion is saved as a recipe
        /// This is important so we can use the potion just like a recipe later
        /// </summary>
        private static void CopyImportantInfoToPotionInstance(Potion copyTo, Potion copyFromPotion, SerializedPotionRecipeData copyFrom)
        {
            var copyToRecipeData = copyTo.GetRecipeData();
            var recipeMarks = copyToRecipeData.recipeMarks;
            recipeMarks.Clear();
            copyFrom.recipeMarks.ForEach(m => recipeMarks.Add(m.Clone()));
            copyToRecipeData.collectedPotionEffects.Clear();
            foreach (var collectedPotionEffect in copyFromPotion?.Effects ?? Managers.Potion.collectedPotionEffects)
            {
                if (collectedPotionEffect == null)
                    break;
                copyToRecipeData.collectedPotionEffects.Add(collectedPotionEffect.name);
            }
            copyToRecipeData.serializedPath = copyFrom.serializedPath;
            if (!copyTo.usedComponents?.GetSummaryComponents()?.Any() ?? false)
            {
                copyTo.usedComponents.Clear();
                var toAdd = Managers.Potion.PotionUsedComponents.GetSummaryComponents().Select(component => component.Clone()).ToList();
                toAdd.ForEach(uc => copyTo.usedComponents.Add(uc));
            }
            if (!copyFrom.usedComponents.components.Any())
            {
                copyTo.usedComponents.GetSummaryComponents().ForEach((component) =>
                {
                    copyFrom.usedComponents.components.Add(new SerializedAlchemySubstanceComponent
                    {
                        name = component.Component.name,
                        amount = component.Amount,
                        type = component.Type.ToString()
                    });
                });
            }
            copyToRecipeData.usedComponents = copyFrom.usedComponents;
            copyToRecipeData.skinSettings = copyFrom.skinSettings;
        }
    }
}
