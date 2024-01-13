using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using ScienceArkive.Manager;

namespace ScienceArkive.Data;

public struct ResearchReportDisplayBag
{
    public string DisplayName { get; set; }

    public float ScienceValue { get; set; }

    public string CelestialBodyName { get; set; }
    public string ResearchLocationName { get; set; }
    public ScienceReportType ReportType { get; set; }


    public ResearchReportDisplayBag(ResearchReport report)
    {
        var dataStore = GameManager.Instance.Game.ScienceManager.ScienceExperimentsDataStore;
        if (dataStore == null)
            throw new Exception(
                $"Failed to get experiment definition for {report.ExperimentID}, ScienceExperimentsDataStore is null");

        DisplayName = LocalizationManager.GetTranslation(dataStore.GetExperimentReportName(
            report.ExperimentID,
            report.ResearchReportType));

        CelestialBodyName = report.Location.BodyName;

        ResearchLocationName = "<color=#E7CA76>" + report.Location.ScienceSituation.GetTranslatedDescription() +
                               "</color>";

        ReportType = report.ResearchReportType;

        if (!string.IsNullOrEmpty(report.Location.ScienceRegion))
            ResearchLocationName += " / <color=#E7CA76>" +
                                    ScienceRegionsHelper.GetRegionDisplayName(report.Location.ScienceRegion) +
                                    "</color>";

        // This is the finalScienceValue, not the potential value. It's called potential I suppose
        // because it's the value before the difficulty multiplier is applied.
        ScienceValue = GameManager.Instance.Game.ScienceManager.GetPotentialReportValueBase(report) *
                       ArchiveManager.GetScienceDifficultyMultiplier();
    }
}