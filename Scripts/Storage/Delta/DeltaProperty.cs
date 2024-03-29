﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public enum DeltaProperty
    {
        IndicatorPosition = 0,
        PathPosition = 1,
        IndicatorTargetPosition = 2,
        FollowButtonTargetPosition = 3,
        Rotation = 10,
        PathHints = 20,
        PathDeletedSegments = 30,
        PathAddedFixedHints = 35,
        PathFixedHintsCount = 36,
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
