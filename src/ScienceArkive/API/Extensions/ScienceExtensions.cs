#nullable enable
using KSP.Game.Science;

namespace ScienceArkive.API.Extensions;

public class ScienceExtensions
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
}