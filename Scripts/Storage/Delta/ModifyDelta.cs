using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public abstract class ModifyDelta : BaseDelta
    {
    }

    public class ModifyDelta<T> : ModifyDelta
    {
        public T NewValue { get; set; }

        public override bool Equals(object obj)
        {
            var baseEqual = base.Equals(obj);
            if (!baseEqual) return false;

            //Check to make sure these ModifyDeltas are of the same type
            if (obj is not ModifyDelta<T> delta)
            {
                throw new ArgumentException("Deltas with the same property enum must be of the same type!");
            }

            //Handle the null case outside of the switch statement;
            if (NewValue == null || delta.NewValue == null) return NewValue == null && delta.NewValue == null;

            return delta.NewValue switch
            {
                string otherValue => 
                    string.Equals(NewValue as string, otherValue),
                List<Vector3> otherValue => 
                    (NewValue as List<Vector3>)?.SequenceEqual(otherValue) ?? otherValue == null,
                _ => EqualityComparer<T>.Default.Equals(NewValue, delta.NewValue),
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), NewValue?.GetHashCode() ?? 0);
        }
    }
}
