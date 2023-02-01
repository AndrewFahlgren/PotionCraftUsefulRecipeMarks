using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System.Collections.Generic;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public static class StaticStorage
    {
        public static List<string> ErrorLog = new();

        public static bool AddedListeners;

        public static Dictionary<int, RecipeMarkInfo> CurrentPotionRecipeMarkInfos = new();
        public static Dictionary<DeltaProperty, BaseDelta> CurrentPotionState = new();
        public static RecipeMarkInfo CurrentRecipeMarkInfo;
        public static Dictionary<int, RecipeMarkInfo> PreviousPotionRecipeMarkInfo;

        public static Dictionary<DeltaProperty, BaseDelta> SelectedRecipePotionState;
        public static int SelectedRecipeMarkIndex;
        public static int SelectedRecipeIndex;

        public static Dictionary<int, Dictionary<int, RecipeMarkInfo>> RecipeMarkInfos = new();

        public static bool ShouldLoadLastBrewState;

        #region Old bookmark organizer failsafe fields

        public static bool BookmarkOrganizerOldVersionInstalled;
        public static List<Potion> RecipeIndexes;

        #endregion
    }
}
