using System.Reflection;
using BepInEx.Logging;
using KSP.Game;
using KSP.Game.Science;
using KSP.Messages;
using ScienceArkive.UI.Loader;

namespace ScienceArkive.Manager;

public class ArchiveManager
{
    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.ArchiveManager");

    public static ArchiveManager Instance { get; } = new();

    public bool IsInitialized { get; private set; }
    public Dictionary<string, CelestialBodyScienceRegionsData> CelestialBodiesScienceData { get; private set; } = new();

    /// <summary>
    ///     When the game loads, build a cache of all the science regions for each celestial body.
    ///     We are doing it here since we are using the private ScienceRegionsDataProvider class.
    /// </summary>
    public void InitializeScienceRegionsCache()
    {
        var dataProvider = GameManager.Instance.Game?.ScienceManager?.ScienceRegionsDataProvider;
        if (dataProvider == null)
        {
            _Logger.LogError("No ScienceRegionsDataProvider found, skipping");
            return;
        }

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
        IsInitialized = true;
    }

    public ScienceRegionDefinition[] GetRegionsForBody(string bodyName)
    {
        if (CelestialBodiesScienceData.TryGetValue(bodyName, out var scienceData)) return scienceData.Regions;

        _Logger.LogWarning($"No regions found for {bodyName}");
        return new ScienceRegionDefinition[0];
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