using PotionCraft.ScriptableObjects.Potion;
using System;
using System.Collections.Generic;
using System.Text;

namespace PotionCraftUsefulRecipeMarks.Scripts.Extensions
{
    public static class PotionExtensions
    {
        public static SerializedPotionRecipeData GetRecipeData(this Potion potion)
        {
            return (SerializedPotionRecipeData)potion.GetSerializedRecipeData();
        }
    }
}
