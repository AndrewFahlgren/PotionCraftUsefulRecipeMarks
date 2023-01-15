using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts
{
    public static class Extensions
    {
        public static (float x, float y) ToTuple(this Vector3 vector)
        {
            return ((Vector2)vector).ToTuple();
        }

        public static (float x, float y) ToTuple(this Vector2 vector)
        {
            return (vector.x, vector.y);
        }

        public static Vector2 ToVector(this (float x, float y) tuple)
        {
            return new Vector2(tuple.x, tuple.y);
        }
    }
}
