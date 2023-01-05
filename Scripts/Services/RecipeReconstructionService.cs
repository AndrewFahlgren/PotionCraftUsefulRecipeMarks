using HarmonyLib;
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
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public static class RecipeReconstructionService
    {

        public static Potion GetPotionForRecipeMark(int recipeIndex, int recipeMarkIndex)
        {
            if (!StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex)) return null;

            var recipeToClone = Managers.Potion.recipeBook.savedRecipes[recipeIndex];

            var recipeDeltas = StaticStorage.RecipeMarkInfos[recipeIndex];
            var potionState = new Dictionary<DeltaProperty, BaseDelta>();
            var reconstructionTimeline = new ReconstructionTimeline();

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
                        if (delta.Property == DeltaProperty.FixedHints)
                        {
                            ((ListDelta)potionState[delta.Property]).AddDeltas.ForEach(addFixedHintDeltaToTimeline);
                        }
                        return;
                    }
                    switch (delta)
                    {
                        case ListDelta listDelta:
                            var existingListDelta = (ListDelta)potionState[delta.Property];
                            listDelta.AddDeltas.ForEach(addDelta =>
                            {
                                processListAddDelta(addDelta, existingListDelta);
                            });
                            break;
                        case ListAddDelta addDelta:
                            processListAddDelta(addDelta, (ListDelta)potionState[delta.Property]);
                            break;
                    }

                    void processListAddDelta(ListAddDelta addDelta, ListDelta existingListDelta)
                    {
                        var curIndex = addDelta.Index;
                        if (addDelta.Property == DeltaProperty.FixedHints)
                        {
                            if (potionState.TryGetValue(DeltaProperty.PathAddedFixedHints, out BaseDelta existingPathAddedFixedHints))
                            {
                                var finalIngredientIndex = ((ModifyDelta<int>)existingPathAddedFixedHints).NewValue - 1;

                                //This ingredient may have been deleted with void salt. If this is the case ignore this added ingredient.
                                if (curIndex > finalIngredientIndex) return;

                                //Check to see if we have already found enough ingredients to satisfy the selected recipe mark
                                if (potionState.TryGetValue(DeltaProperty.PathFixedHintsCount, out BaseDelta existingPathFixedHintsCount))
                                {
                                    if (curIndex < finalIngredientIndex - ((ModifyDelta<int>)existingPathFixedHintsCount).NewValue + 1) return;
                                }
                            }

                            addFixedHintDeltaToTimeline(addDelta);
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

                    void addFixedHintDeltaToTimeline(ListAddDelta addDelta)
                    {
                        if (!reconstructionTimeline.FixedHintTimelines.TryGetValue(addDelta.Index, out var fixedHintTimeline))
                        {
                            fixedHintTimeline = new FixedHintTimeline
                            {
                                FixedHintIndex = addDelta.Index
                            };
                            reconstructionTimeline.FixedHintTimelines[addDelta.Index] = fixedHintTimeline;
                        }
                        fixedHintTimeline.AddInformationFromDeltas(recipeToClone.potionFromPanel.recipeMarks, i, addDelta.Deltas);
                    }
                });

                reconstructionTimeline.AddBasePropertyInformationFromDeltas(recipeToClone.potionFromPanel.recipeMarks, i, curRecipeMarkDeltas.Deltas);
            }

            StaticStorage.SelectedRecipePotionState = potionState;
            DebugLogObject(potionState);
            DebugLogObject(reconstructionTimeline);

            var newRecipe = recipeToClone.Clone();

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

            //Setup new variables
            var oldFixedHintsIndexes = new List<int>();
            var fixedPathHints = new List<FixedHint>();
            var dummyInteractionObject = new GameObject("dummyObject");
            var connectionPoint = Vector2.zero;

            //Reset the path information so we can start from a fresh slate
            Managers.RecipeMap.path.fixedPathHints = fixedPathHints;
            Managers.RecipeMap.path.deletedGraphicsSegments = 0.0f;
            Managers.RecipeMap.path.SetPositionOnMap(Managers.RecipeMap.currentMap.potionBaseMapItem.transform.localPosition);

            //Handle serialized paths from old recipes. This could happen if the player continues brewing from an old recipe and then saves it post mod installation.
            if (potionState.ContainsKey(DeltaProperty.OldSerializedPath))
            {
                var oldSerializedPath = (ModifyDelta<SerializedPath>)potionState[DeltaProperty.OldSerializedPath];
                oldFixedHintsIndexes.AddRange(deltaFixedHints.AddDeltas
                                                             .Where(fh => fh.Deltas.Any(d => d.Property == DeltaProperty.IsOldFixedHint))
                                                             .Select(fh => fh.Index)
                                                             .ToList());

                var oldSerializedPathFixedHints = oldSerializedPath.NewValue.fixedPathPoints.ToList();
                //Remove any fixed paths which are gone by this point
                for (var i = oldSerializedPathFixedHints.Count; i >= 0; i--)
                {
                    if (oldFixedHintsIndexes.Contains(i)) continue;
                    oldSerializedPath.NewValue.fixedPathPoints.RemoveAt(i);
                }

                //Feed in the old path data to construct the starting path
                Managers.RecipeMap.path.ShowLoadedFixedPathHints(oldSerializedPath.NewValue);
                oldSerializedPath.NewValue.fixedPathPoints = oldSerializedPathFixedHints;
            }

            var isFirst = true;
            //Go through each fixed hint delta and construct that portion of the path
            foreach (var fixedHintDelta in deltaFixedHints.AddDeltas)
            {
                var index = fixedHintDelta.Index;

                if (!isFirst)
                {
                    var fixedHintRotationEvents = reconstructionTimeline.GetRotationEventsToApplyBeforeAddingFixedHint(index);
                    fixedHintRotationEvents.ForEach(rotation => RotatePath(rotation));
                }

                var fixedHintTimeline = reconstructionTimeline.FixedHintTimelines[index];
                var ingredientName = fixedHintTimeline.IngredientName;
                var grindPercent = fixedHintTimeline.GrindPercent;

                if (isFirst)
                {
                    connectionPoint = ((ModifyDelta<Vector2>)fixedHintDelta.Deltas.First(d => d.Property == DeltaProperty.FixedHint_ConnectionPoint)).NewValue;
                }

                isFirst = false;

                var isOldFixedHint = oldFixedHintsIndexes.Contains(fixedHintDelta.Index);
                if (!isOldFixedHint)
                {
                    var ingredient = Ingredient.GetByName(ingredientName);

                    Managers.RecipeMap.path.potionComponentHintPainter.ShowIngredientHint(false, 0.0f, dummyInteractionObject, ingredient, grindPercent);
                    Managers.RecipeMap.path.AddCurrentPathToFixedPath(ingredient);
                }

                var deletionEvents = fixedHintTimeline.GetPathDeletionEvents(recipeToClone.potionFromPanel.recipeMarks);

                deletionEvents.ForEach(deletionEvent =>
                {
                    var segmentLengthToDelete = deletionEvent.Item2;
                    var deleteFromEnd = deletionEvent.Item1;
                    if (segmentLengthToDelete > 0)
                    {
                        if (deleteFromEnd)
                        {
                            //Round segment legnth to known void salt delete per grain to ensure exact matching of path length
                            segmentLengthToDelete = MathF.Round(segmentLengthToDelete - 0.099f, 1, MidpointRounding.AwayFromZero);
                            if (segmentLengthToDelete > 0)
                            {
                                Managers.RecipeMap.path.DeleteSegmentFromEnd(segmentLengthToDelete);
                            }
                        }
                        else
                        {
                            Traverse.Create(Managers.RecipeMap.path).Method("DeleteFromFixedPath", segmentLengthToDelete, Managers.RecipeMap.path.fixedPathHints.First()).GetValue();
                        }
                    }
                });
            }

            var rotationEvents = reconstructionTimeline.GetRotationEventsToApplyAfterAllFixedHints();
            rotationEvents.ForEach(rotation => RotatePath(rotation));

            //Serialize the path
            var serializedPath = SerializedPath.GetPathFromCurrentPotion();

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
                        newRecipe.potionFromPanel.serializedPath.pathPosition = ((ModifyDelta<Vector2>)propertyDeltaValue).NewValue;
                        var offset = (Vector3)(connectionPoint - newRecipe.potionFromPanel.serializedPath.pathPosition);
                        newRecipe.potionFromPanel.serializedPath.fixedPathPoints.ForEach(point =>
                        {
                            point.graphicsPoints = point.graphicsPoints.Select(p => p + offset).ToList();
                            point.physicsPoints = point.physicsPoints.Select(p => p + offset).ToList();
                            point.dotsPoints = point.dotsPoints.Select(p => p + offset).ToList();
                        });
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

        private static void RotatePath(Quaternion rotation)
        {
            if (rotation != Quaternion.identity)
            {
                var localPosition = Managers.RecipeMap.path.thisTransform.localPosition;
                var rotationMatrix = Matrix4x4.Translate(-localPosition) * Matrix4x4.Rotate(rotation) * Matrix4x4.Translate(localPosition);
                var rotateByMethod = typeof(FixedHint).GetMethod("RotateBy", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                Managers.RecipeMap.path.fixedPathHints.ForEach(f => rotateByMethod.Invoke(f, new object[] { rotationMatrix }));
            }
        }

        private static void DebugLogObject(object obj)
        {
            Plugin.PluginLogger.LogMessage(JsonConvert.SerializeObject(obj, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
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
