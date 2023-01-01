using System;
using System.Collections.Generic;
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
            return AddDeltas.Count == delta.AddDeltas.Count;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(base.GetHashCode(), AddDeltas.Count);
        }
    }
}
