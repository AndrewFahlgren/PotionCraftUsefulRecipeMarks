using PotionCraft.InputSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System.Collections.Generic;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public static class StaticStorage
    {
        public static List<string> ErrorLog = new();

        public static bool AddedListeners;

        public static Dictionary<int, RecipeMarkInfo> CurrentPotionRecipeMarkInfos = new();
        public static Dictionary<DeltaProperty, BaseDelta> CurrentPotionState = new();
        public static RecipeMarkInfo CurrentRecipeMarkInfo;

        public static Dictionary<DeltaProperty, BaseDelta> SelectedRecipePotionState;
        public static int SelectedRecipeMarkIndex;

        public static Dictionary<int, Dictionary<int, RecipeMarkInfo>> RecipeMarkInfos = new();

        public class SavedStaticStorage
        {
            public List<string> ErrorLog { get; set; }
            public string ModVersion { get; set; }
        }
    }
}
