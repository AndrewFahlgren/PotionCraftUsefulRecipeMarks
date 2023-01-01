using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.RecipeMap.Path;
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
    public static class DeltaService
    {
        private static SerializedRecipeMark.Type? LastRecipeMarkType;
        private static string LastRecipeMarkStringValue;
        public static void RecordRecipeMarkInfo(SerializedRecipeMark recipeMark)
        {
            //If we are switching to a new recipe mark then record the data from the previous recipe mark (always do this in the case of ingredient marks)
            if (StaticStorage.CurrentRecipeMarkInfo != null && 
                (LastRecipeMarkType != recipeMark?.type 
                 || recipeMark?.type == SerializedRecipeMark.Type.Ingredient
                 || (recipeMark?.type == SerializedRecipeMark.Type.Salt && recipeMark?.stringValue != LastRecipeMarkStringValue)))
            {
                StaticStorage.CurrentPotionRecipeMarkInfos[StaticStorage.CurrentRecipeMarkInfo.Index] = StaticStorage.CurrentRecipeMarkInfo;
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
                case SerializedRecipeMark.Type.PotionBase:
                    SetupCurrentPotionState();
                    break;
                case SerializedRecipeMark.Type.Spoon:
                    RecordPositionInfo();
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
                    break;
                case SerializedRecipeMark.Type.Ingredient:
                    RecordNewIngredientInfo(recipeMark.stringValue);
                    break;
            }
        }

        private static void SetupCurrentPotionState()
        {
            //record everything into StaticStorage.CurrentPotionState
            var baseProperties = new List<DeltaProperty> 
            {
                DeltaProperty.Position,
                DeltaProperty.Rotation,
                DeltaProperty.Health,
                DeltaProperty.Effects,
                DeltaProperty.PathDeletedSegments,
                DeltaProperty.PathRotation
            };

            baseProperties.ForEach(p => StaticStorage.CurrentPotionState[p] = GetBaseProperty(p));

            StaticStorage.CurrentPotionState[DeltaProperty.UsedComponents] = GetUsedComponentsProperty();
            StaticStorage.CurrentPotionState[DeltaProperty.FixedHints] = GetFixedHintsProperty();
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

        private static void RecordNewIngredientInfo(string ingredientName)
        {
            //Record new fixed hint
            RecordNewListAddInfo(DeltaProperty.FixedHints, Managers.RecipeMap.path.fixedPathHints);

            //Record new used component
            RecordUsedComponentInfo(ingredientName);
        }

        private static void RecordUsedComponentInfo(string usedComponentName)
        {
            var usedComponent = Managers.Potion.usedComponents.FirstOrDefault(c => c.componentObject.name == usedComponentName);

            if (usedComponent == null)
            {
                throw new ArgumentException($"Cannot record new used component info for {usedComponentName}. Used component not found in usedComponents list");
            }

            RecordNewListAddInfo(DeltaProperty.UsedComponents, usedComponent, Managers.Potion.usedComponents.IndexOf(usedComponent));
        }

        private static void RecordNewListAddInfo<T>(DeltaProperty property, List<T> sourceList)
        {
            var lastHint = sourceList.LastOrDefault();
            if (lastHint == null)
            {
                throw new InvalidOperationException($"RecordNewListAddInfo should only be called when a new item is added. No {property} found!");
            }

            RecordNewListAddInfo(property, lastHint, sourceList.Count - 1);
        }

        private static void RecordNewListAddInfo<T>(DeltaProperty property, T sourceAdded, int index)
        {
            var fixedHintsList = StaticStorage.CurrentPotionState[property] as ListDelta;
            var newFixedHintDelta = GetBaseAddDelta(sourceAdded, index);
            var lastAddedFixedHintDelta = fixedHintsList.AddDeltas.LastOrDefault();

            //As it currently stands this should never happen but lets make sure this code is bug free with these checks
            if (lastAddedFixedHintDelta != null && lastAddedFixedHintDelta == newFixedHintDelta)
            {
                throw new InvalidOperationException($"RecordNewListAddInfo should only be called when a new item is added. No {property} have been added since last call to RecordNewListAddInfo!");
            }

            fixedHintsList.AddDeltas.Add(lastAddedFixedHintDelta);
        }


        private static void RecordCurrentPathHintInfo()
        {
            throw new NotImplementedException();
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

        private static void RecordProperty(DeltaProperty property)
        {
            var curProperty = GetBaseProperty(property);
            if (StaticStorage.CurrentPotionState[property] != curProperty)
            {
                StaticStorage.CurrentRecipeMarkInfo.Deltas.Add(curProperty); //TODO is it best to simplify here 
                StaticStorage.CurrentPotionState[property] = curProperty;
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
                    new List<DeltaProperty> { DeltaProperty.UsedComponent_Name, DeltaProperty.UsedComponent_Ammount },
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
                DeltaProperty.UsedComponent_Name => new ModifyDelta<string>
                {
                    Property = DeltaProperty.Position,
                    NewValue = ((PotionUsedComponent)obj).componentObject.name
                },
                DeltaProperty.UsedComponent_Ammount => new ModifyDelta<int>
                {
                    Property = DeltaProperty.Position,
                    NewValue = ((PotionUsedComponent)obj).amount
                },
                DeltaProperty.FixedHint_EvenlySpacedPoints => new ModifyDelta<List<Vector3>>
                {
                    Property = DeltaProperty.Rotation,
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
                    Property = DeltaProperty.Rotation,
                    NewValue = (float)Traverse.Create(Managers.RecipeMap.indicator).Field("health").GetValue()
                },
                DeltaProperty.Effects => new ModifyDelta<List<string>>
                {
                    Property = DeltaProperty.Rotation,
                    NewValue = Managers.Potion.collectedPotionEffects.Select(e => e.name).ToList()
                },
                DeltaProperty.PathDeletedSegments => new ModifyDelta<float>
                {
                    Property = DeltaProperty.Rotation,
                    NewValue = Managers.RecipeMap.path.deletedGraphicsSegments
                },
                DeltaProperty.PathRotation => new ModifyDelta<Quaternion>
                {
                    Property = DeltaProperty.Rotation,
                    NewValue = Traverse.Create(Managers.RecipeMap.path.fixedPathHints.FirstOrDefault())
                                       ?.Field("mortarTransform")
                                       ?.GetValue<Transform>()?.rotation 
                                       ?? Quaternion.identity
                },
                _ => throw new ArgumentException($"Property: {property} is not a base property!"),
            };
        }
    }
}
