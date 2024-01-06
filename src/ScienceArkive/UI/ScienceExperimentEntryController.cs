using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using SpaceWarp.API.Logging;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScienceArkive.UI
{
    public class ScienceExperimentEntryController
    {
        Label NameLabel;
        private SpaceWarp.API.Logging.ILogger logger;

        public ScienceExperimentEntryController(VisualElement visualElement)
        {
            logger = ScienceArkivePlugin.Instance.SWLogger;

            NameLabel = visualElement.Q<Label>("name-label");
        }

        public void BindExperiment(ExperimentDefinition experiment, CompletedResearchReport? report)
        {
            var gameInstance = GameManager.Instance.Game;
            var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
            var expId = experiment.ExperimentID;

            var location = experiment.ValidLocations[0];
            //var flavorText = dataStore.GetFlavorText(expId, report.ResearchLocationID, report.ResearchReportType);
            var displayName = dataStore.GetExperimentDisplayName(expId);

            var reportName = dataStore.GetExperimentReportName(expId, experiment.ExperimentType == ScienceExperimentType.DataType ? ScienceReportType.DataType : ScienceReportType.SampleType);
            NameLabel.text = LocalizationManager.GetTranslation(reportName);
            logger.LogInfo($"Bound experiment {report?.ExperimentID} {report}");
        }
    }
}
