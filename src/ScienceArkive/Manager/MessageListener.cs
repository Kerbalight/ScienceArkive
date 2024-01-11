using KSP.Game;
using KSP.Messages;
using ScienceArkive.UI;
using ScienceArkive.UI.Loader;

namespace ScienceArkive.Manager;

public class MessageListener
{
    public static MessageListener Instance { get; } = new();

    public MessageCenter MessageCenter => GameManager.Instance.Game.Messages;

    /// <summary>
    ///     Subscribe to messages from the game, without blocking for the needed delay.
    /// </summary>
    public void SubscribeToMessages()
    {
        _ = Subscribe();
    }

    private async Task Subscribe()
    {
        await Task.Delay(100);
        MessageCenter.PersistentSubscribe<GameLoadFinishedMessage>(OnGameLoadFinishedMessage);
        MessageCenter.PersistentSubscribe<ResearchReportScoredMessage>(OnResearchReportScoredMessage);
        MessageCenter.PersistentSubscribe<VesselScienceSituationChangedMessage>(OnVesselScienceSituationChangedMessage);
        MessageCenter.PersistentSubscribe<GameStateChangedMessage>(HideWindowOnInvalidState);
        MessageCenter.PersistentSubscribe<TechTierUnlockedMessage>(OnTechTierUnlockedMessage);
    }

    private void OnGameLoadFinishedMessage(MessageCenterMessage message)
    {
        // Load assets which requires Game UI
        ExistingAssetsLoader.Instance.LoadAssetsFromExistingUI();
        // Build the science regions cache & check discoverables
        ArchiveManager.Instance.Initialize();
        // Refresh the UI
        MainUIManager.Instance.Refresh();
    }

    private void OnResearchReportScoredMessage(MessageCenterMessage message)
    {
        MainUIManager.Instance.Refresh();
    }

    private void OnVesselScienceSituationChangedMessage(MessageCenterMessage message)
    {
        // Beware, this message is sent for every vessel, not just the active one.
        MainUIManager.Instance.ArchiveWindowController.Refresh();
    }

    /// <summary>
    /// Reload the available experiments when a new tech tier is unlocked.
    /// </summary>
    /// <param name="message"></param>
    private void OnTechTierUnlockedMessage(MessageCenterMessage message)
    {
        ArchiveManager.Instance.InitializeUnlockedExperiments();
        MainUIManager.Instance.ArchiveWindowController.Refresh();
    }

    private void HideWindowOnInvalidState(MessageCenterMessage message)
    {
        if (GameStateManager.Instance.IsInvalidState()) MainUIManager.Instance.ToggleUI(false);
    }
}