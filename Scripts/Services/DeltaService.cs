using HarmonyLib;
using Newtonsoft.Json;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.RecipeMap.Path;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Services
{
    public static class DeltaService
    {
        private static SerializedRecipeMark.Type? LastRecipeMarkType;

        public static void RecordRecipeMarkInfo()
        {
            //For some reason a lot of these recipe mark methods get called when potions are not even being brewed
            //Check to make sure we are actually brewing here
            if (!Managers.Potion?.potionCraftPanel?.IsPotionBrewingStarted() ?? false) return;
            RecordRecipeMarkInfo(Managers.Potion.recipeMarks.GetMarksList().LastOrDefault());
        }

        public static void SetupInitialInfo()
        {
            SetupCurrentPotionState();
        }

        private static string LastRecipeMarkStringValue;
        public static void RecordRecipeMarkInfo(SerializedRecipeMark recipeMark)
        {
            if (recipeMark.type == SerializedRecipeMark.Type.PotionBase) return;

            //If we are switching to a new recipe mark then record the data from the previous recipe mark (always do this in the case of ingredient marks)
            if (StaticStorage.CurrentRecipeMarkInfo != null && 
                (LastRecipeMarkType != recipeMark?.type 
                 || recipeMark?.type == SerializedRecipeMark.Type.Ingredient
                 || (recipeMark?.type == SerializedRecipeMark.Type.Salt && recipeMark?.stringValue != LastRecipeMarkStringValue)))
            {
                StaticStorage.CurrentPotionRecipeMarkInfos[StaticStorage.CurrentRecipeMarkInfo.Index] = StaticStorage.CurrentRecipeMarkInfo;
                SetupCurrentPotionState();
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
                    RecordCurrentPathHintInfo();
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
                    RecordCurrentPathHintInfo();
                    RecordUsedComponentInfo(recipeMark.stringValue);

                    //Void salt requires a special case when it fully deletes a fixed path from the end.
                    if (recipeMark.stringValue == "Void Salt")
                    {
                        HandleDeletedFixedHintFromVoidSalt();
                    }
                    break;
                case SerializedRecipeMark.Type.Ingredient:
                    RecordNewIngredientInfo(recipeMark.stringValue);
                    break;
            }

            //DebugPrintDeltas();
        }

        private static void SetupCurrentPotionState()
        {
            //Clear out any old data in here
            StaticStorage.CurrentPotionState.TryGetValue(DeltaProperty.PathAddedFixedHints, out BaseDelta oldAddedFixedHint);
            StaticStorage.CurrentPotionState = new();
            if (oldAddedFixedHint != null) StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints] = oldAddedFixedHint;

            //Setup list data
            StaticStorage.CurrentPotionState[DeltaProperty.UsedComponents] = GetUsedComponentsProperty();
            StaticStorage.CurrentPotionState[DeltaProperty.FixedHints] = GetFixedHintsProperty();

            //record all base properties (some of these rely on potion state data from above so do these last)
            var baseProperties = new List<DeltaProperty> 
            {
                DeltaProperty.Position,
                DeltaProperty.Rotation,
                DeltaProperty.Health,
                DeltaProperty.Effects,
                DeltaProperty.PathDeletedSegments,
                DeltaProperty.PathRotation,
                DeltaProperty.PathFixedHintsCount,
                DeltaProperty.PathAddedFixedHints
            };

            baseProperties.ForEach(p => StaticStorage.CurrentPotionState[p] = GetBaseProperty(p));
        }
        
        /// <summary>
        /// Position, rotation, health
        /// </summary>
        private static void RecordPositionInfo()
        {
            //record position
            RecordProperty(DeltaProperty.Position);

            //record rotation
            RecordProperty(DeltaProperty.Rotation);

            //record health
            RecordCurrentHealthInfo();
        }

        private static void RecordMoveAlongPathInfo()
        {
            //record deleted segments
            RecordProperty(DeltaProperty.PathDeletedSegments);

            //update the potion state to have the correct number of fixed hints for proper index comparisons
            var fixedHintsList = (ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints];
            if (Managers.RecipeMap.path.fixedPathHints.Count == fixedHintsList.AddDeltas.Count - 1)
            {
                fixedHintsList.AddDeltas = fixedHintsList.AddDeltas.Skip(1).ToList();
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathFixedHintsCount));
            }
        }

        private static void RecordEffectInfo()
        {
            //record effects
            RecordProperty(DeltaProperty.Effects);
        }

        private static void RecordCurrentHealthInfo()
        {
            //record health
            RecordProperty(DeltaProperty.Health);
        }

        private static void RecordNewIngredientInfo(string ingredientName)
        {
            //Record new fixed hint
            RecordNewListAddInfo(DeltaProperty.FixedHints, Managers.RecipeMap.path.fixedPathHints);
            //Update the current potion state added fixed hints property
            ((ModifyDelta<int>)StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints]).NewValue++;
            //Record the change to the path added fixed hints property
            StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathAddedFixedHints));
            //Record the change to the fixed hints count property (this isn't really nessesary since the list add contains the index but its better to be consistent)
            StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathFixedHintsCount));

            //Record new used component
            RecordUsedComponentInfo(ingredientName);
        }


        private static void RecordCurrentPathHintInfo()
        {
            var sourceFixedHint = Managers.RecipeMap.path.fixedPathHints.LastOrDefault();
            if (sourceFixedHint == null) return;

            RecordListObjectInfo(DeltaProperty.FixedHints, sourceFixedHint, Managers.RecipeMap.path.fixedPathHints.IndexOf(sourceFixedHint));
        }

        private static void HandleDeletedFixedHintFromVoidSalt()
        {
            var fixedHintsList = (ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints];
            if (Managers.RecipeMap.path.fixedPathHints.Count != fixedHintsList.AddDeltas.Count - 1) return;
            //Update the current potion state
            fixedHintsList.AddDeltas = fixedHintsList.AddDeltas.Take(Managers.RecipeMap.path.fixedPathHints.Count).ToList();
            ((ModifyDelta<int>)StaticStorage.CurrentPotionState[DeltaProperty.PathAddedFixedHints]).NewValue--;

            //In order to ensure proper naviation of fixed hints record an addedFixedHints change
            StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(GetBaseProperty(DeltaProperty.PathAddedFixedHints));
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

        private static void RecordNewListAddInfo<T>(DeltaProperty property, List<T> sourceList)
        {
            var lastItem = sourceList.LastOrDefault();
            if (lastItem == null)
            {
                throw new InvalidOperationException($"RecordNewListAddInfo should only be called when a new item is added. No {property} found!");
            }

            RecordListObjectInfo(property, lastItem, sourceList.Count);
        }

        private static void RecordListObjectInfo<T>(DeltaProperty property, T sourceAdded, int index)
        {
            var fixedHintsList = StaticStorage.CurrentPotionState[property] as ListDelta;
            var newFixedHintDelta = GetBaseAddDelta(sourceAdded, index);
            var lastAddedFixedHintDelta = fixedHintsList.AddDeltas.Count > index ? fixedHintsList.AddDeltas[index] : null;

            //Check to see if anything has actually changed
            if (lastAddedFixedHintDelta != null && lastAddedFixedHintDelta == newFixedHintDelta)
            {
                return;
            }

            //Update recipe mark deltas
            if (lastAddedFixedHintDelta == null)
            {
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(newFixedHintDelta);
                return;
            }

            //If this is an update to an existing item then simplify changed properties
            var changedProperties = newFixedHintDelta.Deltas.Where(newProperty =>
            {
                var correspondingProperty = lastAddedFixedHintDelta.Deltas.FirstOrDefault(d => d.Property == newProperty.Property);
                if (correspondingProperty == null) return true;
                return newProperty != correspondingProperty;
            });
            var simplifiedDelta = new ListAddDelta
            {
                Property = newFixedHintDelta.Property,
                Index = newFixedHintDelta.Index,
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
                AddDeltas = Managers.Potion.usedComponents.Select(GetBaseAddDelta).Where(d => d != null).ToList(),
            };
        }
        private static ListDelta GetFixedHintsProperty()
        {
            return new ListDelta
            {
                Property = DeltaProperty.FixedHints,
                AddDeltas = Managers.RecipeMap.path.fixedPathHints.Select(GetBaseAddDelta).Where(d => d != null).ToList(),
            };
        }

        private static ListAddDelta GetBaseAddDelta(object property, int index)
        {
            if (property == null) return null;

            var propertyEnum = property switch
            {
                PotionUsedComponent => 
                    DeltaProperty.UsedComponents,
                FixedHint => 
                    DeltaProperty.FixedHints,
                _ => throw new ArgumentException($"Object of type: {property.GetType()} is not a valid list add object!"),
            };

            return new ListAddDelta
            {
                Index = index,
                Property = propertyEnum,
                Deltas = GetAllBaseProperties(property)
            };
        }

        private static List<ModifyDelta> GetAllBaseProperties(object property)
        {
            if (property == null) return new List<ModifyDelta>();

            var objectProperties = property switch
            {
                PotionUsedComponent => 
                    new List<DeltaProperty> { DeltaProperty.UsedComponent_Ammount },
                FixedHint => 
                    new List<DeltaProperty> { DeltaProperty.FixedHint_EvenlySpacedPoints },
                _ => throw new ArgumentException($"Object of type: {property.GetType()} is not a valid list add object!"),
            };
            return objectProperties.Select(p => GetListObjectProperty(p, property)).ToList();
        }

        private static ModifyDelta GetListObjectProperty(DeltaProperty property, object obj)
        {
            return property switch
            {
                DeltaProperty.UsedComponent_Ammount => new ModifyDelta<int>
                {
                    Property = DeltaProperty.UsedComponent_Ammount,
                    NewValue = ((PotionUsedComponent)obj).amount
                },
                DeltaProperty.FixedHint_EvenlySpacedPoints => new ModifyDelta<List<Vector3>>
                {
                    Property = DeltaProperty.FixedHint_EvenlySpacedPoints,
                    NewValue = ((FixedHint)obj).evenlySpacedPointsFixedGraphics.points.ToList()
                },
                _ => throw new ArgumentException($"Property: {property} is not a list object property!"),
            };
        }

        private static ModifyDelta GetBaseProperty(DeltaProperty property)
        {
            return property switch
            {
                DeltaProperty.Position => new ModifyDelta<Vector2>
                {
                    Property = DeltaProperty.Position,
                    NewValue = Managers.RecipeMap.recipeMapObject.indicatorContainer.localPosition
                },
                DeltaProperty.Rotation => new ModifyDelta<float>
                {
                    Property = DeltaProperty.Rotation,
                    NewValue = Managers.RecipeMap.indicatorRotation.Value
                },
                DeltaProperty.Health => new ModifyDelta<float>
                {
                    Property = DeltaProperty.Health,
                    NewValue = (float)Traverse.Create(Managers.RecipeMap.indicator).Field("health").GetValue()
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
                                : 0
                },
                DeltaProperty.PathFixedHintsCount => new ModifyDelta<int>
                {
                    Property = DeltaProperty.PathFixedHintsCount,
                    NewValue = ((ListDelta)StaticStorage.CurrentPotionState[DeltaProperty.FixedHints]).AddDeltas.Count
                },
                DeltaProperty.PathRotation => new ModifyDelta<Quaternion>
                {
                    Property = DeltaProperty.PathRotation,
                    NewValue = Traverse.Create(Managers.RecipeMap.path.fixedPathHints.FirstOrDefault())
                                       ?.Field("mortarTransform")
                                       ?.GetValue<Transform>()?.rotation 
                                       ?? Quaternion.identity
                },
                _ => throw new ArgumentException($"Property: {property} is not a base property!"),
            };
        }

        private static void DebugPrintDeltas()
        {
            Plugin.PluginLogger.LogMessage(JsonConvert.SerializeObject(StaticStorage.CurrentRecipeMarkInfo, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore }));
        }
    }
}
