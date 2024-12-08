using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.ScriptableObjects.Potion;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System.Collections.Generic;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public class UsefulRecipeMarksManager : MonoBehaviour
    {
        public Dictionary<int, RecipeMarkInfo> CurrentPotionRecipeMarkInfos => StaticStorage.CurrentPotionRecipeMarkInfos;
        public Dictionary<DeltaProperty, BaseDelta> CurrentPotionState => StaticStorage.CurrentPotionState;
        public RecipeMarkInfo CurrentRecipeMarkInfo => StaticStorage.CurrentRecipeMarkInfo;
        public Dictionary<int, Dictionary<int, RecipeMarkInfo>> RecipeMarkInfos => StaticStorage.RecipeMarkInfos;
        public Dictionary<int, RecipeMarkInfo> PreviousPotionRecipeMarkInfo => StaticStorage.PreviousPotionRecipeMarkInfo;
        public Dictionary<DeltaProperty, BaseDelta> SelectedRecipePotionState => StaticStorage.SelectedRecipePotionState;
        public int SelectedRecipeMarkIndex => StaticStorage.SelectedRecipeMarkIndex;
        public int SelectedRecipeIndex => StaticStorage.SelectedRecipeIndex;
        public List<IRecipeBookPageContent> RecipeIndexes => StaticStorage.RecipeIndexes;
    }
}
