using KSP.Game;
using KSP.Game.Science;
using KSP.Sim.impl;
using ScienceArkive.API.Extensions;
using ScienceArkive.Manager;
using ScienceArkive.Utils;
using UnityEngine.UIElements;

namespace ScienceArkive.UI.Components;

public class PlanetExperimentsDetailPanel
{
    private readonly ScrollView _detailScroll;
    private readonly Label _nameLabel;
    private readonly VisualElement _experimentsList;
    private readonly VisualElement _root;
    private CelestialBodyComponent? _celestialBody;

    public PlanetExperimentsDetailPanel(VisualElement root)
    {
        _root = root;
        _detailScroll = _root.Q<ScrollView>("detail-scroll");
        _detailScroll.verticalScroller.valueChanged += OnDetailScrollChange;

        _nameLabel = _root.Q<Label>("planet-name");
        _experimentsList = _root.Q<VisualElement>("experiments-container");

        _root.Q<Button>("toggle-collapse-button").RegisterCallback<ClickEvent>(_ => { ToggleCollapse(); });
    }

    public void ToggleCollapse(bool shouldCollapse = true)
    {
        foreach (var experimentEntry in _experimentsList.Children())
        {
            var controller = experimentEntry.userData as ExperimentSummary;
            controller?.ToggleCollapse(shouldCollapse);
        }
    }

    private static void OnDetailScrollChange(float value)
    {
        MainUIManager.Instance.ArchiveWindowController.detailScrollPosition = value;
    }

    public void BindPlanet(CelestialBodyComponent? celestialBody)
    {
        if (celestialBody == null) return;

        _celestialBody = celestialBody;

        var gameInstance = GameManager.Instance.Game;
        var scienceDataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
        // var allExperimentIds = scienceDataStore.GetAllExperimentIDs();
        // var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name, true);

        gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var completedReports);

        // Available experiments
        var displayedExperiments = ArchiveManager.Instance.GetExperimentDefinitions(Settings.ShowOnlyUnlockedExperiments
            .Value);
        var experiments = new List<ExperimentDefinition>();
        foreach (var experiment in displayedExperiments)
        foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
        {
            var researchLocation = new ResearchLocation(true, celestialBody.Name, situation, "");
            // This is not sufficient, we need to check if it's _possible_ to reach this location (es Kerbol_Splashed in invalid)
            var isLocationValid = experiment.IsLocationValid(researchLocation, out var regionRequired);
            var isFlavorPresent = isLocationValid && experiment.DataFlavorDescriptions.Any(flavor =>
                flavor.ResearchLocationID.StartsWith(researchLocation.ResearchLocationId));
            if (!isLocationValid || !isFlavorPresent) continue;

            experiments.Add(experiment);
            break;
        }

        // UI
        _experimentsList.Clear();
        _detailScroll.verticalScroller.value = MainUIManager.Instance.ArchiveWindowController.detailScrollPosition;

        var experimentTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentSummary.uxml");
        foreach (var experiment in experiments)
        {
            var experimentEntry = experimentTemplate.Instantiate();
            var experimentEntryController = new ExperimentSummary(experimentEntry);
            experimentEntryController.BindExperiment(experiment, celestialBody, completedReports);
            experimentEntry.userData = experimentEntryController;
            _experimentsList.Add(experimentEntry);
        }

        // UI Label
        _nameLabel.text = celestialBody.DisplayName;
    }
}