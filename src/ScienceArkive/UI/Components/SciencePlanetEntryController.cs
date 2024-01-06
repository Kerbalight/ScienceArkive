using KSP.Game;
using KSP.Game.Science;
using KSP.Sim.impl;
using ScienceArkive.UI.Manager;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.UIElements;

namespace ScienceArkive.UI.Components
{
    public class SciencePlanetEntryController
    {
        VisualElement _root;
        Foldout _foldout;
        ListView _experimentsList;

        public SciencePlanetEntryController(VisualElement root)
        {
            _root = root;
            _foldout = _root.Q<Foldout>("planet-foldout");
            _experimentsList = _root.Q<VisualElement>("experiments-container");
        }

        public void BindPlanet(CelestialBodyComponent celestialBody)
        {
            //var foldoutLabelContainer = _foldout.Q<VisualElement>(".unity-foldout__input");
            //foldoutLabelContainer.Insert(1, )
            _foldout.text = celestialBody.DisplayName;

            // Available experiments
            var gameInstance = GameManager.Instance.Game;
            var scienceDataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
            var regionsDataProvider = gameInstance.ScienceManager.ScienceRegionsDataProvider;
            var allExperimentIds = scienceDataStore.GetAllExperimentIDs();
            var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name);

            gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var completedReports);

            
            var experiments = new List<ExperimentDefinition>();
            foreach (var expId in allExperimentIds)
            {
                var experiment = scienceDataStore.GetExperimentDefinition(expId);                
                foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
                {
                    var researchLocation = new ResearchLocation(true, celestialBody.Name, situation, "");
                    if (experiment.IsLocationValid(researchLocation, out var regionRequired))
                    {
                        experiments.Add(experiment);
                    }
                }
            }

            // UI
            var experimentTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ScienceExperimentEntry.uxml");
            _experimentsList.itemsSource = experiments;
            _experimentsList.makeItem = () =>
            {
                var experimentTemplateInstance = experimentTemplate.Instantiate();
                experimentTemplateInstance.userData = new ScienceExperimentEntryController(experimentTemplateInstance);
                return experimentTemplateInstance;
            };
            _experimentsList.bindItem = (element, index) =>
            {
                var experimentEntryController = (ScienceExperimentEntryController)element.userData;
                var experiment = experiments[index];
                CompletedResearchReport? completedReport = completedReports.Find(report => report.ExperimentID == experiment.ExperimentID);
                experimentEntryController.BindExperiment(experiment, completedReport);
            };
        }

        private void GetAvailableExperiments()
        {

        }
    }
}
