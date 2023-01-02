using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public class ListDelta : BaseDelta
    {
        public List<ListAddDelta> AddDeltas { get; set; } = new();

        public override bool Equals(object obj)
        {
            var baseEqual = base.Equals(obj);
            if (!baseEqual) return false;
            if (obj is not ListDelta delta)
            {
                throw new ArgumentException("Deltas with the same property enum must be of the same type!");
            }
            return AddDeltas.SequenceEqual(delta.AddDeltas);
        }

        public override int GetHashCode()
        {
            var hashCode = HashCode.Combine(base.GetHashCode(), AddDeltas.Count);
            AddDeltas.ForEach(delta => hashCode = HashCode.Combine(hashCode, delta.GetHashCode()));
            return hashCode;
        }
    }
}
