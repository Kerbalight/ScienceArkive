using System.Reflection;
using BepInEx.Logging;
using KSP.Game;
using KSP.Game.Science;
using KSP.Messages;
using KSP.Modules;
using KSP.Sim;
using KSP.Sim.impl;
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

    /// <summary>
    /// We keep track of the bodies which have been discovered by the player.
    /// </summary>
    public HashSet<string> DiscoveredBodies { get; set; } = [];

    // Old experiment ID, just ignore it
    private const string OldCrewReportExperimentId = "CrewReport";

    // Discoverables which are present in the game but are Hidden
    private HashSet<string> HiddenDiscoverableRegionsIds { get; } = ["KerbinGlacier"];

    /// <summary>
    /// Dictionary of experiments which should be shown together in the same `ExperimentSummary` component.
    /// This is used to group experiments which are similar, like Orbital Survey which provides
    /// 25%, 50% and 100% experiments. (it's the same experiment, but triggered at different percentages
    /// of scan completion).
    /// </summary>
    public Dictionary<string, List<ExperimentDefinition>> RelatedExperiments { get; } = new();


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
            "SurfaceSurvey"
            // "CrewReport" // Old one
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

        _Logger.LogInfo("Found " + _unlockedExperimentsIds.Count + " unlocked experiments");

        // Group related experiments together
        RelatedExperiments.Clear();
        var visitedExperimentNames = new Dictionary<string, string>();
        foreach (var experimentId in allExperimentIds)
        {
            var definition = experimentDataStore.GetExperimentDefinition(experimentId);
            if (visitedExperimentNames.TryGetValue(definition.DisplayName, out var parentExpId))
            {
                if (!RelatedExperiments.ContainsKey(parentExpId))
                    RelatedExperiments.Add(parentExpId, []);

                RelatedExperiments[parentExpId].Add(definition);
            }
            else
            {
                visitedExperimentNames.Add(definition.DisplayName, experimentId);
            }
        }

        _Logger.LogInfo("Found " + RelatedExperiments.Count + " related experiments");
    }

    /// <summary>
    /// List of celestial bodies which have been reached by the player.
    /// </summary>
    /// <returns></returns>
    public IEnumerable<string> GetCelestialBodiesNames(bool onlyDiscovered = false)
    {
        return CelestialBodiesScienceData.Keys.Where(cb =>
            !onlyDiscovered || _firsts.SOIReached.ContainsKey(cb) || DiscoveredBodies.Contains(cb));
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

    /// <summary>
    /// Checks if a Celestial Body permits the specified science situation.
    /// </summary>
    public bool ExistsBodyScienceSituation(CelestialBodyComponent body, ScienceSitutation situation)
    {
        switch (situation)
        {
            case ScienceSitutation.HighOrbit:
            case ScienceSitutation.LowOrbit:
                return true;

            case ScienceSitutation.Atmosphere:
                return body.hasAtmosphere;

            case ScienceSitutation.Splashed:
                return body.hasOcean;

            case ScienceSitutation.Landed:
                return body.hasSolidSurface;

            case ScienceSitutation.None:
            default:
                return false;
        }
    }

    public bool IsRegionVisible(string bodyName, string regionId, out bool isDiscoverable)
    {
        var scienceRegionsDataProvider = GameManager.Instance.Game.ScienceManager.ScienceRegionsDataProvider;
        isDiscoverable = scienceRegionsDataProvider.IsRegionADiscoverable(bodyName, regionId);
        return !isDiscoverable ||
               _firsts.DiscoverableReached.ContainsKey($"{bodyName}_{regionId}");
    }

    /// <summary>
    /// Some experiments are not available in certain locations. We cannot use `IsLocationValid` since it
    /// doesn't check for discoverables.
    /// </summary>
    public bool ShouldSkipExperimentInResearchLocation(ExperimentDefinition definition, ResearchLocation location)
    {
        var scienceRegionsDataProvider = GameManager.Instance.Game.ScienceManager.ScienceRegionsDataProvider;
        var isDiscoverable = !string.IsNullOrEmpty(location.ScienceRegion) &&
                             scienceRegionsDataProvider.IsRegionADiscoverable(location.BodyName,
                                 location.ScienceRegion);

        // No way to take experiments in "Low Orbit" on a discoverable
        if (location.ScienceSituation == ScienceSitutation.LowOrbit && isDiscoverable)
            return true;

        // "KerbinGlacier"
        if (HiddenDiscoverableRegionsIds.Contains(location.ScienceRegion))
            return true;

        return false;
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
        {
            if (experimentId == OldCrewReportExperimentId) continue;

            var definition = experimentDataStore.GetExperimentDefinition(experimentId);
            if (experimentDefinitions.Any(d => d.DisplayName == definition.DisplayName))
                // Related experiments are grouped together in the same `ExperimentSummary` component.
                continue;

            experimentDefinitions.Add(definition);
        }

        return experimentDefinitions;
    }

    public static float GetScienceDifficultyMultiplier()
    {
        if (!SciencePartsHandler.Instance.IsGameModeSciencePointsFeatureEnabled()) return 1f;

        var sessionManager = GameManager.Instance.Game.SessionManager;
        if (!sessionManager.TryGetDifficultyOptionState<float>("ScienceRewards",
                out var scienceMultiplier)) scienceMultiplier = 1f;

        return scienceMultiplier;
    }

    public static IEnumerable<CompletedResearchReport> GetRegionAndExperimentReports(
        IEnumerable<CompletedResearchReport>? allReports, ResearchLocation location, string experimentId)
    {
        var reports = new List<CompletedResearchReport>();
        if (allReports == null) return reports;

        foreach (var report in allReports)
            if (report.ResearchLocationID == location.ResearchLocationId && report.ExperimentID == experimentId)
                reports.Add(report);

        return reports;
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