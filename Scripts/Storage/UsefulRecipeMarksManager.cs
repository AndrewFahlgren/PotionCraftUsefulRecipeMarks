using PotionCraft.InputSystem;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace PotionCraftUsefulRecipeMarks.Scripts.Storage
{
    public class UsefulRecipeMarksManager : MonoBehaviour
    {
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
