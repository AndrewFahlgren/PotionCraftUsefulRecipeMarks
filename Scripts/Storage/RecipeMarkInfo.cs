using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public class RecipeMarkInfo
    {
        public int Index { get; set; }
        public List<BaseDelta> Deltas { get; set; } = new();
    }
}
