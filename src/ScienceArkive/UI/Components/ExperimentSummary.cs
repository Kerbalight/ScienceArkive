using I2.Loc;
using KSP.Game;
using KSP.Game.Science;
using KSP.Sim.impl;
using ScienceArkive.Manager;
using ScienceArkive.UI.Components;
using ScienceArkive.Utils;
using SpaceWarp.API.Logging;
using UnityEngine.UIElements;

namespace ScienceArkive.UI;

public class ExperimentSummary
{
    private readonly VisualElement content;
    private readonly Foldout foldout;
    private string _celestialBodyName = "";

    public string ExperimentId { get; private set; } = null!;

    private ILogger logger;

    public ExperimentSummary(VisualElement visualElement)
    {
        logger = ScienceArkivePlugin.Instance.SWLogger;

        foldout = visualElement.Q<Foldout>("foldout-experiment");
        foldout.RegisterValueChangedCallback(OnFoldoutChange);
        content = visualElement.Q<VisualElement>("content");
    }

    public void ToggleCollapse(bool shouldCollapse = true)
    {
        foldout.value = !shouldCollapse;
    }

    private void OnFoldoutChange(ChangeEvent<bool> evt)
    {
        MainUIManager.Instance.ArchiveWindowController.CollapsedExperiments[ExperimentId] = evt.newValue;
    }

    public void BindExperiment(ExperimentDefinition experiment, CelestialBodyComponent celestialBody,
        List<CompletedResearchReport> reports)
    {
        var gameInstance = GameManager.Instance.Game;
        var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;

        var expId = experiment.ExperimentID;
        ExperimentId = expId;
        _celestialBodyName = celestialBody.Name;

        foldout.text = LocalizationManager.GetTranslation(dataStore.GetExperimentDisplayName(expId));
        if (MainUIManager.Instance.ArchiveWindowController.CollapsedExperiments.TryGetValue(ExperimentId,
                out var isFolded))
            foldout.value = isFolded;

        var situationLabelTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentSituationLabel.uxml");
        var regionEntryTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentRegionRow.uxml");

        var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name).ToArray();

        foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
        {
            // we need to check if it's _possible_ to reach this location (es Kerbol_Splashed in invalid)
            if (!ArchiveManager.Instance.ExistsBodyScienceSituation(celestialBody, situation)) continue;

            // Then we need to check if the experiment is valid for this location
            var researchLocation = new ResearchLocation(false, celestialBody.Name, situation, "");
            var isLocationValid = experiment.IsLocationValid(researchLocation, out var regionRequired);
            if (!isLocationValid) continue;

            var situationLabel = situationLabelTemplate.Instantiate();
            situationLabel.Q<Label>("situation-label").text =
                "// <color=#E7CA76>" + situation.GetTranslatedDescription().ToUpper() + "</color>";
            content.Add(situationLabel);

            if (regionRequired)
            {
                foreach (var region in regions)
                {
                    var regionEntry = regionEntryTemplate.Instantiate();
                    var regionController = new ExperimentRegionRow(regionEntry);
                    regionEntry.userData = regionController;
                    var regionResearchLocation = new ResearchLocation(true, celestialBody.Name, situation, region.Id);

                    // We previously checked the DataFlavorDescriptions, since we only want to show locations which has been validated by devs, but it appears sometimes they're missing
                    // So we use the ResearchLocationScalar, since we want to omit negative values (es. Kerbin_Beach_Splashed: technically _there is a flavor text_, even if the science is negative)
                    // We should probably double check how KSP2 does it by itself
                    ArchiveManager.Instance.GetResearchLocationScalar(regionResearchLocation, out var scienceScalar);
                    if (scienceScalar < 0f) continue;

                    regionController.Bind(experiment, regionResearchLocation, reports);
                    content.Add(regionEntry);
                }
            }
            else
            {
                var regionEntry = regionEntryTemplate.Instantiate();
                var regionController = new ExperimentRegionRow(regionEntry);
                regionEntry.userData = regionController;
                regionController.Bind(experiment, researchLocation, reports);
                content.Add(regionEntry);

                // Related experiments. Used only for Orbital Survey (25%, 50%, 75%, 100%)
                // TODO This is supported only for NO region required. We could support it for region required too, but there is no experiment which requires it
                if (ArchiveManager.Instance.RelatedExperiments.TryGetValue(ExperimentId, out var relatedExperiments))
                {
                    regionController.InRelatedContext = true;

                    foreach (var related in relatedExperiments)
                    {
                        var relatedEntry = regionEntryTemplate.Instantiate();
                        var relatedController = new ExperimentRegionRow(relatedEntry);
                        relatedEntry.userData = relatedController;

                        relatedController.InRelatedContext = true;
                        relatedController.Bind(related, researchLocation, reports);
                        content.Add(relatedEntry);
                    }
                }
            }
        }

        Refresh(reports);
    }

    public void Refresh(List<CompletedResearchReport> reports)
    {
        var visibleRegions = ArchiveManager.Instance.GetRegionsForBody(_celestialBodyName,
            Settings.DiscoverablesDisplay.Value == Settings.DiscoverablesDisplayMode.Discovered).ToArray();

        foreach (var regionEntry in content.Children())
        {
            if (regionEntry.userData is not ExperimentRegionRow regionController) continue;
            var isVisible = string.IsNullOrEmpty(regionController.Location.ScienceRegion) ||
                            visibleRegions.Any(r => r.Id == regionController.Location.ScienceRegion);
            regionEntry.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;

            if (!isVisible) continue;


            regionController.Bind(regionController.Experiment, regionController.Location, reports);
        }
    }
}