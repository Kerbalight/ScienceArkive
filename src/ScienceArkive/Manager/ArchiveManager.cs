using System.Reflection;
using BepInEx.Logging;
using KSP.Game;
using KSP.Game.Science;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim;
using ScienceArkive.API.Extensions;
using ScienceArkive.UI.Loader;

namespace ScienceArkive.Manager;

public class ArchiveManager
{
    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.ArchiveManager");

    public static ArchiveManager Instance { get; } = new();

    private bool IsInitialized { get; set; }
    public Dictionary<string, CelestialBodyScienceRegionsData> CelestialBodiesScienceData { get; private set; } = new();

    private TravelFirsts _firsts = null!;
    private readonly List<string> _unlockedExperimentsIds = [];


    public void Initialize()
    {
        var dataProvider = GameManager.Instance.Game?.ScienceManager?.ScienceRegionsDataProvider;
        if (dataProvider == null)
        {
            _Logger.LogError("No ScienceRegionsDataProvider found, skipping");
            return;
        }

        // Build the cache of science regions
        InitializeScienceRegionsCache();
        // Save reference to `TravelFirsts` for later use. We need to do this here since it's private.
        InitializeTravelFirst();
        // Save the list of unlocked experiments
        InitializeUnlockedExperiments();

        IsInitialized = true;
    }

    /// <summary>
    ///     When the game loads, build a cache of all the science regions for each celestial body.
    ///     We are doing it here since we are using the private ScienceRegionsDataProvider class.
    /// </summary>
    private void InitializeScienceRegionsCache()
    {
        if (IsInitialized)
        {
            _Logger.LogInfo("Science regions cache already initialized, skipping");
            return;
        }

        var dataProvider = GameManager.Instance.Game.ScienceManager.ScienceRegionsDataProvider;

        // Get the CelestialBodyScienceRegionsData dictionary, which is private.
        var cbToScienceRegions = typeof(ScienceRegionsDataProvider)
            .GetField("_cbToScienceRegions", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(dataProvider) as Dictionary<string, CelestialBodyScienceRegionsData>;

        if (cbToScienceRegions == null)
        {
            _Logger.LogWarning("No CelestialBodyScienceRegionsData found, skipping");
            return;
        }

        CelestialBodiesScienceData = [];
        foreach (var cb in cbToScienceRegions.Keys)
        {
            var bodyScienceData = cbToScienceRegions[cb];
            CelestialBodiesScienceData.Add(cb, bodyScienceData);
        }

        _Logger.LogInfo($"Found {CelestialBodiesScienceData.Count} celestial bodies with science regions");
    }

    /// <summary>
    /// Reloads the private `TravelFirsts` class, which is used to check if a discoverable has been found.
    /// We don't check to `IsInitialized` since it changes every time a new game is loaded.
    /// </summary>
    private void InitializeTravelFirst()
    {
        var travelFirsts = typeof(TravelLogManager)
            .GetField("_firsts", BindingFlags.NonPublic | BindingFlags.Instance)
            ?.GetValue(GameManager.Instance.Game.TravelLogManager) as TravelFirsts;

        if (travelFirsts == null)
        {
            _Logger.LogWarning("No TravelFirsts found, skipping");
            return;
        }

        _firsts = travelFirsts;
    }

    /// <summary>
    /// Checks for all the experiments which are unlocked by the player.
    /// Experiments are unlocked if a part which contains them is unlocked in the tech tree.
    /// </summary>
    public void InitializeUnlockedExperiments()
    {
        var experimentDataStore = GameManager.Instance.Game.ScienceManager.ScienceExperimentsDataStore;
        var allExperimentIds =
            experimentDataStore.GetAllExperimentIDs();

        // Get all the experiments from all the parts
        HashSet<string> availableExperimentIds =
        [
            // EVA (always available)
            "SurfaceSurvey",
            "CrewReport"
        ];


        // Get all the experiments from all the parts
        var allParts = GameManager.Instance.Game.Parts.AllParts();
        foreach (var part in allParts)
        {
            var scienceExperiment = part.GetSerializedModuleData<Data_ScienceExperiment>();
            if (scienceExperiment == null) continue;
            if (!SciencePartsHandler.Instance.IsPartUnlocked(part)) continue;

            foreach (var experimentConfiguration in scienceExperiment.Experiments)
            {
                var expId = experimentConfiguration.ExperimentDefinitionID;
                if (expId == null) continue;
                availableExperimentIds.Add(expId);
            }
        }

        _unlockedExperimentsIds.Clear();
        _unlockedExperimentsIds.AddRange(availableExperimentIds);
    }

    /// <summary>
    /// List of celestial bodies which have been reached by the player.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetCelestialBodiesNames(bool onlyDiscovered = false)
    {
        return CelestialBodiesScienceData.Keys.Where(cb => !onlyDiscovered || _firsts.SOIReached.ContainsKey(cb));
    }

    /// <summary>
    /// All the science regions for a given celestial body.
    /// This includes discoverable regions which have not been reached yet.
    /// </summary>
    public IEnumerable<ScienceRegionDefinition> GetRegionsForBody(string bodyName, bool onlyDiscovered = false)
    {
        if (!CelestialBodiesScienceData.TryGetValue(bodyName, out var scienceData))
        {
            _Logger.LogWarning($"No science data found for {bodyName}");
            return [];
        }

        if (!onlyDiscovered) return scienceData.Regions;

        var scienceRegionsDataProvider = GameManager.Instance.Game.ScienceManager.ScienceRegionsDataProvider;
        var regions = new List<ScienceRegionDefinition>();
        foreach (var region in scienceData.Regions)
            if (!scienceRegionsDataProvider.IsRegionADiscoverable(bodyName, region.Id) ||
                _firsts.DiscoverableReached.ContainsKey($"{bodyName}_{region.Id}"))
                regions.Add(region);

        return regions;
    }

    public bool IsRegionVisible(string bodyName, string regionId, out bool isDiscoverable)
    {
        var scienceRegionsDataProvider = GameManager.Instance.Game.ScienceManager.ScienceRegionsDataProvider;
        isDiscoverable = scienceRegionsDataProvider.IsRegionADiscoverable(bodyName, regionId);
        return !isDiscoverable ||
               _firsts.DiscoverableReached.ContainsKey($"{bodyName}_{regionId}");
    }

    public bool IsExperimentUnlocked(string experimentId)
    {
        return _unlockedExperimentsIds.Contains(experimentId);
    }

    public List<ExperimentDefinition> GetExperimentDefinitions(bool onlyUnlocked = false)
    {
        var experimentDataStore = GameManager.Instance.Game.ScienceManager.ScienceExperimentsDataStore;
        var experimentIds = onlyUnlocked ? _unlockedExperimentsIds : experimentDataStore.GetAllExperimentIDs();

        var experimentDefinitions = new List<ExperimentDefinition>();
        foreach (var experimentId in experimentIds)
            experimentDefinitions.Add(experimentDataStore.GetExperimentDefinition(experimentId));

        return experimentDefinitions;
    }

    public float GetScienceDifficultyMultiplier()
    {
        var sessionManager = GameManager.Instance.Game.SessionManager;
        if (!sessionManager.TryGetDifficultyOptionState<float>("ScienceRewards",
                out var scienceMultiplier)) scienceMultiplier = 1f;

        return scienceMultiplier;
    }

    public void GetResearchLocationScalar(ResearchLocation location, out float scienceScalar)
    {
        if (CelestialBodiesScienceData.TryGetValue(location.BodyName, out var scienceData))
        {
            var bodyScalar = scienceData.SituationData.CelestialBodyScalar;
            var situationScalar = 1f;
            switch (location.ScienceSituation)
            {
                case ScienceSitutation.Splashed:
                    situationScalar = scienceData.SituationData.SplashedScalar;
                    break;
                case ScienceSitutation.Landed:
                    situationScalar = scienceData.SituationData.LandedScalar;
                    break;
                case ScienceSitutation.LowOrbit:
                    situationScalar = scienceData.SituationData.LowOrbitScalar;
                    break;
                case ScienceSitutation.HighOrbit:
                    situationScalar = scienceData.SituationData.HighOrbitScalar;
                    break;
                case ScienceSitutation.Atmosphere:
                    situationScalar = scienceData.SituationData.AtmosphereScalar;
                    break;
                default:
                    _Logger.LogWarning($"GetResearchLocationScalar: Unknown situation {location.ScienceSituation}");
                    break;
            }

            var regionScalar = 1f;
            // See `GetScienceRegionForVessel()`
            if (location.RequiresRegion)
            {
                var region = scienceData.Regions.FirstOrDefault(r => r.Id == location.ScienceRegion);
                if (region != null)
                    switch (location.ScienceSituation)
                    {
                        case ScienceSitutation.Splashed:
                            regionScalar = region.SplashedScalar;
                            break;
                        case ScienceSitutation.Landed:
                            regionScalar = region.LandedScalar;
                            break;
                        case ScienceSitutation.Atmosphere:
                            regionScalar = region.AtmosphereScalar;
                            break;
                        case ScienceSitutation.LowOrbit:
                        case ScienceSitutation.HighOrbit:
                            regionScalar = 1f; // No scalar effect
                            break;
                        default:
                            _Logger.LogWarning(
                                $"GetResearchLocationScalar: Unknown situation for region: {location.ScienceSituation}");
                            break;
                    }
                else
                    _Logger.LogWarning($"GetResearchLocationScalar: Unknown region {location.ScienceRegion}");
            }

            scienceScalar = bodyScalar * situationScalar * regionScalar * GetScienceDifficultyMultiplier();
        }
        else
        {
            _Logger.LogWarning($"No scalar found for {location.BodyName}");
            scienceScalar = 1f;
        }
    }
}