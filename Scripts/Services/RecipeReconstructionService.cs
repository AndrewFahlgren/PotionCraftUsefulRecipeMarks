using Newtonsoft.Json;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.RecipeMap;
using PotionCraft.ObjectBased.RecipeMap.Path;
using PotionCraft.SaveLoadSystem;
using PotionCraft.ScriptableObjects;
using PotionCraft.ScriptableObjects.Ingredient;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public static class RecipeReconstructionService
    {

        public static Potion GetPotionForRecipeMark(int recipeIndex, int recipeMarkIndex)
        {
            if (!StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex)) return null;

            var recipeDeltas = StaticStorage.RecipeMarkInfos[recipeIndex];
            var potionState = new Dictionary<DeltaProperty, BaseDelta>();

            for (var i = recipeMarkIndex; i >= 0; i--)
            {
                var curRecipeMarkDeltas = recipeDeltas[i];
                curRecipeMarkDeltas.Deltas.ForEach(delta =>
                {
                    if (!potionState.ContainsKey(delta.Property))
                    {
                        potionState[delta.Property] = delta switch
                        {
                            ListDelta listDelta => delta,
                            ListAddDelta addDelta => new ListDelta
                            {
                                Property = delta.Property,
                                AddDeltas = new List<ListAddDelta> { addDelta }
                            },
                            _ => delta,
                        };
                        return;
                    }
                    switch (delta)
                    {
                        case ListDelta listDelta:
                            var existingListDelta = (ListDelta)potionState[delta.Property];
                            listDelta.AddDeltas.ForEach(addDelta =>
                            {
                                ProcessListAddDelta(addDelta, potionState, curRecipeMarkDeltas, existingListDelta);
                            });
                            break;
                        case ListAddDelta addDelta:
                            ProcessListAddDelta(addDelta, potionState, curRecipeMarkDeltas, (ListDelta)potionState[delta.Property]);
                            break;
                    }
                });
            }

            DebugLogPotionState(potionState);

            var newRecipe = Managers.Potion.recipeBook.savedRecipes[recipeIndex].Clone();

            //Setup used components
            //Sort indexes from high to low for efficient searching
            var deltaUsedComponents = (ListDelta)potionState[DeltaProperty.UsedComponents];
            deltaUsedComponents.AddDeltas.Sort(SortAddDeltasByIndexDesc);
            for (var i = newRecipe.usedComponents.Count - 1; i > 0; i--)
            {
                var correspondingDelta = deltaUsedComponents.AddDeltas.FirstOrDefault(d => d.Index == i);
                if (correspondingDelta == null)
                {
                    newRecipe.usedComponents.RemoveAt(i);
                    continue;
                }
                Plugin.PluginLogger.LogMessage("1");
                var ammountDelta = (ModifyDelta<int>)correspondingDelta.Deltas.First(d => d.Property == DeltaProperty.UsedComponent_Ammount);
                newRecipe.usedComponents[i].amount = ammountDelta.NewValue;
            }

            //Sort the fixed hints deltas
            var deltaFixedHints = (ListDelta)potionState[DeltaProperty.FixedHints];
            deltaFixedHints.AddDeltas.Sort(SortAddDeltasByIndex);

            //Save the current path information so we can restore it later
            var oldActualPathFixedPathHints = Managers.RecipeMap.path.fixedPathHints;
            var oldDeletedGraphicsSegments = Managers.RecipeMap.path.deletedGraphicsSegments;
            var oldPathPos = Managers.RecipeMap.path.transform.localPosition;

            //Reset the path information so we can start from a fresh slate
            var fixedPathHints = new List<FixedHint>();
            Managers.RecipeMap.path.fixedPathHints = fixedPathHints;
            Managers.RecipeMap.path.deletedGraphicsSegments = 0.0f;
            Managers.RecipeMap.path.SetPositionOnMap(((ModifyDelta<Vector2>)potionState[DeltaProperty.PathPosition]).NewValue);
            var dummyInteractionObject = new GameObject("dummyObject");

            var isFirst = true;
            var connectionPoint = Vector2.zero;
            //Go through each fixed hint delta and construct that portion of the path
            foreach (var fixedHintDelta in deltaFixedHints.AddDeltas)
            {
                Plugin.PluginLogger.LogMessage("2");
                var ingredientName = (ModifyDelta<string>)fixedHintDelta.Deltas.First(d => d.Property == DeltaProperty.FixedHint_IngredientName);
                Plugin.PluginLogger.LogMessage("3");
                var grindPercent = (ModifyDelta<float>)fixedHintDelta.Deltas.First(d => d.Property == DeltaProperty.FixedHint_GrindPercent);
                Plugin.PluginLogger.LogMessage("4");
                var length = (ModifyDelta<float>)fixedHintDelta.Deltas.First(d => d.Property == DeltaProperty.FixedHint_Length);
                Plugin.PluginLogger.LogMessage("5");

                if (isFirst)
                {
                    connectionPoint = ((ModifyDelta<Vector2>)fixedHintDelta.Deltas.First(d => d.Property == DeltaProperty.FixedHint_ConnectionPoint)).NewValue;
                }

                var ingredient = Ingredient.GetByName(ingredientName.NewValue);

                Managers.RecipeMap.path.potionComponentHintPainter.ShowIngredientHint(false, 0.0f, dummyInteractionObject, ingredient, grindPercent.NewValue);
                Managers.RecipeMap.path.AddCurrentPathToFixedPath(ingredient);
                if (isFirst)
                {
                    Managers.RecipeMap.path.DeleteSegment(((ModifyDelta<float>)potionState[DeltaProperty.PathDeletedSegments]).NewValue);
                }
                var newFixedHint = Managers.RecipeMap.path.fixedPathHints.Last();
                var segmentLengthToDelete = newFixedHint.GetPathLength() - length.NewValue;
                if (segmentLengthToDelete > 0)
                {
                    Managers.RecipeMap.path.DeleteSegmentFromEnd(segmentLengthToDelete);
                }
                isFirst = false;
            }

            //Rotate the path if needed
            var rotation = ((ModifyDelta<Quaternion>)potionState[DeltaProperty.PathRotation]).NewValue;
            if (rotation != Quaternion.identity)
            {
                var localPosition = Managers.RecipeMap.path.thisTransform.localPosition;
                var rotationMatrix = Matrix4x4.Translate(-localPosition) * Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(localPosition);
                var rotateByMethod = typeof(FixedHint).GetMethod("RotateBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Managers.RecipeMap.path.fixedPathHints.ForEach(f => rotateByMethod.Invoke(f, new object[] { rotationMatrix }));
            }

            //Serialize the path
            var serializedPath = ProgressState.SerializedPath.GetPathFromCurrentPotion();

            //Update the recipe with the new path
            newRecipe.potionFromPanel.serializedPath = serializedPath;

            UnityEngine.Object.Destroy(dummyInteractionObject);
            fixedPathHints.ForEach(fh => fh.DestroyPath());

            //Restore the path map object's path
            Managers.RecipeMap.path.fixedPathHints = oldActualPathFixedPathHints;
            Managers.RecipeMap.path.deletedGraphicsSegments = oldDeletedGraphicsSegments;
            Managers.RecipeMap.path.SetPositionOnMap(oldPathPos);

            //Apply all other properties
            potionState.ToList().ForEach(propertyDelta =>
            {
                var propertyDeltaValue = propertyDelta.Value;
                switch (propertyDelta.Key)
                {
                    case DeltaProperty.IndicatorPosition:
                        newRecipe.potionFromPanel.serializedPath.indicatorPosition = ((ModifyDelta<Vector2>)propertyDeltaValue).NewValue;
                        break;
                    case DeltaProperty.PathPosition:
                        newRecipe.potionFromPanel.serializedPath.pathPosition = connectionPoint;//((ModifyDelta<Vector2>)propertyDeltaValue).NewValue;
                        break;
                    case DeltaProperty.Rotation:
                        newRecipe.potionFromPanel.serializedPath.indicatorRotationValue = ((ModifyDelta<float>)propertyDeltaValue).NewValue;
                        break;
                    case DeltaProperty.Health:
                        newRecipe.potionFromPanel.serializedPath.health = ((ModifyDelta<float>)propertyDeltaValue).NewValue;
                        break;
                    case DeltaProperty.Effects:
                        newRecipe.Effects = ((ModifyDelta<List<string>>)propertyDeltaValue).NewValue.Select(e => PotionEffect.GetByName(e)).ToArray();
                        break;
                    case DeltaProperty.PathDeletedSegments:
                        newRecipe.potionFromPanel.serializedPath.deletedGraphicsSegments = ((ModifyDelta<float>)propertyDeltaValue).NewValue;
                        break;
                }
            });

            return newRecipe;
        }

        private static void DebugLogPotionState(Dictionary<DeltaProperty, BaseDelta> potionState)
        {
            Plugin.PluginLogger.LogMessage(JsonConvert.SerializeObject(potionState, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        }

        private static void ProcessListAddDelta(ListAddDelta addDelta, Dictionary<DeltaProperty, BaseDelta> potionState, RecipeMarkInfo curRecipeMarkDeltas, ListDelta existingListDelta)
        {
            var curIndex = addDelta.Index;
            if (addDelta.Property == DeltaProperty.FixedHints)
            {
                if (potionState.TryGetValue(DeltaProperty.PathAddedFixedHints, out BaseDelta existingPathAddedFixedHints))
                {
                    var finalIngredientTrueIndex = ((ModifyDelta<int>)existingPathAddedFixedHints).NewValue;

                    //This ingredient may have been deleted with void salt. If this is the case ignore this added ingredient.
                    if (curIndex > finalIngredientTrueIndex) return;

                    //Check to see if we have already found enough ingredients to satisfy the selected recipe mark
                    if (potionState.TryGetValue(DeltaProperty.PathFixedHintsCount, out BaseDelta existingPathFixedHintsCount))
                    {
                        if (curIndex < finalIngredientTrueIndex - ((ModifyDelta<int>)existingPathFixedHintsCount).NewValue) return;
                    }
                }
            }

            var existingIndexDelta = existingListDelta.AddDeltas.FirstOrDefault(d => d.Index == curIndex);
            if (existingIndexDelta == null)
            {
                existingListDelta.AddDeltas.Add(addDelta);
                return;
            }
            addDelta.Deltas.ForEach(modifyDelta =>
            {
                var existingModifyDelta = existingIndexDelta.Deltas.FirstOrDefault(d => d.Property == modifyDelta.Property);
                if (existingModifyDelta != null) return;
                existingIndexDelta.Deltas.Add(modifyDelta);
            });
        }

        public static int SortAddDeltasByIndex(ListAddDelta ad1, ListAddDelta ad2)
        {
            return ad1.Index > ad2.Index ? 1 : (ad1.Index == ad2.Index ? 0 : -1);
        }

        public static int SortAddDeltasByIndexDesc(ListAddDelta ad1, ListAddDelta ad2)
        {
            return -SortAddDeltasByIndex(ad1, ad2);
        }
    }
}
