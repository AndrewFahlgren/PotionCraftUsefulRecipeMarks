using PotionCraft.ObjectBased.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public class ReconstructionTimeline
    {
        public Dictionary<int, FixedHintTimeline> FixedHintTimelines = new ();

        public SortedDictionary<int, List<BaseDelta>> PathRotationTimeline = new ();

        public void AddBasePropertyInformationFromDeltas(List<SerializedRecipeMark> recipeMarks, int recipeMarkIndex, List<BaseDelta> deltas)
        {
            PathRotationTimeline[recipeMarkIndex] = deltas.Where(d => d.Property == DeltaProperty.PathRotation).ToList();
        }

        public List<Quaternion> GetRotationEventsToApplyBeforeAddingFixedHint(int fixedHintIndex)
        {
            var rotationEvents = new List<Quaternion>();
            if (!FixedHintTimelines.TryGetValue(fixedHintIndex, out var fixedHintTimeline))
            {
                return rotationEvents;
            }

            var fixedHintRecipeMarkIndex = fixedHintTimeline.AddedRecipeIndex;
            var previousRecipeMarkIndex = FixedHintTimelines.Values.ToList()
                                                                   .OrderByDescending(fht => fht.AddedRecipeIndex)
                                                                   .FirstOrDefault(fht => fht.AddedRecipeIndex < fixedHintRecipeMarkIndex)
                                                                   ?.AddedRecipeIndex ?? 0;
            PathRotationTimeline.Where(prt => prt.Key >= previousRecipeMarkIndex && prt.Key < fixedHintRecipeMarkIndex).ToList().ForEach(prt =>
            {
                var pathRotationDelta = prt.Value.FirstOrDefault(d => d.Property == DeltaProperty.PathRotation) as ModifyDelta<Quaternion>;
                if (pathRotationDelta == null) return;
                rotationEvents.Add(pathRotationDelta.NewValue);
            });
            return rotationEvents;
        }

        public List<Quaternion> GetRotationEventsToApplyAfterAllFixedHints()
        {
            var rotationEvents = new List<Quaternion>();
            var previousRecipeMarkIndex = FixedHintTimelines.Values.LastOrDefault()?.AddedRecipeIndex ?? 0;
            PathRotationTimeline.Where(prt => prt.Key >= previousRecipeMarkIndex).ToList().ForEach(prt =>
            {
                var pathRotationDelta = prt.Value.FirstOrDefault(d => d.Property == DeltaProperty.PathRotation) as ModifyDelta<Quaternion>;
                if (pathRotationDelta == null) return;
                rotationEvents.Add(pathRotationDelta.NewValue);
            });
            return rotationEvents;
        }
    }
}
