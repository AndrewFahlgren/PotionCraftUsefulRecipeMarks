using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public class ListAddDelta : BaseDelta
    {
        public int Index { get; set; }

        public List<ModifyDelta> Deltas { get; set; } = new();


        public override bool Equals(object obj)
        {
            var baseEqual = base.Equals(obj);
            if (!baseEqual) return false;
            if (obj is not ListAddDelta delta)
            {
                throw new ArgumentException("Deltas with the same property enum must be of the same type!");
            }
            return Index == delta.Index && Deltas.SequenceEqual(delta.Deltas);
        }

        public override int GetHashCode()
        {
            var hashCode = base.GetHashCode();
            Deltas.ForEach(delta => hashCode = HashCode.Combine(hashCode, delta.GetHashCode()));
            return hashCode;
        }
    }
}
