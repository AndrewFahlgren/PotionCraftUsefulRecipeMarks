using PotionCraft.ObjectBased.Potion;
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
            if (recipeMark.type == SerializedRecipeMark.Type.Ingredient)
            {
                IngredientName = recipeMark.stringValue;
                GrindPercent = recipeMark.floatValue;
            }

            Timeline[recipeMarkIndex] = deltas.Where(d => d.Property == DeltaProperty.FixedHint_Length).ToList();
            if (recipeMarkIndex < AddedRecipeIndex) AddedRecipeIndex = recipeMarkIndex;
        }

        public List<(bool, float)> GetPathDeletionEvents(List<SerializedRecipeMark> allRecipeMarks) 
        {
            var currentLength = -1f;
            var deletionEvents = new List<(bool, float)>();
            Timeline.ToList().ForEach(fixedHintEvent =>
            {
                var lengthDelta = fixedHintEvent.Value.FirstOrDefault(d => d.Property == DeltaProperty.FixedHint_Length) as ModifyDelta<float>;
                if (lengthDelta == null) return;

                if (currentLength < 0)
                {
                    currentLength = lengthDelta.NewValue;
                    return;
                }

                var eventRecipeMark = allRecipeMarks[fixedHintEvent.Key];
                var deleteFromEnd = eventRecipeMark.type == SerializedRecipeMark.Type.Salt && eventRecipeMark.stringValue == DeltaRecordingService.VoidSaltName;
                var deletedLength = currentLength - lengthDelta.NewValue;
                if (deletedLength < 0)
                {
                    throw new Exception("Looks like someone added a new salt. This mod simply cannot handle paths which increase in length.");
                }

                deletionEvents.Add((deleteFromEnd, deletedLength));
                currentLength = lengthDelta.NewValue;
            });
            return deletionEvents;
        }
    }
}
