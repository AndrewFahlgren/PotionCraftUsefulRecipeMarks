using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public enum DeltaProperty
    {
        IndicatorPosition = 0,
        PathPosition = 1,
        Rotation = 10,
        PathHints = 20,
        PathDeletedSegments = 30,
        PathAddedFixedHints = 35, //TODO get rid of this and just count ingredient marks
        PathFixedHintsCount = 36,
        PathRotation = 40,
        FixedHints = 50,
        FixedHint_Length = 53,
        FixedHint_ConnectionPoint = 54,
        UsedComponents = 60,
        UsedComponent_Ammount = 61,
        Effects = 70,
        Health = 80,
        OldSerializedPath = 1000,
        IsOldFixedHint = 1010
    }
}
