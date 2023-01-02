﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public enum DeltaProperty
    {
        Position = 0,
        Rotation = 10,
        PathHints = 20,
        PathDeletedSegments = 30,
        PathAddedFixedHints = 35,
        PathFixedHintsCount = 36,
        PathRotation = 40,
        FixedHints = 50,
        FixedHint_EvenlySpacedPoints = 51,
        //FixedHint_EvenlySpacedPointsPhysics = 52,
        //FixedHint_StartParameters = 53, TODO we might need these
        //FixedHint_EndParameters = 54,
        UsedComponents = 60,
        UsedComponent_Ammount = 61,
        Effects = 70,
        Health = 80
    }
}
