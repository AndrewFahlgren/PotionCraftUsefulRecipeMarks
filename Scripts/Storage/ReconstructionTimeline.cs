using PotionCraft.ObjectBased.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
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
            var rotationEvents = deltas.Where(d => d.Property == DeltaProperty.Rotation).ToList();
            if (rotationEvents.Any())
            {
                PathRotationTimeline[recipeMarkIndex] = rotationEvents;
            }
        }

        public List<(int index, bool fromEnd, float length)> GetPathDeletionEvents(List<SerializedRecipeMark> allRecipeMarks, int fixedHintIndex)
        {

            var deletionEvents = new List<(int, bool, float)>();
            if (!FixedHintTimelines.TryGetValue(fixedHintIndex, out var fixedHintTimeline))
            {
                return deletionEvents;
            }
            var addedIndex = fixedHintTimeline.AddedRecipeIndex;
            var nextAddedIndex = FixedHintTimelines.Where(p => p.Key > fixedHintIndex)
                                                   .OrderBy(p => p.Key)
                                                   .Select(p => p.Value)
                                                   .FirstOrDefault()
                                                   ?.AddedRecipeIndex ?? int.MaxValue;
            FixedHintTimelines.Values.ToList().ForEach(FixedHintTimeline =>
            {
                var currentLength = -1f;
                FixedHintTimeline.Timeline
                                 .Where(prt => prt.Key < nextAddedIndex)
                                 .ToList()
                                 .ForEach(prt =>
                                 {
                                    var lengthDelta = prt.Value.FirstOrDefault(d => d.Property == DeltaProperty.FixedHint_Length) as ModifyDelta<float>;
                                    if (lengthDelta == null) return;

                                    if (currentLength < 0)
                                    {
                                        currentLength = lengthDelta.NewValue;
                                        return;
                                    }

                                    var eventRecipeMark = allRecipeMarks[prt.Key];
                                    var deleteFromEnd = eventRecipeMark.type == SerializedRecipeMark.Type.Salt && eventRecipeMark.stringValue == DeltaRecordingService.VoidSaltName;
                                    var deletedLength = currentLength - lengthDelta.NewValue;
                                    //Plugin.PluginLogger.LogMessage($"GetPathDeletionEvents for fixedHintIndex={fixedHintIndex}, deletedLength={deletedLength}");
                                    if (deletedLength < 0)
                                    {
                                        throw new Exception("Looks like someone added a new salt. This mod simply cannot handle paths which increase in length.");
                                    }

                                    //Floating point math can cause some issues like this
                                    if (deletedLength < 0.0001f)
                                    {
                                         return;
                                    }

                                    //Plugin.PluginLogger.LogMessage($"GetPathDeletionEvents for fixedHintIndex={fixedHintIndex}, prt.Key={prt.Key}, addedIndex={addedIndex}");

                                    if (prt.Key >= addedIndex)
                                    {
                                        deletionEvents.Add((prt.Key, deleteFromEnd, deletedLength));
                                    }
                                    currentLength = lengthDelta.NewValue;
                                 });
            });
            return deletionEvents;
        }

        public List<(int index, Quaternion rotation)>GetRotationEventsForFixedHint(int fixedHintIndex)
        {
            var rotationEvents = new List<(int, Quaternion)>();
            if (!FixedHintTimelines.TryGetValue(fixedHintIndex, out var fixedHintTimeline))
            {
                return rotationEvents;
            }
            var addedIndex = fixedHintTimeline.AddedRecipeIndex;
            var nextAddedIndex = FixedHintTimelines.Where(p => p.Key > fixedHintIndex)
                                                   .OrderBy(p => p.Key)
                                                   .Select(p => p.Value)
                                                   .FirstOrDefault()
                                                   ?.AddedRecipeIndex ?? int.MaxValue;
            PathRotationTimeline.Where(prt => prt.Key >= addedIndex && prt.Key < nextAddedIndex)
                                .ToList()
                                .ForEach(prt => AddRotationEvent(prt.Key, prt.Value, rotationEvents));

            return rotationEvents;
        }

        private void AddRotationEvent(int index, List<BaseDelta> deltas, List<(int, Quaternion)> rotationEvents)
        {
            var pathRotationDelta = deltas.FirstOrDefault(d => d.Property == DeltaProperty.Rotation) as ModifyDelta<float>;
            if (pathRotationDelta == null) return;
            var previousRotationDelta = PathRotationTimeline.Where(p => p.Key < index)
                                                            .OrderByDescending(p => p.Key)
                                                            .Select(p => p.Value.FirstOrDefault(d => d.Property == DeltaProperty.Rotation) as ModifyDelta<float>)
                                                            .Where(p => p != null)
                                                            .FirstOrDefault();
            var previousRotation = previousRotationDelta?.NewValue ?? 0f;
            var newRotation = pathRotationDelta.NewValue;
            if (newRotation == previousRotation) return;

            rotationEvents.Add((index, Quaternion.Euler(0.0f, 0.0f, newRotation - previousRotation)));
        }
    }
}
