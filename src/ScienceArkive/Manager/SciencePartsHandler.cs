using BepInEx.Logging;
using KSP.Game;
using KSP.Sim.Definitions;
using UnityEngine;
using Logger = BepInEx.Logging.Logger;

namespace ScienceArkive.Manager;

public class SciencePartsHandler
{
    public static SciencePartsHandler Instance { get; } = new();

    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.SciencePartsHandler");

    private SciencePartsHandler()
    {
    }

    public bool IsGameModeFeatureEnabled(string featureId)
    {
        return GameManager.Instance.GameModeManager.IsGameModeFeatureEnabled(featureId);
    }

    public bool IsPartUnlocked(PartCore part)
    {
        if (!IsGameModeFeatureEnabled("SciencePartUnlock")) return true;
        if (GameManager.Instance.Game.CheatSystem.Get(CheatSystemItemID.UnlockAllParts)) return true;
        if (GameManager.Instance.Game.ScienceManager?.TechNodeDataStore?.PartIDLookup == null)
        {
            _Logger.LogError("Part " + part.data.partName + "is not available, error in ScienceManager lookup");
            return false;
        }

        if (!GameManager.Instance.Game.CampaignPlayerManager.TryGetMyCampaignPlayerEntry(out var campaignPlayerEntry))
        {
            _Logger.LogError("Cannot get CampaignPlayerManager");
            return false;
        }

        return GameManager.Instance.Game.ScienceManager.TechNodeDataStore.PartIDLookup.TryGetValue(part.data.partName,
                   out var techNodeName) &&
               !string.IsNullOrEmpty(techNodeName) && campaignPlayerEntry.UnlockedTechNodes.Contains(techNodeName);
    }
}