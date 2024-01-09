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
        MessageCenter.PersistentSubscribe<ScienceEarnedOnRecoveryMessage>(OnScienceEarnedOnRecoveryMessage);
    }

    private void OnGameLoadFinishedMessage(MessageCenterMessage message)
    {
        // Load assets which requires Game UI
        ExistingAssetsLoader.Instance.LoadAssetsFromExistingUI();
        // Build the science regions cache
        if (!ArchiveManager.Instance.IsInitialized)
            ArchiveManager.Instance.InitializeScienceRegionsCache();
    }

    private void OnScienceEarnedOnRecoveryMessage(MessageCenterMessage message)
    {
        MainUIManager.Instance.ArchiveWindowController.Refresh();
    }
}