using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;
using System.Text;
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

        void Update()
        {
            //if (StaticStorage.HotkeyDown == null || StaticStorage.HotkeyUp == null) return;
            //if (Commands.roomDown.State == State.JustDowned)
            //{
            //    RecipeBookService.FlipPageToNextGroup(false);
            //    StaticStorage.HotkeyDown.SetAction(repeater => RecipeBookService.FlipPageToNextGroup(false)).StopWhen(() => !CanHotkeysBeUsed());
            //}
            //else if (Commands.roomUp.State == State.JustDowned)
            //{ 
            //    RecipeBookService.FlipPageToNextGroup(true);
            //    StaticStorage.HotkeyUp.SetAction(repeater => RecipeBookService.FlipPageToNextGroup(true)).StopWhen(() => !CanHotkeysBeUsed());
            //}
        }

        private bool CanHotkeysBeUsed() => !Managers.Input.HasInputGotToBeDisabled();
    }
}
