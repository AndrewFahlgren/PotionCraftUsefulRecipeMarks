﻿using HarmonyLib;
using Newtonsoft.Json;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.RecipeMap.Path;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static PotionCraft.SaveLoadSystem.ProgressState;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public static class DeltaRecordingService
    {
        public const string VoidSaltName = "Void Salt";
        private static SerializedRecipeMark.Type? LastRecipeMarkType;

        public static void RecordRecipeMarkInfo()
        {
            //For some reason a lot of these recipe mark methods get called when potions are not even being brewed
            //Check to make sure we are actually brewing here
            if (!Managers.Potion?.potionCraftPanel?.IsPotionBrewingStarted() ?? false) return;
            RecordRecipeMarkInfo(Managers.Potion.recipeMarks.GetMarksList().LastOrDefault());
        }

        public static void SetupInitialInfoForRecipe(RecipeBookRightPageContent rightPageContent)
        {
            var recipeIndex = Managers.Potion.recipeBook.currentPageIndex;
            if (!StaticStorage.RecipeMarkInfos.TryGetValue(recipeIndex, out var recipeMarkInfos))
            {
                //Our custom recipe breaks this initial index set so fix that here
                Managers.Potion.recipeBook.currentPotionRecipeIndex = recipeIndex;

                SetupInitialInfo();
                //Add the old serialized path for this pre mod recipe
                StaticStorage.CurrentPotionRecipeMarkInfos[0].Deltas.Add(new ModifyDelta<SerializedPath>
                {
                    Property = DeltaProperty.OldSerializedPath,
                    NewValue = rightPageContent.currentPotion.potionFromPanel.serializedPath
                });
                //Mark each pre mod fixed hint as an old fixed hint
                var currentPotionStateFixedHintDeltas = ((ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints]).AddDeltas;
                var recipeMarkDeltaFixedHintDeltas = ((ListDelta)StaticStorage.CurrentPotionRecipeMarkInfos[0].Deltas.First(d => d.Property == DeltaProperty.FixedHints)).AddDeltas;
                currentPotionStateFixedHintDeltas.Concat(recipeMarkDeltaFixedHintDeltas).ToList().ForEach(fixedHintDelta =>
                {
                    fixedHintDelta.Deltas.Add(new ModifyDelta<bool>
                    {
                        Property = DeltaProperty.IsOldFixedHint,
                        NewValue = true
                    });
                });
                return;
            }

            var recipeMarkIndex = StaticStorage.SelectedRecipeMarkIndex;
            StaticStorage.CurrentPotionRecipeMarkInfos = recipeMarkInfos.Count > recipeMarkIndex 
                                                            ? recipeMarkInfos.Take(recipeMarkIndex + 1).ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                                                            : recipeMarkInfos;
            var currentRecipeMarks = Managers.Potion.recipeMarks.GetMarksList();
            while (currentRecipeMarks.Count > recipeMarkIndex + 1)
            {
                currentRecipeMarks.RemoveAt(currentRecipeMarks.Count - 1);
            }

            if (StaticStorage.SelectedRecipePotionState != null)
            {
                StaticStorage.CurrentPotionState = StaticStorage.SelectedRecipePotionState;
            }
        }


        public static bool CommitRecipeMarkInfoForSavedRecipe()
        {
            var recipeIndex = Managers.Potion.recipeBook.savedRecipes.IndexOf(null);

            //This is an error case handled in game. Its best to just return here and let the game deal with the fallout
            if (recipeIndex == -1) 
            {
                return true;
            }
            //This will commit the last 
            RecordRecipeMarkInfo(null);
            StaticStorage.RecipeMarkInfos[recipeIndex] = StaticStorage.CurrentPotionRecipeMarkInfos;

            return true;
        }

        public static void SetupInitialInfo()
        {
            SaveLoadService.SetupListeners(); //TODO move to a more appropriate spot
            StaticStorage.CurrentPotionState = new();
            SetupCurrentPotionState(false);
            StaticStorage.CurrentPotionRecipeMarkInfos = new Dictionary<int, RecipeMarkInfo>
            {
                [0] = new RecipeMarkInfo
                {
                    Index = 0,
                    Deltas = StaticStorage.CurrentPotionState.Values.ToList()
                }
            };
            StaticStorage.CurrentRecipeMarkInfo = null;
        }

        private static string LastRecipeMarkStringValue;
        public static void RecordRecipeMarkInfo(SerializedRecipeMark recipeMark)
        {
            if (recipeMark?.type == SerializedRecipeMark.Type.PotionBase) return;

            //If we are switching to a new recipe mark then record the data from the previous recipe mark (always do this in the case of ingredient marks)
            if (StaticStorage.CurrentRecipeMarkInfo != null && 
                (LastRecipeMarkType != recipeMark?.type 
                 || recipeMark?.type == SerializedRecipeMark.Type.Ingredient
                 || (recipeMark?.type == SerializedRecipeMark.Type.Salt && recipeMark?.stringValue != LastRecipeMarkStringValue)))
            {
                //We avoid comparisions for position info to save time
                //Check to see if anything has changed here
                CommitPositionInfo();
                StaticStorage.CurrentPotionRecipeMarkInfos[StaticStorage.CurrentRecipeMarkInfo.Index] = StaticStorage.CurrentRecipeMarkInfo;
                //Update the current potion state the the last recorded state
                if (TemporaryCurrentPotionState != null) StaticStorage.CurrentPotionState = TemporaryCurrentPotionState;
            }

            //This indicates an end of recipe. No need to record further.
            if (recipeMark == null) return;

            LastRecipeMarkType = recipeMark.type;
            LastRecipeMarkStringValue = recipeMark.stringValue;

            //Throw away any data we have already recorded. We only need the latest properties for each recipe mark.
            var index = Managers.Potion.recipeMarks.GetMarksList().IndexOf(recipeMark);
            StaticStorage.CurrentRecipeMarkInfo = new RecipeMarkInfo { Index = index };

            switch (recipeMark.type)
            {
                case SerializedRecipeMark.Type.Spoon:
                    RecordPositionInfo();
                    RecordMoveAlongPathInfo();
                    break;
                case SerializedRecipeMark.Type.Ladle:
                    RecordPositionInfo();
                    break;
                case SerializedRecipeMark.Type.Bellows: //TODO does this get recorded if you start to heat on an effect and then don't?
                    RecordEffectInfo();
                    //TODO is it possible to get this mark while moving in a vortex? If so also record position info.
                    break;
                case SerializedRecipeMark.Type.Salt:
                    RecordPositionInfo();
                    RecordUsedComponentInfo(recipeMark.stringValue);

                    //Void salt requires a special case when it fully deletes a fixed path from the end.
                    if (recipeMark.stringValue == VoidSaltName)
                    {
                        HandleDeletedFixedHintFromVoidSalt();
                        RecordEndPathHintInfo();
                    }
                    else
                    {
                        RecordConnectionPointInfo();
                    }
                    break;
                case SerializedRecipeMark.Type.Ingredient:
                    RecordNewIngredientInfo(recipeMark.stringValue);
                    break;
            }
            SetupCurrentPotionState(true);
            //DebugPrintDeltas();
        }

        private static Dictionary<DeltaProperty, BaseDelta> TemporaryCurrentPotionState;
        private static void SetupCurrentPotionState(bool useTemporaryStateDict)
        {
            //Clear out any old data in here
            StaticStorage.CurrentPotionState.TryGetValue(DeltaProperty.PathAddedFixedHints, out BaseDelta oldAddedFixedHint);

            if (useTemporaryStateDict) TemporaryCurrentPotionState = new();
            else StaticStorage.CurrentPotionState = new();

            var currentPotionState = useTemporaryStateDict ? TemporaryCurrentPotionState : StaticStorage.CurrentPotionState;
            if (oldAddedFixedHint != null) currentPotionState[DeltaProperty.PathAddedFixedHints] = oldAddedFixedHint;
            else currentPotionState[DeltaProperty.PathAddedFixedHints] = GetBaseProperty(DeltaProperty.PathAddedFixedHints);

            //Setup list data
            currentPotionState[DeltaProperty.UsedComponents] = GetUsedComponentsProperty();
            currentPotionState[DeltaProperty.FixedHints] = GetFixedHintsProperty();

            //record all base properties (some of these rely on potion state data from above so do these last)
            var baseProperties = new List<DeltaProperty> 
            {
                DeltaProperty.IndicatorPosition,
                DeltaProperty.PathPosition,
                DeltaProperty.IndicatorTargetPosition,
                DeltaProperty.FollowButtonTargetPosition,
                DeltaProperty.Rotation,
                DeltaProperty.Health,
                DeltaProperty.Effects,
                DeltaProperty.PathDeletedSegments,
                DeltaProperty.PathFixedHintsCount
            };

            baseProperties.ForEach(p => currentPotionState[p] = GetBaseProperty(p));
        }

        private static void RecordPositionInfo()
        {
            indicatorPosition = ((ModifyDelta<Vector2>)GetBaseProperty(DeltaProperty.IndicatorPosition)).NewValue;
            pathPosition = ((ModifyDelta<Vector2>)GetBaseProperty(DeltaProperty.PathPosition)).NewValue;
            indicatorTargetPosition = ((ModifyDelta<Vector2>)GetBaseProperty(DeltaProperty.IndicatorTargetPosition)).NewValue;
            followButtonTargetPosition = ((ModifyDelta<Vector2>)GetBaseProperty(DeltaProperty.FollowButtonTargetPosition)).NewValue;
            rotation = ((ModifyDelta<float>)GetBaseProperty(DeltaProperty.Rotation)).NewValue;
            health = ((ModifyDelta<float>)GetBaseProperty(DeltaProperty.Health)).NewValue;
        }

        private static Vector2 indicatorPosition;
        private static Vector2 pathPosition;
        private static Vector2 indicatorTargetPosition;
        private static Vector2 followButtonTargetPosition;
        private static float rotation;
        private static float health;

        private static void CommitPositionInfo()
        {
            CommitProperty(DeltaProperty.IndicatorPosition, indicatorPosition);
            CommitProperty(DeltaProperty.PathPosition, pathPosition);
            CommitProperty(DeltaProperty.IndicatorTargetPosition, indicatorTargetPosition);
            CommitProperty(DeltaProperty.FollowButtonTargetPosition, followButtonTargetPosition);
            CommitProperty(DeltaProperty.Rotation, rotation);
            CommitProperty(DeltaProperty.Health, health);
        }

        private static void CommitProperty<T>(DeltaProperty property, T value)
        {
            var curProperty = new ModifyDelta<T> { Property = property, NewValue = value };
            if (StaticStorage.CurrentPotionState[property] != curProperty)
            {
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(curProperty);
            }
        }

        private static void RecordMoveAlongPathInfo()
        {
            //record deleted segments
            RecordProperty(DeltaProperty.PathDeletedSegments);

            var fixedHintsList = (ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints];
           // var firstPathHint = fixedHintsList.AddDeltas.FirstOrDefault();
            if (Managers.RecipeMap.path.fixedPathHints.Count < fixedHintsList.AddDeltas.Count)
            {
                //var countDifference = fixedHintsList.AddDeltas.Count - Managers.RecipeMap.path.fixedPathHints.Count;
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathFixedHintsCount));
                //firstPathHint = fixedHintsList.AddDeltas.Skip(countDifference).FirstOrDefault();
            }

            //if (firstPathHint == null) return;
            var offset = fixedHintsList.AddDeltas.Count - Managers.RecipeMap.path.fixedPathHints.Count;

            //Record fixed hint info for the current hint we are deleting
            RecordListObjectInfo(DeltaProperty.FixedHints, Managers.RecipeMap.path.fixedPathHints.FirstOrDefault(), 0, offset);

            //Record connection point info for all later hints since moving can rotate
            RecordConnectionPointInfo(offset);
        }

        private static void RecordConnectionPointInfo(int indexOffset = 0)
        {
            for (var i = 1; i < Managers.RecipeMap.path.fixedPathHints.Count; i++)
            {
                //Plugin.PluginLogger.LogMessage($"RecordConnectionPointInfo: i={i}");
                var curFixedHint = Managers.RecipeMap.path.fixedPathHints[i];
                RecordListObjectInfo(DeltaProperty.FixedHints, curFixedHint, i, i + indexOffset, new List<DeltaProperty> { DeltaProperty.FixedHint_ConnectionPoint }); //TODO I think this is doing unexpected things. Put debug statements here to make sure all of the indexing and stuff is right
            }
            //Plugin.PluginLogger.LogMessage($"RecordConnectionPointInfo: end");
        }

        private static void RecordEffectInfo()
        {
            //record effects
            RecordProperty(DeltaProperty.Effects);
        }

        private static void RecordNewIngredientInfo(string ingredientName)
        {
            //Update the current potion state added fixed hints property
            var pathAddedFixedHints = (ModifyDelta<int>)StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints];
            pathAddedFixedHints.NewValue++;
            //Record new fixed hint
            RecordNewListAddInfo(DeltaProperty.FixedHints, Managers.RecipeMap.path.fixedPathHints);
            //Record the change to the path added fixed hints property
            StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathAddedFixedHints));
            //Record the change to the fixed hints count property (this isn't really nessesary since the list add contains the index but its better to be consistent)
            StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathFixedHintsCount));

            //Record new used component
            RecordUsedComponentInfo(ingredientName);
        }

        private static void RecordEndPathHintInfo()
        {
            RecordNewListAddInfo(DeltaProperty.FixedHints, Managers.RecipeMap.path.fixedPathHints, Managers.RecipeMap.path.fixedPathHints.Count - 1);
        }

        private static void HandleDeletedFixedHintFromVoidSalt()
        {
            var fixedHintsList = (ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints];
            if (Managers.RecipeMap.path.fixedPathHints.Count == fixedHintsList.AddDeltas.Count - 1)
            {
                //Update the current potion state
                fixedHintsList.AddDeltas = fixedHintsList.AddDeltas.Take(Managers.RecipeMap.path.fixedPathHints.Count).ToList();
                ((ModifyDelta<int>)StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints]).NewValue--;

                //In order to ensure proper naviation of fixed hints record an addedFixedHints change
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathAddedFixedHints));
            }
        }

        private static void RecordUsedComponentInfo(string usedComponentName)
        {
            var usedComponent = Managers.Potion.usedComponents.FirstOrDefault(c => c.componentObject.name == usedComponentName);

            if (usedComponent == null)
            {
                throw new ArgumentException($"Cannot record new used component info for {usedComponentName}. Used component not found in usedComponents list");
            }

            RecordListObjectInfo(DeltaProperty.UsedComponents, usedComponent, Managers.Potion.usedComponents.IndexOf(usedComponent));
        }

        private static void RecordNewListAddInfo<T>(DeltaProperty property, List<T> sourceList, int indexOverride = -1)
        {
            var lastItem = sourceList.LastOrDefault();
            if (lastItem == null)
            {
                throw new InvalidOperationException($"RecordNewListAddInfo should only be called when a new item is added. No {property} found!");
            }

            RecordListObjectInfo(property, lastItem, indexOverride == -1 ? sourceList.Count - 1 : indexOverride);
        }

        private static void RecordListObjectInfo<T>(DeltaProperty property, T sourceAdded, int index, int addDeltaIndex = -1, List<DeltaProperty> propertiesToRecord = null)
        {
            var potionStateList = StaticStorage.CurrentPotionState[property] as ListDelta;
            var newAddDelta = GetBaseAddDelta(sourceAdded, index, propertiesToRecord);
            if (addDeltaIndex == -1)
            {
                addDeltaIndex = index; //TODO this just seems wrong. Lets think about what happens later on here
            }
            var lastAddedDelta = potionStateList.AddDeltas.Count > addDeltaIndex ? potionStateList.AddDeltas[addDeltaIndex] : null;

            //Check to see if anything has actually changed
            if (lastAddedDelta != null && lastAddedDelta == newAddDelta)
            {
                return;
            }

            //Update recipe mark deltas
            if (lastAddedDelta == null)
            {
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(newAddDelta);
                return;
            }

            //If this is an update to an existing item then simplify changed properties
            var changedProperties = newAddDelta.Deltas.Where(newProperty =>
            {
                var correspondingProperty = lastAddedDelta.Deltas.FirstOrDefault(d => d.Property == newProperty.Property);
                if (correspondingProperty == null) return true;
                return newProperty != correspondingProperty;
            });
            var simplifiedDelta = new ListAddDelta
            {
                Property = newAddDelta.Property,
                Index = newAddDelta.Index,
                Deltas = changedProperties.ToList()
            };
            StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(simplifiedDelta);
        }

        private static void RecordProperty(DeltaProperty property)
        {
            var curProperty = GetBaseProperty(property);
            if (StaticStorage.CurrentPotionState[property] != curProperty)
            {
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(curProperty);
            }
        }

        private static ListDelta GetUsedComponentsProperty()
        {
            return new ListDelta
            {
                Property = DeltaProperty.UsedComponents,
                AddDeltas = Managers.Potion.usedComponents.Select((u, i) => GetBaseAddDelta(u, i)).Where(d => d != null).ToList(),
            };
        }
        private static ListDelta GetFixedHintsProperty()
        {
            return new ListDelta
            {
                Property = DeltaProperty.FixedHints,
                AddDeltas = Managers.RecipeMap.path.fixedPathHints.Select((h, i) => GetBaseAddDelta(h, i)).Where(d => d != null).ToList(),
            };
        }

        private static ListAddDelta GetBaseAddDelta(object obj, int index, List<DeltaProperty> propertiesToRecord = null)
        {
            if (obj == null) return null;

            var propertyEnum = obj switch
            {
                PotionUsedComponent => 
                    DeltaProperty.UsedComponents,
                FixedHint => 
                    DeltaProperty.FixedHints,
                _ => throw new ArgumentException($"Object of type: {obj.GetType()} is not a valid list add object!"),
            };

            if (propertyEnum == DeltaProperty.FixedHints)
            {
                var totalAdded = ((ModifyDelta<int>)StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints]).NewValue;
                var indexOffset = totalAdded - Managers.RecipeMap.path.fixedPathHints.Count;
                index += indexOffset;
            }

            return new ListAddDelta
            {
                Index = index,
                Property = propertyEnum,
                Deltas = propertiesToRecord == null ? GetAllBaseProperties(obj) : propertiesToRecord.Select(p => GetListObjectProperty(p, obj)).ToList()
            };
        }

        private static List<ModifyDelta> GetAllBaseProperties(object obj)
        {
            if (obj == null) return new List<ModifyDelta>();

            var objectProperties = obj switch
            {
                PotionUsedComponent => 
                    new List<DeltaProperty> { DeltaProperty.UsedComponent_Ammount },
                FixedHint => 
                    new List<DeltaProperty> 
                    { 
                        DeltaProperty.FixedHint_Length, 
                        DeltaProperty.FixedHint_ConnectionPoint
                    },
                _ => throw new ArgumentException($"Object of type: {obj.GetType()} is not a valid list add object!"),
            };
            return objectProperties.Select(p => GetListObjectProperty(p, obj)).ToList();
        }

        private static ModifyDelta GetListObjectProperty(DeltaProperty property, object obj)
        {
           // Plugin.PluginLogger.LogMessage($"GetListObjectProperty: property={property}");
            return property switch
            {
                DeltaProperty.UsedComponent_Ammount => new ModifyDelta<int>
                {
                    Property = DeltaProperty.UsedComponent_Ammount,
                    NewValue = ((PotionUsedComponent)obj).amount
                },
                DeltaProperty.FixedHint_ConnectionPoint => new ModifyDelta<Vector2> //TODO switch to tuples here to save storage space
                {
                    Property = DeltaProperty.FixedHint_ConnectionPoint,
                    NewValue = GetConnectionPointForFixedHint((FixedHint)obj)
                },
                DeltaProperty.FixedHint_Length => new ModifyDelta<float>
                {
                    Property = DeltaProperty.FixedHint_Length,
                    NewValue = GetFixedHintPathLength((FixedHint)obj)
                },
                _ => throw new ArgumentException($"Property: {property} is not a list object property!"),
            };
        }

        private static Vector2 GetConnectionPointForFixedHint(FixedHint obj)
        {
            var index = Managers.RecipeMap.path.fixedPathHints.IndexOf(obj);
            if (index == -1)
            {
                return Vector2.zero;
            }
            //Check for a previously stored value. We only want to store while this isn't the current first fixed hint.
            if (index == 0)
            {
                var deltaChange = GetFixedHintDeltaForFixedHint(obj)?.Deltas?.FirstOrDefault(d => d.Property == DeltaProperty.FixedHint_ConnectionPoint) as ModifyDelta<Vector2>;
                if (deltaChange != null)
                {
                    return deltaChange.NewValue;
                }

                //If this is the first index and we don't have a previously saved delta change then this is the entry point and we can save the connection point from the first dot
                return Managers.RecipeMap.path.fixedPathHints.First().evenlySpacedPointsFixedGraphics.points.First();
            }

            //If this isn't the first fixed hint we can always check the last hint of the previous fixed hint
            return Managers.RecipeMap.path.fixedPathHints[index - 1].evenlySpacedPointsFixedGraphics.points.Last();
        }

        private static float GetPreviouslyRecordedLengthForFixedHint(FixedHint obj)
        {
            if (StaticStorage.CurrentPotionState.ContainsKey(DeltaProperty.PathAddedFixedHints) 
                && StaticStorage.CurrentPotionState.ContainsKey(DeltaProperty.FixedHints))
            {
                var deltaChange = GetFixedHintDeltaForFixedHint(obj)?.Deltas?.FirstOrDefault(d => d.Property == DeltaProperty.FixedHint_Length) as ModifyDelta<float>;
                if (deltaChange != null)
                {
                    return deltaChange.NewValue;
                }
            }

            return GetFixedHintPathLength(obj);
        }

        private static ListAddDelta GetFixedHintDeltaForFixedHint(FixedHint obj)
        {
            if (!StaticStorage.CurrentPotionState.ContainsKey(DeltaProperty.PathAddedFixedHints)
                || !StaticStorage.CurrentPotionState.ContainsKey(DeltaProperty.FixedHints))
            {
                return null;
            }
            var index = Managers.RecipeMap.path.fixedPathHints.IndexOf(obj);
            var totalAdded = ((ModifyDelta<int>)StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints]).NewValue;
            var indexOffset = totalAdded - Managers.RecipeMap.path.fixedPathHints.Count;
            index += indexOffset;
            return ((ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints]).AddDeltas.FirstOrDefault(d => d.Index == index);
        }

        private static float GetFixedHintPathLength(FixedHint hint)
        {
            var pathMapItem = Managers.RecipeMap.path;
            return hint.GetPathLength() - pathMapItem.segmentLengthToDeleteFromEndGraphics;
        }

        private static SerializedRecipeMark GetIngredientMarkForFixedHint(FixedHint obj)
        {
            var fixedHintIndex = Managers.RecipeMap.path.fixedPathHints.IndexOf(obj);
            var lastIndex = Managers.RecipeMap.path.fixedPathHints.Count - 1;
            var ingredientMarksToSkip = lastIndex - fixedHintIndex;
            var ingredientsSkipped = 0;
            var marksList = Managers.Potion.recipeMarks.GetMarksList();
            for (var i = marksList.Count - 1; i > 0; i--)
            {
                var curRecipeMark = marksList[i];
                if (curRecipeMark.type != SerializedRecipeMark.Type.Ingredient) continue;
                if (ingredientMarksToSkip > ingredientsSkipped)
                {
                    ingredientsSkipped++;
                    continue;
                }
                return curRecipeMark;
            }
            throw new InvalidOperationException("No corresponding recipe mark can be found for this fixed hint!");
        }

        private static ModifyDelta GetBaseProperty(DeltaProperty property)
        {
            return property switch
            {
                DeltaProperty.IndicatorPosition => new ModifyDelta<Vector2>
                {
                    Property = DeltaProperty.IndicatorPosition,
                    NewValue = Managers.RecipeMap.indicator.thisTransform.localPosition
                },
                DeltaProperty.PathPosition => new ModifyDelta<Vector2>
                {
                    Property = DeltaProperty.PathPosition,
                    NewValue = Managers.RecipeMap.path.transform.localPosition
                },
                DeltaProperty.IndicatorTargetPosition => new ModifyDelta<Vector2>
                {
                    Property = DeltaProperty.IndicatorTargetPosition,
                    NewValue = Managers.RecipeMap.indicator.targetPosition
                },
                DeltaProperty.FollowButtonTargetPosition => new ModifyDelta<Vector2>
                {
                    Property = DeltaProperty.FollowButtonTargetPosition,
                    NewValue = Managers.RecipeMap.recipeMapObject.followButtonTargetObject.localPosition
                },
                DeltaProperty.Rotation => new ModifyDelta<float>
                {
                    Property = DeltaProperty.Rotation,
                    NewValue = Managers.RecipeMap.indicatorRotation.Value
                },
                DeltaProperty.Health => new ModifyDelta<float>
                {
                    Property = DeltaProperty.Health,
                    NewValue = (float)Traverse.Create(Managers.RecipeMap.indicator).Field("visualHealth").GetValue()
                },
                DeltaProperty.Effects => new ModifyDelta<List<string>>
                {
                    Property = DeltaProperty.Effects,
                    NewValue = Managers.Potion.collectedPotionEffects.Where(e => e != null).Select(e => e.name).ToList()
                },
                DeltaProperty.PathDeletedSegments => new ModifyDelta<float>
                {
                    Property = DeltaProperty.PathDeletedSegments,
                    NewValue = Managers.RecipeMap.path.deletedGraphicsSegments
                },
                DeltaProperty.PathAddedFixedHints => new ModifyDelta<int>
                {
                    Property = DeltaProperty.PathAddedFixedHints,
                    NewValue = StaticStorage.CurrentPotionState.TryGetValue(DeltaProperty.PathAddedFixedHints, out BaseDelta addedFixedHints)
                                ? ((ModifyDelta<int>)addedFixedHints).NewValue
                                : Managers.RecipeMap.path.fixedPathHints.Count
                },
                DeltaProperty.PathFixedHintsCount => new ModifyDelta<int>
                {
                    Property = DeltaProperty.PathFixedHintsCount,
                    NewValue = Managers.RecipeMap.path.fixedPathHints.Count
                },
                _ => throw new ArgumentException($"Property: {property} is not a base property!"),
            };
        }

        public static void DeleteMarkInfoForRecipe(Potion recipe)
        {
            var recipeIndex = Managers.Potion.recipeBook.savedRecipes.IndexOf(recipe);
            if (!StaticStorage.RecipeMarkInfos.ContainsKey(recipeIndex)) return;
            StaticStorage.RecipeMarkInfos.Remove(recipeIndex);
        }

        private static void DebugPrintDeltas()
        {
            Plugin.PluginLogger.LogMessage(JsonConvert.SerializeObject(StaticStorage.CurrentRecipeMarkInfo, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        }
    }
}
