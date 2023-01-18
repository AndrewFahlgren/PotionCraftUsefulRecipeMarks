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
        public static bool MarkHasSavedData(int recipeIndex, int recipeMarkIndex)
        {
            if (!StaticStorage.RecipeMarkInfos.TryGetValue(recipeIndex, out var recipeMarkList)) return false;
            return recipeMarkList.ContainsKey(recipeMarkIndex);
        }

        public static Potion GetPotionForRecipeMark(int recipeIndex, int recipeMarkIndex)
        {
            if (!StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex)) return null;

            var recipeToClone = Managers.Potion.recipeBook.savedRecipes[recipeIndex];

            //Do not clone the recipe if the last mark is selected. Instead return the original recipe;
            if (recipeToClone.potionFromPanel.recipeMarks.Count - 1 == recipeMarkIndex)
            {
                return recipeToClone;
            }

            var recipeDeltas = StaticStorage.RecipeMarkInfos[recipeIndex];
            var potionState = new Dictionary<DeltaProperty, BaseDelta>();
            var reconstructionTimeline = new ReconstructionTimeline();
            var lowestIngredientIndex = -1;
            var ignoreFixedHintIndexes = new List<int>();
            int? previousAddedIngredients = null;
            var oldRecipeIndex = 0;

            for (var i = recipeMarkIndex; i >= 0; i--)
            {
                if (!recipeDeltas.TryGetValue(i, out var curRecipeMarkDeltas))
                {
                    if (oldRecipeIndex == 0) oldRecipeIndex = i;
                    continue;
                }

                //For newly saved recipes which were continued from old ones its important that we move up the zero mark starting index to the actual starting index
                if (i == 0) i = oldRecipeIndex;

                curRecipeMarkDeltas.Deltas.ForEach(delta =>
                {
                    if (!potionState.ContainsKey(delta.Property))
                    {
                        potionState[delta.Property] = delta switch
                        {
                            ListDelta listDelta => delta,
                            ListAddDelta addDelta => new ListDelta
                            {
                                Property = delta.Property
                            },
                            _ => delta,
                        };
                        if (delta is ListAddDelta firstAddDelta)
                        {
                            processListAddDelta(firstAddDelta, (ListDelta)potionState[delta.Property]);
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
                            //This fixed hint was deleted by void salt in the future. Ignore this adddelta.
                            if (ignoreFixedHintIndexes.Contains(curIndex))
                            {
                                //Plugin.PluginLogger.LogInfo($"Returning for ignored index {i}, fixedHintIndex={curIndex}");
                                return;
                            }

                            if (!potionState.TryGetValue(DeltaProperty.PathAddedFixedHints, out BaseDelta existingPathAddedFixedHints))
                                existingPathAddedFixedHints = curRecipeMarkDeltas.Deltas.FirstOrDefault(d => d.Property == DeltaProperty.PathAddedFixedHints) as ModifyDelta<int>;

                            if (existingPathAddedFixedHints != null)
                            {
                                var existingAddedIngredients = ((ModifyDelta<int>)existingPathAddedFixedHints).NewValue;

                                //When we are navigating backwards this value should never increase. If it does a void salt deletion occured
                                var currentAddedIngredients = curRecipeMarkDeltas.Deltas.FirstOrDefault(d => d.Property == DeltaProperty.PathAddedFixedHints) as ModifyDelta<int>;
                                if (currentAddedIngredients != null)
                                {
                                    //For old recipes for whatever reason this value is not dependible for the first record
                                    //Just grab the fixed hint list instead
                                    if (i == oldRecipeIndex)
                                    {
                                        currentAddedIngredients.NewValue = ((ListDelta)curRecipeMarkDeltas.Deltas.FirstOrDefault(d => d.Property == DeltaProperty.FixedHints))?.AddDeltas?.Count ?? currentAddedIngredients.NewValue;
                                    }

                                    if (previousAddedIngredients != null && currentAddedIngredients.NewValue > previousAddedIngredients)
                                    {
                                        //At this point everything referring to the latest index should be ignored since that ingredient was fully deleted by void salt
                                        ignoreFixedHintIndexes.Add(curIndex);
                                        //Plugin.PluginLogger.LogInfo($"Returning for void salt index {i}, fixedHintIndex={curIndex}");
                                        return;
                                    }
                                    previousAddedIngredients = currentAddedIngredients.NewValue;
                                }
                                var finalIngredientIndex = existingAddedIngredients - 1;

                                //This ingredient may have been deleted with void salt. If this is the case ignore this added ingredient.
                                if (curIndex > finalIngredientIndex)
                                {
                                    //Plugin.PluginLogger.LogInfo($"Returning for deleted void salt index {i}, fixedHintIndex={curIndex}");
                                    return;
                                }

                                //Check to see if we have already found enough ingredients to satisfy the selected recipe mark
                                if (potionState.TryGetValue(DeltaProperty.PathFixedHintsCount, out BaseDelta existingPathFixedHintsCount))
                                {
                                    lowestIngredientIndex = finalIngredientIndex - ((ModifyDelta<int>)existingPathFixedHintsCount).NewValue + 1;
                                    if (curIndex < lowestIngredientIndex)
                                    {
                                        //Plugin.PluginLogger.LogInfo($"Returning for too low index {i}, fixedHintIndex={curIndex}");
                                        return;
                                    }
                                }
                            }

                            addFixedHintDeltaToTimeline(addDelta);
                            //Plugin.PluginLogger.LogInfo($"Processing fixed hint add delta for index {i}, fixedHintIndex={curIndex}");
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

                //Fix our index manipulation from earlier
                if (i == oldRecipeIndex) i = 0;
            }

            var fixedHints = (ListDelta)potionState[DeltaProperty.FixedHints];
            var fixedHintCount = ((ModifyDelta<int>)potionState[DeltaProperty.PathFixedHintsCount]).NewValue;
            //Due to timing issues when recording potion state we may have added additional fixed hints. Remove all of those here.
            fixedHints.AddDeltas = fixedHints.AddDeltas.OrderBy(d => d.Index).Take(fixedHintCount).ToList();

            //We may have added information for some fixed hints which were not actually a part of the final path
            fixedHints.AddDeltas.RemoveAll(d => d.Index < lowestIngredientIndex);
            reconstructionTimeline.FixedHintTimelines.Where(fht => fht.Key < lowestIngredientIndex)
                                                     .ToList()
                                                     .ForEach(fht => reconstructionTimeline.FixedHintTimelines.Remove(fht.Key));

            StaticStorage.SelectedRecipePotionState = potionState;
            //DebugLogObject(potionState);
            //DebugLogObject(reconstructionTimeline);

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
                var ammountDelta = (ModifyDelta<int>)correspondingDelta.Deltas.First(d => d.Property == DeltaProperty.UsedComponent_Ammount);
                newRecipe.usedComponents[i].amount = ammountDelta.NewValue;
            }
            newRecipe.potionFromPanel.potionUsedComponents.Clear();
            newRecipe.usedComponents.ForEach(component => newRecipe.potionFromPanel.potionUsedComponents.Add(new SerializedUsedComponent()
            {
                componentName = component.componentObject.name,
                componentAmount = component.amount,
                componentType = component.componentType.ToString()
            }));

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
            var connectionPoint = Vector3.zero;
            var firstGeneratedPoint = Vector3.zero;

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
                for (var i = oldSerializedPathFixedHints.Count - 1; i >= 0; i--)
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
                reconstructionTimeline.FixedHintTimelines.TryGetValue(index, out var fixedHintTimeline);

                var isOldFixedHint = oldFixedHintsIndexes.Contains(fixedHintDelta.Index);
                if (!isOldFixedHint)
                {
                    var ingredientName = fixedHintTimeline.IngredientName;
                    var grindPercent = fixedHintTimeline.GrindPercent;
                    var ingredient = Ingredient.GetByName(ingredientName);

                    Managers.RecipeMap.path.potionComponentHintPainter.ShowIngredientHint(false, 0.0f, dummyInteractionObject, ingredient, grindPercent);
                    Managers.RecipeMap.path.AddCurrentPathToFixedPath(ingredient);
                }

                if (isFirst)
                {
                    connectionPoint = ((ModifyDelta<(float x, float y)>)fixedHintDelta.Deltas.First(d => d.Property == DeltaProperty.FixedHint_ConnectionPoint)).NewValue.ToVector();
                    firstGeneratedPoint = Managers.RecipeMap.path.fixedPathHints.First().evenlySpacedPointsFixedGraphics.points.First();
                }

                if (fixedHintTimeline == null) continue;

                //Run through all path events in order to manipulate the path correctly
                var deletionEvents = reconstructionTimeline.GetPathDeletionEvents(recipeToClone.potionFromPanel.recipeMarks, index);
                var maxDeletionIndex = deletionEvents.Any() ? deletionEvents.Max(d => d.index) : -1;
                var rotationEvents = reconstructionTimeline.GetRotationEventsForFixedHint(index);
                var maxRotationIndex = rotationEvents.Any() ? rotationEvents.Max(d => d.index) : -1;
                var maxEventIndex = Mathf.Max(maxDeletionIndex, maxRotationIndex);
                //Plugin.PluginLogger.LogMessage($"Events for fixedHintIndex={index}, deletionEvents.Count:{deletionEvents.Count}, maxDeletionIndex={maxDeletionIndex}, maxEventIndex={maxEventIndex}");
                for (var i = 0; i <= maxEventIndex; i++)
                {
                    var anyDeletion = deletionEvents.Any(e => e.index == i);
                    var anyRotation = rotationEvents.Any(e => e.index == i);
                    if (anyDeletion)
                    {
                        var deletionEvent = deletionEvents.First(e => e.index == i);
                        var segmentLengthToDelete = deletionEvent.length;
                        var deleteFromEnd = deletionEvent.fromEnd;
                        if (segmentLengthToDelete > 0.0001f)
                        {
                            //Plugin.PluginLogger.LogMessage($"Deletion event at index: {i} (fixedHintIndex={index}), segmentLengthToDelete={segmentLengthToDelete}");
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
                    }
                    if (anyRotation)
                    {
                        var rotationEvent = rotationEvents.First(e => e.index == i);
                        var currentPosition = Managers.RecipeMap.path.fixedPathHints[0].evenlySpacedPointsFixedGraphics.points[0];
                        //Plugin.PluginLogger.LogMessage($"Rotation event at index: {i} (fixedHintIndex={index}), currentPosition:{currentPosition}");
                        RotatePath(rotationEvent.rotation, -currentPosition);
                    }
                }
                isFirst = false;
            }

            var offset = connectionPoint - firstGeneratedPoint;

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
            potionState.ToList().ForEach(propertyDelta => //TODO for some reason vector2s are getting in here
            {
                var propertyDeltaValue = propertyDelta.Value;
                switch (propertyDelta.Key)
                {
                    case DeltaProperty.IndicatorPosition:
                        newRecipe.potionFromPanel.serializedPath.indicatorPosition = ((ModifyDelta<(float x, float y)>)propertyDeltaValue).NewValue.ToVector();
                        break;
                    case DeltaProperty.PathPosition:
                        var pathPosition = ((ModifyDelta<(float x, float y)>)propertyDeltaValue).NewValue.ToVector();
                        newRecipe.potionFromPanel.serializedPath.pathPosition = pathPosition;

                        if (deltaFixedHints.AddDeltas.Any())
                        {
                            if (offset != Vector3.zero)
                            {
                                newRecipe.potionFromPanel.serializedPath.fixedPathPoints.ForEach(point =>
                                {
                                    point.graphicsPoints = point.graphicsPoints.Select(p => p + offset).ToList();
                                    point.physicsPoints = point.physicsPoints.Select(p => p + offset).ToList();
                                    point.dotsPoints = point.dotsPoints.Select(p => p + offset).ToList();
                                });
                            }
                        }
                        else
                        {
                            newRecipe.potionFromPanel.serializedPath.fixedPathPoints.Add(new SerializedPathPoints
                            {
                                graphicsPoints = new List<Vector3> { -newRecipe.potionFromPanel.serializedPath.pathPosition },
                                physicsPoints = new List<Vector3> { -newRecipe.potionFromPanel.serializedPath.pathPosition },
                                pathEndParameters = new PathEndParameters { spriteIndex = 2 },
                                pathStartParameters = new PathEndParameters { spriteIndex = 2 }
                            });
                        }
                        break;
                    case DeltaProperty.IndicatorTargetPosition:
                        newRecipe.potionFromPanel.serializedPath.indicatorTargetPosition = ((ModifyDelta<(float x, float y)>)propertyDeltaValue).NewValue.ToVector();
                        break;
                    case DeltaProperty.FollowButtonTargetPosition:
                        newRecipe.potionFromPanel.serializedPath.followButtonTargetObjectPosition = ((ModifyDelta<(float x, float y)>)propertyDeltaValue).NewValue.ToVector();
                        break;
                    case DeltaProperty.Rotation:
                        newRecipe.potionFromPanel.serializedPath.indicatorRotationValue = ((ModifyDelta<float>)propertyDeltaValue).NewValue;
                        break;
                    case DeltaProperty.Health:
                        var newHealth = ((ModifyDelta<float>)propertyDeltaValue).NewValue;
                        if (newHealth > 0.01f)
                        {
                            newRecipe.potionFromPanel.serializedPath.health = newHealth;
                        }
                        break;
                    case DeltaProperty.Effects:
                        var effectsList = ((ModifyDelta<List<string>>)propertyDeltaValue).NewValue;
                        newRecipe.Effects = effectsList.Select(e => PotionEffect.GetByName(e)).ToArray();
                        newRecipe.potionFromPanel.collectedPotionEffects = effectsList.ToList();
                        break;
                    case DeltaProperty.PathDeletedSegments:
                        newRecipe.potionFromPanel.serializedPath.deletedGraphicsSegments = ((ModifyDelta<float>)propertyDeltaValue).NewValue;
                        break;
                }
            });

            return newRecipe;
        }

        private static void RotatePath(Quaternion rotation, Vector3 localPosition)
        {
            if (rotation != Quaternion.identity)
            {
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
