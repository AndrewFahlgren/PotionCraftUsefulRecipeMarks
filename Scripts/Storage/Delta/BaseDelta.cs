﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta
{
    public abstract class BaseDelta
    {
        public DeltaProperty Property { get; set; }

        public static bool operator ==(BaseDelta obj1, BaseDelta obj2)
        {
            if (obj1 is null || obj2 is null) 
                return obj1 is null && obj2 is null;
            return obj1.Equals(obj2);
        }

        public static bool operator !=(BaseDelta obj1, BaseDelta obj2)
        {
            if (obj1 is null || obj2 is null)
                return obj1 is not null || obj2 is not null;
            return !obj1.Equals(obj2);
        }

        public override bool Equals(object obj)
        {
            return obj is BaseDelta delta && delta.Property == Property;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Property);
        }
    }
}
