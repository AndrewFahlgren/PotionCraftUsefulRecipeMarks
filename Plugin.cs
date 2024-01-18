using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using PotionCraftUsefulRecipeMarks.Scripts;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using System;
using System.Linq;
using System.Reflection;

namespace PotionCraftUsefulRecipeMarks
{
    [BepInPlugin(PLUGIN_GUID, "PotionCraftBrewFromHere", PLUGIN_VERSION)]
    [BepInProcess("Potion Craft.exe")]
    [BepInDependency("com.fahlgorithm.potioncraftbookmarkorganizer", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PLUGIN_GUID = "com.fahlgorithm.potioncraftbrewfromhere";
        public const string PLUGIN_VERSION = "1.1.0.1";

        public static ManualLogSource PluginLogger {get; private set; }

        public static void LogInfo(string message) => PluginLogger.LogInfo(message);
        public static void LogError(string message)
        {
            PluginLogger.LogError(message);
            Ex.SaveErrorMessage(message);
        }

        private void Awake()
        {
            PluginLogger = Logger;
            PluginLogger.LogInfo($"Plugin {PLUGIN_GUID} is loaded!");
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), PLUGIN_GUID);
            PluginLogger.LogInfo($"Plugin {PLUGIN_GUID}: Patch Succeeded!");

            StaticStorage.BookmarkOrganizerOldVersionInstalled = Chainloader.PluginInfos.Any(p => p.Value.Metadata.GUID.Equals("com.fahlgorithm.potioncraftbookmarkorganizer") && p.Value.Metadata.Version < new System.Version(1, 0, 5, 3));

            if (StaticStorage.BookmarkOrganizerOldVersionInstalled) PluginLogger.LogError($"An old version of Bookmark Organizer is installed! This version causes some issues for this mod and should be updated as soon as possible!"); ;
        }
    }
}
