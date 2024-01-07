using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using ScienceArkive.UI.Loader;
using SpaceWarp.API.Logging;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScienceArkive.UI
{
    public class ScienceExperimentRegionEntryController
    {
        Label NameLabel;
        private VisualElement sampleIcon;
        private VisualElement dataIcon;
        private Label regionLabel;
        private SpaceWarp.API.Logging.ILogger logger;

        public ScienceExperimentRegionEntryController(VisualElement visualElement)
        {
            logger = ScienceArkivePlugin.Instance.SWLogger;

            NameLabel = visualElement.Q<Label>("name-label");

            sampleIcon = visualElement.Q<VisualElement>("sample-icon");
            dataIcon = visualElement.Q<VisualElement>("data-icon");
            regionLabel = visualElement.Q<Label>("region-label");

            sampleIcon.style.backgroundImage = new StyleBackground(AssetsPatchedLoader.Instance.SampleIcon.texture);
            dataIcon.style.backgroundImage = new StyleBackground(AssetsPatchedLoader.Instance.DataIcon.texture);
        }

        public void Bind(ExperimentDefinition experiment, ResearchLocation location, IEnumerable<CompletedResearchReport> regionReports)
        {
            var gameInstance = GameManager.Instance.Game;
            var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
            var expId = experiment.ExperimentID;
            
            //var flavorText = dataStore.GetFlavorText(expId, report.ResearchLocationID, report.ResearchReportType);
            var displayName = dataStore.GetExperimentDisplayName(expId);

            var reportName = dataStore.GetExperimentReportName(expId, experiment.ExperimentType == ScienceExperimentType.DataType ? ScienceReportType.DataType : ScienceReportType.SampleType);
            NameLabel.text = LocalizationManager.GetTranslation(displayName);
               
            var sampleReport = regionReports.Where(r => r.ResearchReportType == ScienceReportType.SampleType).Cast<CompletedResearchReport?>().FirstOrDefault();
            sampleIcon.style.unityBackgroundImageTintColor = sampleReport == null ? Color.white : Color.cyan;

            var dataReport = regionReports.Where(r => r.ResearchReportType == ScienceReportType.DataType).Cast<CompletedResearchReport?>().FirstOrDefault();
            dataIcon.style.unityBackgroundImageTintColor = dataReport == null ? Color.white : Color.cyan;
            regionLabel.text = ScienceRegionsHelper.GetRegionDisplayName(location.ScienceRegion);
            
            //logger.LogInfo($"Bound experiment {report?.ExperimentID} {report}");
        }
    }
}
