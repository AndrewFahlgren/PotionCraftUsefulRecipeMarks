using HarmonyLib;
using Newtonsoft.Json;
using PotionCraft.ManagersSystem;
using PotionCraft.ManagersSystem.SaveLoad;
using PotionCraft.ObjectBased.UIElements.Books.RecipeBook;
using PotionCraft.SaveFileSystem;
using PotionCraft.SaveLoadSystem;
using PotionCraftUsefulRecipeMarks.Scripts.Services;
using PotionCraftUsefulRecipeMarks.Scripts.Storage;
using PotionCraftUsefulRecipeMarks.Scripts.Storage.Delta;
using System;
using System.Collections.Generic;

namespace PotionCraftUsefulRecipeMarks.Scripts
{
    public static class Saver
    {

        #region Customize this for your mod

        public const string JsonSaveName = "FahlgorithmUsefulRecipeMarks";

        /// <summary>
        /// Fill this class with all of the properties you want to save.
        /// These properties can be any type!
        /// </summary>
        public class SaveState
        {
            public Dictionary<int, RecipeMarkInfo> CurrentPotionRecipeMarkInfos { get; set; }
            public Dictionary<DeltaProperty, BaseDelta> CurrentPotionState { get; set; }
            public RecipeMarkInfo CurrentRecipeMarkInfo { get; set; }
            public Dictionary<int, RecipeMarkInfo> PreviousPotionRecipeMarkInfo { get; set; }
            public Dictionary<DeltaProperty, BaseDelta> SelectedRecipePotionState { get; set; }
            public int SelectedRecipeMarkIndex { get; set; }
            public Dictionary<int, Dictionary<int, RecipeMarkInfo>> RecipeMarkInfos { get; set; }
            public List<string> ErrorLog { get; set; }
            public string Version { get; set; }
        }

        /// <summary>
        /// Use this method to round up all of the data you need to save and put it in a SaveState object
        /// </summary>
        private static SaveState GetSaveState()
        {
            return new SaveState
            {
                CurrentPotionRecipeMarkInfos = StaticStorage.CurrentPotionRecipeMarkInfos,
                CurrentPotionState = StaticStorage.CurrentPotionState,
                CurrentRecipeMarkInfo = StaticStorage.CurrentRecipeMarkInfo,
                PreviousPotionRecipeMarkInfo = StaticStorage.PreviousPotionRecipeMarkInfo,
                SelectedRecipePotionState = StaticStorage.SelectedRecipePotionState,
                SelectedRecipeMarkIndex = StaticStorage.SelectedRecipeMarkIndex,
                RecipeMarkInfos = StaticStorage.RecipeMarkInfos,
                ErrorLog = StaticStorage.ErrorLog,
                Version = Plugin.PLUGIN_VERSION
            };
        }

        /// <summary>
        /// Use this method to do whatever you need to do with the loaded data
        /// </summary>
        private static void LoadSaveState(SaveState loadedSaveState)
        {
            SaveLoadService.ClearFileSpecificDataOnFileLoad();

            //Check to make sure it deserialized properly
            if (loadedSaveState == null)
            {
                LogError("Error: An error occured during deserialization. Could not find load save state!");
                return;
            }

            if (loadedSaveState.CurrentPotionRecipeMarkInfos == null 
                || loadedSaveState.CurrentPotionState == null
                || loadedSaveState.RecipeMarkInfos == null
                || loadedSaveState.ErrorLog == null)
            {
                LogError("Error: An error occured during deserialization. A property was null!");
                return;
            }

            //Actually load in the data
            StaticStorage.CurrentPotionRecipeMarkInfos = loadedSaveState.CurrentPotionRecipeMarkInfos;
            StaticStorage.CurrentPotionState = loadedSaveState.CurrentPotionState;
            StaticStorage.CurrentRecipeMarkInfo = loadedSaveState.CurrentRecipeMarkInfo;
            StaticStorage.PreviousPotionRecipeMarkInfo = loadedSaveState.PreviousPotionRecipeMarkInfo;
            StaticStorage.SelectedRecipePotionState = loadedSaveState.SelectedRecipePotionState;
            StaticStorage.SelectedRecipeMarkIndex = loadedSaveState.SelectedRecipeMarkIndex;
            StaticStorage.RecipeMarkInfos = loadedSaveState.RecipeMarkInfos;
            StaticStorage.ErrorLog = loadedSaveState.ErrorLog;

            //Update recipe book with selected recipe mark
            var recipeIndex = Managers.Potion.recipeBook.currentPageIndex;
            if (!RecipeReconstructionService.MarkHasSavedData(recipeIndex, StaticStorage.SelectedRecipeMarkIndex))
            {
                var rightPage = Managers.Potion.recipeBook.curlPageController.frontRightPage;
                var rightPageContent = (RecipeBookRightPageContent)rightPage.pageContent;
                var visibleMarksDict = (Dictionary<int, List<RecipeBookRecipeMark>>)Traverse.Create(rightPageContent).Field("visibleMarks").GetValue();
                RecipeBookUIService.DisableOldRecipeMarks(visibleMarksDict);
            }
            else
            {
                RecipeBookUIService.UpdateRecipeBookPageForSelectedRecipeMark(recipeIndex, StaticStorage.SelectedRecipeMarkIndex);
            }
        }

        private static void LogError(string errorMessage)
        {
            //Do some error logging
            Plugin.PluginLogger.LogInfo(errorMessage);
        }

        #endregion

        #region patches

        [HarmonyPatch(typeof(SavedState), "ToJson")]
        public class SavedState_ToJson
        {
            static void Postfix(ref string __result)
            {
                StoreData(ref __result);
            }
        }

        [HarmonyPatch(typeof(SaveLoadManager), "LoadSelectedState")]
        public class SaveLoadManager_LoadSelectedState
        {
            static bool Prefix(Type type)
            {
                return RetreiveStoredData(type);
            }
        }

        [HarmonyPatch(typeof(File), "Load")]
        public class File_Load
        {
            static bool Prefix(File __instance)
            {
                return RetrieveStateJsonString(__instance);
            }
        }

        #endregion

        public static string StateJsonString;

        public static void StoreData(ref string result)
        {
            string modifiedResult = null;
            try
            {
                var savedStateJson = result;
                //Serialize data to json
                var toSerialize = GetSaveState();

                if (toSerialize == null)
                {
                    LogError("Error: no data found to save!");
                }

                var serializedData = JsonConvert.SerializeObject(toSerialize, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto });

                var serialized = $",\"{JsonSaveName}\":{serializedData}";
                //Insert custom field at the end of the save file at the top level
                var insertIndex = savedStateJson.LastIndexOf('}');
                modifiedResult = savedStateJson.Insert(insertIndex, serialized);
            }
            catch (Exception ex) 
            {
                LogError($"{ex.GetType()}: {ex.Message}\r\n{ex.StackTrace}\r\n{ex.InnerException?.Message}");
            }
            if (!string.IsNullOrEmpty(modifiedResult))
            {
                result = modifiedResult;
            }
        }

        /// <summary>
        /// Reads the raw json string to find our custom field and parse any bookmark groups within it
        /// </summary>
        public static bool RetreiveStoredData(Type type)
        {
            if (type != typeof(ProgressState)) return true;

            try
            {
                var stateJsonString = StateJsonString;
                StateJsonString = null;
                if (string.IsNullOrEmpty(stateJsonString))
                {
                    LogError("Error: stateJsonString is empty. Cannot load data.");
                    return true;
                }

                //Check if there are any existing bookmark groups in save file
                var keyIndex = stateJsonString.IndexOf(JsonSaveName);
                if (keyIndex == -1)
                {
                    LogError("No existing data found during load");
                    return true;
                }

                //Deserialize the bookmark groups from json using our dummy class
                var deserialized = JsonConvert.DeserializeObject<Deserialized<SaveState>>(stateJsonString, new JsonSerializerSettings { ReferenceLoopHandling = ReferenceLoopHandling.Ignore, TypeNameHandling = TypeNameHandling.Auto });

                LoadSaveState(deserialized.DeserializedObject);

            }
            catch (Exception ex)
            {
                LogError($"{ex.GetType()}: {ex.Message}\r\n{ex.StackTrace}\r\n{ex.InnerException?.Message}");
            }
            return true;
        }

        /// <summary>
        /// This method retrieves the raw json string and stores it in static storage for later use.
        /// The StateJsonString is inaccessible later on when we need it so this method is necessary to provide access to it.
        /// </summary>
        public static bool RetrieveStateJsonString(File instance)
        {
            try
            {
                StateJsonString = instance.StateJsonString;
            }
            catch (Exception ex)
            {
                LogError($"{ex.GetType()}: {ex.Message}\r\n{ex.StackTrace}\r\n{ex.InnerException?.Message}");
            }
            return true;
        }

        private class Deserialized<T>
        {
            [JsonProperty(JsonSaveName)]
            public T DeserializedObject { get; set; }
        }
    }
}
