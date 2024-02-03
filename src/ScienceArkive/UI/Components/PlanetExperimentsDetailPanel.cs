using KSP.Game;
using KSP.Game.Science;
using KSP.Sim.impl;
using ScienceArkive.API.Extensions;
using ScienceArkive.Data;
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
    private readonly ProgressBar _progressBar;
    private readonly Button _collapseButton;
    private CelestialBodyComponent? _celestialBody;
    private Dictionary<string, bool> _visibleExperimentsIds = new();
    private VisualTreeAsset _planetExperimentTemplate;

    private float _sciencePoints = 0f;
    private float _maxSciencePoints = 0f;

    public PlanetExperimentsDetailPanel(VisualElement root)
    {
        _root = root;
        _detailScroll = _root.Q<ScrollView>("detail-scroll");
        _detailScroll.verticalScroller.valueChanged += OnDetailScrollChange;

        _nameLabel = _root.Q<Label>("planet-name");
        _experimentsList = _root.Q<VisualElement>("experiments-container");

        _progressBar = _root.Q<ProgressBar>("discover-progress");

        _collapseButton = _root.Q<Button>("toggle-collapse-button");
        _collapseButton.RegisterCallback<ClickEvent>(_ => { ToggleCollapse(); });

        _planetExperimentTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentSummary.uxml");
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

    private void ClearDiscoverProgress()
    {
        _sciencePoints = 0f;
        _maxSciencePoints = 0f;
        _progressBar.value = 0f;
        _progressBar.title = "0%";
    }

    public void UpdateDiscoverProgress(float potential, float scored)
    {
        _sciencePoints += scored;
        _maxSciencePoints += potential;
        _progressBar.value = _sciencePoints / _maxSciencePoints * 100;
        _progressBar.title = _progressBar.value.ToString("0.0") + "%";
    }

    public void BindPlanet(CelestialBodyComponent? celestialBody)
    {
        if (celestialBody == null) return;

        _celestialBody = celestialBody;
        ClearDiscoverProgress();

        var gameInstance = GameManager.Instance.Game;
        var scienceDataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;
        // var allExperimentIds = scienceDataStore.GetAllExperimentIDs();
        // var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name, true);

        if (!gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var completedReports))
            completedReports = [];

        // Available experiments
        var allExperiments = ArchiveManager.Instance.GetExperimentDefinitions();
        var experiments = new List<ExperimentDefinition>();
        foreach (var experiment in allExperiments)
        {
            if (ArchiveManager.Instance.ShouldSkipExperimentInCelestialBody(experiment, celestialBody.bodyName))
                continue;

            foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
            {
                //  we need to check if it's _possible_ to reach this location (es Kerbol_Splashed in invalid)
                if (!ArchiveManager.Instance.ExistsBodyScienceSituation(celestialBody, situation)) continue;

                var researchLocation = new ResearchLocation(true, celestialBody.Name, situation, "");
                // Then we need to check if the experiment is valid for this location
                var isLocationValid = experiment.IsLocationValid(researchLocation, out var regionRequired);
                if (!isLocationValid) continue;

                experiments.Add(experiment);
                break;
            }
        }

        // UI
        _experimentsList.Clear();
        _detailScroll.verticalScroller.value = MainUIManager.Instance.ArchiveWindowController.detailScrollPosition;

        foreach (var experiment in experiments)
        {
            var experimentEntry = _planetExperimentTemplate.Instantiate();
            var experimentEntryController = new ExperimentSummary(experimentEntry);
            experimentEntryController.BindExperiment(experiment, celestialBody, completedReports);
            experimentEntry.userData = experimentEntryController;
            experimentEntry.style.display = !Settings.ShowOnlyUnlockedExperiments.Value ||
                                            ArchiveManager.Instance.IsExperimentUnlocked(experiment.ExperimentID)
                ? DisplayStyle.Flex
                : DisplayStyle.None;
            _experimentsList.Add(experimentEntry);
        }

        // UI Label
        _nameLabel.text = celestialBody.DisplayName;

        Refresh();
    }

    /// <summary>
    /// Refreshes the UI to show only the experiments that are unlocked.
    /// If the body changes, we need to call BindPlanet() instead.
    /// </summary>
    public void Refresh()
    {
        ClearDiscoverProgress();

        var gameInstance = GameManager.Instance.Game;
        if (!gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var completedReports))
            completedReports = [];

        foreach (var experimentEntry in _experimentsList.Children())
        {
            if (experimentEntry.userData is not ExperimentSummary experimentEntryController) continue;

            var expId = experimentEntryController.ExperimentId;
            var isVisible = !Settings.ShowOnlyUnlockedExperiments.Value ||
                            ArchiveManager.Instance.IsExperimentUnlocked(expId);
            experimentEntry.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (isVisible) experimentEntryController.Refresh(completedReports);
        }
    }
}