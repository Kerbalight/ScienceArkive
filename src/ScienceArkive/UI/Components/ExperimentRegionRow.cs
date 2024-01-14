using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using ScienceArkive.Manager;
using ScienceArkive.UI.Loader;
using ScienceArkive.Utils;
using UnityEngine;
using UnityEngine.UIElements;
using ILogger = SpaceWarp.API.Logging.ILogger;

namespace ScienceArkive.UI;

public class ExperimentRegionRow
{
    public ExperimentDefinition Experiment { get; private set; } = null!;
    public ResearchLocation Location { get; private set; } = null!;
    public bool InRelatedContext { get; set; }

    private readonly VisualElement dataCheck;

    private readonly VisualElement dataContainer;
    private readonly VisualElement dataIcon;
    private readonly Label dataScienceLabel;
    private readonly ILogger logger;
    private readonly Label nameLabel;
    private readonly Label regionLabel;
    private readonly VisualElement sampleCheck;
    private readonly VisualElement sampleContainer;
    private readonly VisualElement sampleIcon;
    private readonly Label sampleScienceLabel;

    public ExperimentRegionRow(VisualElement visualElement)
    {
        logger = ScienceArkivePlugin.Instance.SWLogger;

        nameLabel = visualElement.Q<Label>("name-label"); // Currently hidden
        regionLabel = visualElement.Q<Label>("region-label");

        sampleContainer = visualElement.Q<VisualElement>("sample");
        sampleIcon = visualElement.Q<VisualElement>("sample-icon");
        sampleCheck = visualElement.Q<VisualElement>("sample-done-icon");
        sampleScienceLabel = visualElement.Q<Label>("sample-science");

        dataContainer = visualElement.Q<VisualElement>("data");
        dataIcon = visualElement.Q<VisualElement>("data-icon");
        dataScienceLabel = visualElement.Q<Label>("data-science");
        dataCheck = visualElement.Q<VisualElement>("data-done-icon");

        sampleIcon.style.backgroundImage = new StyleBackground(ExistingAssetsLoader.Instance.SampleIcon);
        dataIcon.style.backgroundImage = new StyleBackground(ExistingAssetsLoader.Instance.DataIcon);
        sampleCheck.style.backgroundImage = new StyleBackground(ExistingAssetsLoader.Instance.CheckIcon);
        dataCheck.style.backgroundImage = new StyleBackground(ExistingAssetsLoader.Instance.CheckIcon);
    }

    private float GetSampleValue()
    {
        ArchiveManager.Instance.GetResearchLocationScalar(Location, out var scienceScalar);
        return scienceScalar * Experiment.SampleValue;
    }

    private float GetDataValue()
    {
        ArchiveManager.Instance.GetResearchLocationScalar(Location, out var scienceScalar);
        return scienceScalar * Experiment.DataValue;
    }

    public void Bind(ExperimentDefinition experiment, ResearchLocation location,
        IEnumerable<CompletedResearchReport> allReports)
    {
        Experiment = experiment;
        Location = location;

        var regionReports =
            ArchiveManager.GetRegionAndExperimentReports(allReports, Location, Experiment.ExperimentID);

        var gameInstance = GameManager.Instance.Game;
        var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
        var expId = experiment.ExperimentID;

        //var flavorText = dataStore.GetFlavorText(expId, report.ResearchLocationID, report.ResearchReportType);
        var displayName = dataStore.GetExperimentDisplayName(expId);
        var requirements = dataStore.GetExperimentDisplayRequirements(expId);

        // The region is visible if the region is visibile or anyway if atleast one report is present
        // We need this second check because TravelFirst is not updated when we are here
        var isRegionVisible = ArchiveManager.Instance.IsRegionVisible(location.BodyName, location.ScienceRegion,
            out var isRegionDiscoverable) || regionReports.Any();

        var reportName = dataStore.GetExperimentReportName(expId,
            experiment.ExperimentType == ScienceExperimentType.DataType
                ? ScienceReportType.DataType
                : ScienceReportType.SampleType);
        nameLabel.text = LocalizationManager.GetTranslation(displayName);

        regionLabel.text = !isRegionVisible && isRegionDiscoverable &&
                           Settings.DiscoverablesDisplay.Value == Settings.DiscoverablesDisplayMode.Censored
            ? "???"
            : ScienceRegionsHelper.GetRegionDisplayName(location.ScienceRegion);
        regionLabel.tooltip = LocalizationManager.GetTranslation(requirements);

        // Related experiments - add data report name to the region to differentiate
        if (InRelatedContext)
        {
            var dataReportName = dataStore.GetExperimentReportName(expId, ScienceReportType.DataType);
            regionLabel.text += " " + LocalizationManager.GetTranslation(dataReportName);
        }

        // Sample
        var completedResearchReports = regionReports as CompletedResearchReport[] ?? regionReports.ToArray();

        if (experiment.ExperimentType is ScienceExperimentType.SampleType or ScienceExperimentType.Both)
        {
            sampleContainer.style.visibility = Visibility.Visible;
            var sampleReport = completedResearchReports.Where(r => r.ResearchReportType == ScienceReportType.SampleType)
                .Cast<CompletedResearchReport?>().FirstOrDefault();
            sampleIcon.style.unityBackgroundImageTintColor = sampleReport == null ? Color.white : Color.cyan;
            sampleCheck.style.visibility = sampleReport == null ? Visibility.Hidden : Visibility.Visible;

            var sampleValue = GetSampleValue();
            sampleScienceLabel.text = sampleValue.ToString("0.00");

            MainUIManager.Instance.ArchiveWindowController.PlanetExperimentsDetail.UpdateDiscoverProgress(sampleValue,
                sampleReport?.FinalScienceValue ?? 0f);
        }
        else
        {
            sampleCheck.style.visibility = Visibility.Hidden;
            sampleContainer.style.visibility = Visibility.Hidden;
        }

        // Data
        if (experiment.ExperimentType is ScienceExperimentType.DataType or ScienceExperimentType.Both)
        {
            dataContainer.style.visibility = Visibility.Visible;
            var dataReport = completedResearchReports.Where(r => r.ResearchReportType == ScienceReportType.DataType)
                .Cast<CompletedResearchReport?>().FirstOrDefault();
            dataIcon.style.unityBackgroundImageTintColor = dataReport == null ? Color.white : Color.cyan;
            dataCheck.style.visibility = dataReport == null ? Visibility.Hidden : Visibility.Visible;

            var dataValue = GetDataValue();
            dataScienceLabel.text = dataValue.ToString("0.00");

            MainUIManager.Instance.ArchiveWindowController.PlanetExperimentsDetail.UpdateDiscoverProgress(dataValue,
                dataReport?.FinalScienceValue ?? 0f);

            if (dataReport.HasValue && Math.Abs(dataReport.Value.FinalScienceValue - GetDataValue()) > 1E-5f)
                logger.LogWarning(
                    $"Science value mismatch for {reportName} ({dataReport.Value.ResearchLocationID}): {dataReport?.FinalScienceValue} != {GetDataValue()}");
        }
        else
        {
            dataCheck.style.visibility = Visibility.Hidden;
            dataContainer.style.visibility = Visibility.Hidden;
        }


        //logger.LogInfo($"Bound experiment {report?.ExperimentID} {report}");
    }
}