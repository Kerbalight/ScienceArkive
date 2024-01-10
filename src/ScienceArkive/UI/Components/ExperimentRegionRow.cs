﻿using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using ScienceArkive.Manager;
using ScienceArkive.UI.Loader;
using UnityEngine;
using UnityEngine.UIElements;
using ILogger = SpaceWarp.API.Logging.ILogger;

namespace ScienceArkive.UI;

public class ExperimentRegionRow
{
    private ExperimentDefinition _experiment = null!;
    private ResearchLocation _location = null!;
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
        ArchiveManager.Instance.GetResearchLocationScalar(_location, out var scienceScalar);
        return scienceScalar * _experiment.SampleValue;
    }

    private float GetDataValue()
    {
        ArchiveManager.Instance.GetResearchLocationScalar(_location, out var scienceScalar);
        return scienceScalar * _experiment.DataValue;
    }

    public void Bind(ExperimentDefinition experiment, ResearchLocation location,
        IEnumerable<CompletedResearchReport> regionReports)
    {
        _experiment = experiment;
        _location = location;

        var gameInstance = GameManager.Instance.Game;
        var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
        var expId = experiment.ExperimentID;

        //var flavorText = dataStore.GetFlavorText(expId, report.ResearchLocationID, report.ResearchReportType);
        var displayName = dataStore.GetExperimentDisplayName(expId);
        var requirements = dataStore.GetExperimentDisplayRequirements(expId);

        var reportName = dataStore.GetExperimentReportName(expId,
            experiment.ExperimentType == ScienceExperimentType.DataType
                ? ScienceReportType.DataType
                : ScienceReportType.SampleType);
        nameLabel.text = LocalizationManager.GetTranslation(displayName);
        regionLabel.text = ScienceRegionsHelper.GetRegionDisplayName(location.ScienceRegion);
        regionLabel.tooltip = LocalizationManager.GetTranslation(requirements);

        // Sample
        if (experiment.ExperimentType == ScienceExperimentType.SampleType ||
            experiment.ExperimentType == ScienceExperimentType.Both)
        {
            sampleContainer.style.visibility = Visibility.Visible;
            var sampleReport = regionReports.Where(r => r.ResearchReportType == ScienceReportType.SampleType)
                .Cast<CompletedResearchReport?>().FirstOrDefault();
            sampleIcon.style.unityBackgroundImageTintColor = sampleReport == null ? Color.white : Color.cyan;
            sampleCheck.style.visibility = sampleReport == null ? Visibility.Hidden : Visibility.Visible;
            sampleScienceLabel.text = GetSampleValue().ToString("0.00");
        }
        else
        {
            sampleCheck.style.visibility = Visibility.Hidden;
            sampleContainer.style.visibility = Visibility.Hidden;
        }

        // Data
        if (experiment.ExperimentType == ScienceExperimentType.DataType ||
            experiment.ExperimentType == ScienceExperimentType.Both)
        {
            dataContainer.style.visibility = Visibility.Visible;
            var dataReport = regionReports.Where(r => r.ResearchReportType == ScienceReportType.DataType)
                .Cast<CompletedResearchReport?>().FirstOrDefault();
            dataIcon.style.unityBackgroundImageTintColor = dataReport == null ? Color.white : Color.cyan;
            dataCheck.style.visibility = dataReport == null ? Visibility.Hidden : Visibility.Visible;
            dataScienceLabel.text = GetDataValue().ToString("0.00");

            if (dataReport.HasValue && dataReport.Value.FinalScienceValue != GetDataValue())
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