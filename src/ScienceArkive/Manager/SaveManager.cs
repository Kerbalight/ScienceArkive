using BepInEx.Logging;
using KSP.Game;
using ScienceArkive.Data;
using ScienceArkive.UI;
using SpaceWarp.API.SaveGameManager;

namespace ScienceArkive.Manager;

public class SaveManager
{
    public static SaveManager Instance { get; private set; } = new();
    private readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.SaveManager");

    private SaveData? loadedSaveData;

    public void Register()
    {
        ModSaves.RegisterSaveLoadGameData<SaveData>(ScienceArkivePlugin.ModGuid, SaveGameData, LoadGameData);
    }

    private void SaveGameData(SaveData dataToSave)
    {
        dataToSave.WindowPosition = MainUIManager.Instance.ArchiveWindowController.WindowPosition;
        dataToSave.SelectedBody = MainUIManager.Instance.ArchiveWindowController.SelectedCelestialBody?.Name;
        dataToSave.DiscoveredBodies = ArchiveManager.Instance.DiscoveredBodies.ToList();
    }

    private void LoadGameData(SaveData dataToLoad)
    {
        loadedSaveData = dataToLoad;
        _Logger.LogInfo("Loaded game data");
    }

    /// <summary>
    /// Called when the UI is built, to load the saved data into the UI.
    /// </summary>
    public void LoadDataIntoUI()
    {
        if (loadedSaveData == null) return;

        MainUIManager.Instance.ArchiveWindowController.WindowPosition = loadedSaveData.WindowPosition;

        if (loadedSaveData.SelectedBody != null)
        {
            var body = GameManager.Instance.Game?.UniverseModel.FindCelestialBodyByName(loadedSaveData.SelectedBody);
            if (body != null)
                MainUIManager.Instance.ArchiveWindowController.SelectedCelestialBody = body;
            else
                _Logger.LogWarning($"Could not find body {loadedSaveData.SelectedBody}");
        }

        if (loadedSaveData.DiscoveredBodies != null)
            ArchiveManager.Instance.DiscoveredBodies = [..loadedSaveData.DiscoveredBodies];

        MainUIManager.Instance.ArchiveWindowController.ReloadAfterSaveLoad();
        loadedSaveData = null;
    }
}