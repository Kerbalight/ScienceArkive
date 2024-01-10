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

    private ILogger logger;

    public ExperimentSummary(VisualElement visualElement)
    {
        logger = ScienceArkivePlugin.Instance.SWLogger;

        foldout = visualElement.Q<Foldout>("foldout-experiment");
        content = visualElement.Q<VisualElement>("content");
    }

    public void ToggleCollapse(bool shouldCollapse = true)
    {
        foldout.value = !shouldCollapse;
    }

    public void BindExperiment(ExperimentDefinition experiment, CelestialBodyComponent celestialBody,
        List<CompletedResearchReport> reports)
    {
        var gameInstance = GameManager.Instance.Game;
        var dataStore = gameInstance.ScienceManager.ScienceExperimentsDataStore;

        var expId = experiment.ExperimentID;

        foldout.text = LocalizationManager.GetTranslation(dataStore.GetExperimentDisplayName(expId));

        var situationLabelTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentSituationLabel.uxml");
        var regionEntryTemplate = UIToolkitElement.Load("ScienceArchiveWindow/ExperimentRegionRow.uxml");

        foreach (ScienceSitutation situation in Enum.GetValues(typeof(ScienceSitutation)))
        {
            var researchLocation = new ResearchLocation(false, celestialBody.Name, situation, "");
            // This is not sufficient, we need to check if it's _possible_ to reach this location (es Kerbol_Splashed in invalid)
            var isLocationValid = experiment.IsLocationValid(researchLocation, out var regionRequired);
            var isFlavorPresent = isLocationValid && experiment.DataFlavorDescriptions.Any(flavor =>
                flavor.ResearchLocationID.StartsWith(researchLocation.ResearchLocationId));
            if (!isLocationValid || !isFlavorPresent) continue;

            var situationLabel = situationLabelTemplate.Instantiate();
            situationLabel.Q<Label>("situation-label").text =
                "// <color=#E7CA76>" + situation.GetTranslatedDescription().ToUpper() + "</color>";
            content.Add(situationLabel);

            if (regionRequired)
            {
                var regions = ArchiveManager.Instance.GetRegionsForBody(celestialBody.Name,
                    Settings.DiscoverablesDisplay.Value == Settings.DiscoverablesDisplayMode.Discovered);
                foreach (var region in regions)
                {
                    var regionEntry = regionEntryTemplate.Instantiate();
                    var regionController = new ExperimentRegionRow(regionEntry);
                    var regionResearchLocation = new ResearchLocation(true, celestialBody.Name, situation, region.Id);

                    // This double check is not the cleanest. We use:
                    // - the DataFlavorDescriptions, since we only want to show locations which has been validated by devs
                    // - the ResearchLocationScalar, since we want to omit negative values (es. Kerbin_Beach_Splashed: technically _there is a flavor text_, even if the science is negative)
                    // We should probably double check how KSP2 does it by itself
                    ArchiveManager.Instance.GetResearchLocationScalar(regionResearchLocation, out var scienceScalar);
                    if (scienceScalar < 0f) continue;
                    var isRegionFlavorPresent = experiment.DataFlavorDescriptions.Any(flavor =>
                        flavor.ResearchLocationID == regionResearchLocation.ResearchLocationId);
                    if (!isRegionFlavorPresent) continue;

                    var regionReports = reports.Where(r =>
                        r.ResearchLocationID == regionResearchLocation.ResearchLocationId && r.ExperimentID == expId);

                    regionController.Bind(experiment, regionResearchLocation, regionReports);
                    content.Add(regionEntry);
                }
            }
            else
            {
                var regionEntry = regionEntryTemplate.Instantiate();
                var regionController = new ExperimentRegionRow(regionEntry);
                regionController.Bind(experiment, researchLocation,
                    reports.Where(r =>
                        r.ResearchLocationID == researchLocation.ResearchLocationId && r.ExperimentID == expId));
                content.Add(regionEntry);
            }
        }
    }
}