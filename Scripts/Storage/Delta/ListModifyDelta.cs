using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public class ListModifyDelta<T> : ModifyDelta<T>
    {
        public int Index { get; set; }
    }
}
