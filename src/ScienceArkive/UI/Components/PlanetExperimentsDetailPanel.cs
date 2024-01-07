using KSP.Game;
using KSP.Game.Science;
using KSP.Sim.impl;
using ScienceArkive.API.Extensions;
using ScienceArkive.Manager;
using UnityEngine.UIElements;

namespace ScienceArkive.UI.Components
{
    public class PlanetExperimentsDetailPanel
    {
        VisualElement _root;
        VisualElement _detailScroll;
        VisualElement _experimentsList;

        public PlanetExperimentsDetailPanel(VisualElement root)
        {
            _root = root;
            _detailScroll = _root.Q<VisualElement>("detail-scroll");
            _detailScroll.StopWheelEventPropagation();

            _experimentsList = _root.Q<VisualElement>("experiments-container");

            _root.Q<Button>("toggle-collapse-button").RegisterCallback<ClickEvent>(_ =>
            {
                ToggleCollapse();
            });
        }

        public void ToggleCollapse(bool shouldCollapse = true)
        {
            foreach (var experimentEntry in _experimentsList.Children())
            {
                var controller = experimentEntry.userData as ExperimentSummary;
                controller.ToggleCollapse(shouldCollapse);
            }
        }

        public void BindPlanet(CelestialBodyComponent celestialBody)
        {
            var gameInstance = GameManager.Instance.Game;
            var scienceDataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
            var allExperimentIds = scienceDataStore.GetAllExperimentIDs();
            var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name);

            gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var completedReports);

            // Available experiments
            var experiments = new List<ExperimentDefinition>();
            foreach (var expId in allExperimentIds)
            {
                var experiment = scienceDataStore.GetExperimentDefinition(expId);                
                foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
                {
                    var researchLocation = new ResearchLocation(true, celestialBody.Name, situation, "");
                    // This is not sufficient, we need to check if it's _possible_ to reach this location (es Kerbol_Splashed in invalid)
                    var isLocationValid = experiment.IsLocationValid(researchLocation, out var regionRequired);
                    var isFlavorPresent = isLocationValid && experiment.DataFlavorDescriptions.Any(flavor => flavor.ResearchLocationID.StartsWith(researchLocation.ResearchLocationId));
                    if (isLocationValid && isFlavorPresent)
                    {
                        experiments.Add(experiment);
                        break;
                    }
                }
            }

            // UI
            _experimentsList.Clear();

            var experimentTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentSummary.uxml");
            foreach (var experiment in experiments)
            {
                var experimentEntry = experimentTemplate.Instantiate();
                var experimentEntryController = new ExperimentSummary(experimentEntry);
                experimentEntryController.BindExperiment(experiment, celestialBody, completedReports);
                experimentEntry.userData = experimentEntryController;
                _experimentsList.Add(experimentEntry);
            }
            
        }
    }
}
