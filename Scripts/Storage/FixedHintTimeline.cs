using PotionCraft.ObjectBased.Potion;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public class FixedHintTimeline
    {
        public int FixedHintIndex { get; set; }
        public string IngredientName { get; set; }
        public float GrindPercent { get; set; }
        public int AddedRecipeIndex { get; set; } = int.MaxValue;

        public SortedDictionary<int, List<ModifyDelta>> Timeline { get; set; } = new();

        public void AddInformationFromDeltas(List<SerializedRecipeMark> recipeMarks, int recipeMarkIndex, List<ModifyDelta> deltas)
        {
            var recipeMark = recipeMarks[recipeMarkIndex];
            if (recipeMark.type == RecipeBookRecipeMarkType.Ingredient)
            {
                IngredientName = recipeMark.stringValue;
                GrindPercent = recipeMark.floatValue;
            }

            Timeline[recipeMarkIndex] = deltas.Where(d => d.Property == DeltaProperty.FixedHint_Length).ToList();
            if (recipeMarkIndex < AddedRecipeIndex) AddedRecipeIndex = recipeMarkIndex;
        }
    }
}
