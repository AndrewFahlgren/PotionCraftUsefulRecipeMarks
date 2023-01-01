﻿using PotionCraft.InputSystem;
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
        public const string BookmarkGroupsJsonSaveName = "FahlgorithmUsefulRecipeMarks";

        public static List<string> ErrorLog = new();

        public static string StateJsonString;

        public static Dictionary<int, RecipeMarkInfo> CurrentPotionRecipeMarkInfos = new();
        public static Dictionary<DeltaProperty, BaseDelta> CurrentPotionState = new();
        public static RecipeMarkInfo CurrentRecipeMarkInfo = new();

        public static Dictionary<int, Dictionary<int, RecipeMarkInfo>> RecipeMarkInfos = new();

        public class SavedStaticStorage
        {
            public List<string> ErrorLog { get; set; }
            public string ModVersion { get; set; }
        }
    }
}
