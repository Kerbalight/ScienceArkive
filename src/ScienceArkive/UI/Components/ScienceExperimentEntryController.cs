using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using KSP.Sim.impl;
using ScienceArkive.UI.Components;
using ScienceArkive.UI.Loader;
using ScienceArkive.UI.Manager;
using SpaceWarp.API.Logging;
using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScienceArkive.UI
{
    public class ScienceExperimentEntryController
    {
        private Foldout foldout;
        private VisualElement content;

        private SpaceWarp.API.Logging.ILogger logger;

        public ScienceExperimentEntryController(VisualElement visualElement)
        {
            logger = ScienceArkivePlugin.Instance.SWLogger;

            foldout = visualElement.Q<Foldout>("foldout-experiment");
            //situationLabel = visualElement.Q<Label>("situation-label");
            content = visualElement.Q<VisualElement>("content");
        }

        public void BindExperiment(ExperimentDefinition experiment, CelestialBodyComponent celestialBody, List<CompletedResearchReport> reports)
        {
            var gameInstance = GameManager.Instance.Game;
            var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;

            var expId = experiment.ExperimentID;

            foldout.text = LocalizationManager.GetTranslation(dataStore.GetExperimentDisplayName(expId));

            var situationLabelTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentSituation.uxml");
            var regionEntryTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ScienceExperimentRegionEntry.uxml");

            foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
            {
                var researchLocation = new ResearchLocation(true, celestialBody.Name, situation, "");
                // This is not sufficient, we need to check if it's _possible_ to reach this location (es Kerbol_Splashed in invalid)
                var isLocationValid = experiment.IsLocationValid(researchLocation, out var regionRequired);
                var isFlavorPresent = isLocationValid && experiment.DataFlavorDescriptions.Any(flavor => flavor.ResearchLocationID.StartsWith(researchLocation.ResearchLocationId));
                if (!isLocationValid || !isFlavorPresent) continue;

                var situationLabel = situationLabelTemplate.Instantiate();
                situationLabel.Q<Label>("situation-label").text = "// " + situation.GetTranslatedDescription();
                content.Add(situationLabel);

                if (regionRequired)
                {
                    var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name);
                    foreach (var region in regions)
                    {
                        var regionEntry = regionEntryTemplate.Instantiate();
                        var regionController = new ScienceExperimentRegionEntryController(regionEntry);
                        var regionLocation = new ResearchLocation(true, celestialBody.Name, situation, region.Id);
                        
                        var regionReports = reports.Where(r => r.ResearchLocationID == regionLocation.ResearchLocationId);

                        regionController.Bind(experiment, regionLocation, regionReports);
                        content.Add(regionEntry);
                    }
                }
                else
                {
                    var regionEntry = regionEntryTemplate.Instantiate();
                    var regionController = new ScienceExperimentRegionEntryController(regionEntry);
                    regionController.Bind(experiment, researchLocation, reports.Where(r => r.ResearchLocationID == researchLocation.ResearchLocationId));
                    content.Add(regionEntry);
                }
            }
        }
    }
}
