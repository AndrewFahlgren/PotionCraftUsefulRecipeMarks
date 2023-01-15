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
                return false;
            }

            //Handle the null case outside of the switch statement;
            if (NewValue == null || delta.NewValue == null) return NewValue == null && delta.NewValue == null;

            return delta.NewValue switch
            {
                string otherValue => 
                    string.Equals(NewValue as string, otherValue),
                float otherValue =>
                    Mathf.Abs((NewValue as float?).Value - otherValue) < 0.00001,
                List<string> otherValue => 
                    (NewValue as List<string>)?.SequenceEqual(otherValue) ?? otherValue == null,
                _ => EqualityComparer<T>.Default.Equals(NewValue, delta.NewValue),
            };
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), NewValue?.GetHashCode() ?? 0);
        }
    }
}
