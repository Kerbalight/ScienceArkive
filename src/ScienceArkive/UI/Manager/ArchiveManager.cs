using BepInEx.Logging;
using KSP.Game;
using KSP.Game.Science;
using KSP.Messages;
using System.Reflection;
namespace ScienceArkive.UI.Manager
{
    public class ArchiveManager
    {
        public static ArchiveManager Instance { get; } = new();

        public MessageCenter MessageCenter => GameManager.Instance.Game.Messages;

        public Dictionary<string, ScienceRegionDefinition[]> Regions { get; private set; }

        private static readonly ManualLogSource logger = Logger.CreateLogSource("ScienceArkive.ArchiveManager");

        public ArchiveManager()
        { 
        }

        /// <summary>
        /// Subscribe to messages from the game, without blocking for the needed delay.
        /// </summary>
        public void SubscribeToMessages() => _ = Subscribe();
        private async Task Subscribe()
        {
            await Task.Delay(100);
            MessageCenter.Subscribe<GameLoadFinishedMessage>(OnGameLoadFinishedMessage);
        }

        private void OnGameLoadFinishedMessage(MessageCenterMessage message)
        {
            BuildScienceRegionsCache();
        }

        /// <summary>
        /// When the game loads, build a cache of all the science regions for each celestial body.
        /// We are doing it here since we are using the private ScienceRegionsDataProvider class.
        /// </summary>
        private void BuildScienceRegionsCache()
        {
            var dataProvider = GameManager.Instance?.Game?.ScienceManager.ScienceRegionsDataProvider;
            if (dataProvider == null)
            {
                logger.LogInfo("No ScienceRegionsDataProvider found, skipping");
                return;
            }

            // Get the CelestialBodyScienceRegionsData dictionary, which is private.
            var cbToScienceRegions = typeof(ScienceRegionsDataProvider)
                .GetField("_cbToScienceRegions", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(dataProvider) as Dictionary<string, CelestialBodyScienceRegionsData>;

            Regions = [];
            foreach (var cb in cbToScienceRegions.Keys)
            {
                var bodyRegions = cbToScienceRegions[cb].Regions;
                logger.LogInfo($"Found {bodyRegions.Length} regions for {cb}");
                Regions.Add(cb, bodyRegions);
            }
        }

        public ScienceRegionDefinition[] GetRegionsForBody(string bodyName)
        {
            if (Regions.TryGetValue(bodyName, out var regions))
            {
                return regions;
            }
            else
            {
                logger.LogWarning($"No regions found for {bodyName}");
                return new ScienceRegionDefinition[0];
            }
        }
    }

    
}
