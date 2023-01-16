using HarmonyLib;
using PotionCraft.ManagersSystem;
using PotionCraft.ObjectBased.UIElements.Bookmarks;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;

namespace PotionCraftUsefulRecipeMarks.Scripts.Patches
{
    public class OldBookmarkOrganizerFailsafePatch
    {
        private const float SwapAboveY = 1.2f;
        private const float DontSwapBelowX = -0.6f;

        private static bool ShouldSwapOnRelease;

        [HarmonyPatch(typeof(Bookmark), "UpdateMovingState")]
        public class Bookmark_UpdateMovingState
        {
            [HarmonyAfter("com.fahlgorithm.potioncraftbookmarkorganizer")]
            static void Postfix(Bookmark.MovingState value)
            {
                Ex.RunSafe(() => BookmarkMovingStateChanged(value));
            }
        }

        [HarmonyPatch(typeof(Bookmark), "UpdateMoving")]
        public class Bookmark_UpdateMoving
        {
            static void Postfix(Bookmark __instance)
            {
                Ex.RunSafe(() => DetermineIfShouldSwapOnRelease(__instance));
            }
        }

        [HarmonyPatch(typeof(RecipeBook), "OnLoad")]
        public class RecipeBook_OnLoad
        {
            static void Postfix()
            {
                Ex.RunSafe(() => SaveLoadService.UpdateFailsafeListOnLoad());
            }
        }

        private static void BookmarkMovingStateChanged(Bookmark.MovingState value)
        {
            if (!StaticStorage.BookmarkOrganizerOldVersionInstalled) return;
            switch (value)
            {
                case Bookmark.MovingState.Idle:
                    if (!ShouldSwapOnRelease) break;
                    //Get rid of our preview
                    ShouldSwapOnRelease = false;
                    RecipeBookUIService.DoOldBookmarkOrganizerFailsfe();
                    break;
                case Bookmark.MovingState.Moving:
                    break;
            }
        }

        private static void DetermineIfShouldSwapOnRelease(Bookmark instance)
        {
            if (!StaticStorage.BookmarkOrganizerOldVersionInstalled) return;
            if (instance.CurrentMovingState == Bookmark.MovingState.Idle) return;
            var bookmarkController = Managers.Potion.recipeBook.bookmarkControllersGroupController.controllers[0].bookmarkController;
            if (bookmarkController.rails.Count < 8) return;
            var subRail = bookmarkController.rails[7];
            if (instance.rail != subRail)
            {
                if (ShouldSwapOnRelease)
                {
                    ShouldSwapOnRelease = false;
                }
                return;
            }
            if (ShouldSwapOnRelease) return;
            var mouseWorldPosition = Managers.Input.controlsProvider.CurrentMouseWorldPosition;
            var swapAboveYWorld = subRail.transform.position.y + SwapAboveY;
            var dontSwapBelowXWorld = subRail.transform.position.x - DontSwapBelowX;
            ShouldSwapOnRelease = mouseWorldPosition.y > swapAboveYWorld && mouseWorldPosition.x > dontSwapBelowXWorld;
        }
    }
}
