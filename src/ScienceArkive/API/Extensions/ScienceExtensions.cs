#nullable enable
using KSP.Game.Science;

namespace ScienceArkive.API.Extensions;

public static class ScienceExtensions
{
    /// <summary>
    /// We need to parse the `researchLocationId` string to get the ScienceSituation and CelestialBody,
    /// and eventually the ScienceRegionId.
    ///
    /// Example: `ResearchLocationId = "Kerbin_Surface"`
    /// Example: `ResearchLocationId = "Kerbin_Splashed_KerbinWater"`
    /// </summary>
    /// <param name="researchLocation"></param>
    /// <returns></returns>
    public static ResearchLocation? ParseResearchLocation(string researchLocationId)
    {
        var tokens = researchLocationId.Split('_');

        if (tokens.Length is < 1 or > 3)
        {
            ScienceArkivePlugin.Instance.SWLogger.LogError("Invalid researchLocationId: " + researchLocationId);
            return null;
        }

        if (!Enum.TryParse<ScienceSitutation>(tokens[1], out var scienceSituation)) return null;

        return new ResearchLocation(tokens.Length == 3, tokens[0], scienceSituation,
            tokens.Length == 3 ? tokens[2] : string.Empty);
    }

    /// <summary>
    /// Experiments provided by OrbitalSurvey mod are not using "ValidLocations" to check if the location is valid.
    /// So we need to implement the check here by hand, overriding the normal `IsLocationValid` method.
    /// </summary>
    public static bool IsArchiveLocationValid(this ExperimentDefinition exp, ResearchLocation location,
        out bool regionRequired)
    {
        if (exp.ExperimentID.StartsWith("orbital_survey_visual_mapping") ||
            exp.ExperimentID.StartsWith("orbital_survey_biome_mapping"))
        {
            regionRequired = false;
            return location.BodyName != "Kerbol" && location.ScienceSituation == ScienceSitutation.HighOrbit;
        }

        return exp.IsLocationValid(location, out regionRequired);
    }
}