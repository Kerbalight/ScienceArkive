﻿using HarmonyLib;
using KSP.Game;
using KSP.Game.Science;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.UI.Flight;
using KSP.Utilities;
using ScienceArkive.Data;
using ScienceArkive.UI.Loader;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace ScienceArkive.Patches;

public class FlightReportUIManagerPatches
{
    private static Dictionary<IGGuid, List<ResearchReportDisplayBag>> _scienceReportsCache = new();

    /// <summary>
    /// We need to save the science reports before the vessel is recovered, because
    /// after they are submitted to ScienceManager, they are removed from the vessel.
    /// </summary>
    [HarmonyPatch(typeof(VesselComponent), "RecoverVessel")]
    [HarmonyPrefix]
    public static void SaveScienceReportsBeforeRecoverVessel(IGGuid recoveryLocation, VesselComponent __instance)
    {
        var vesselObject = __instance.SimulationObject;
        if (vesselObject.ScienceStorage == null || __instance.Game.ScienceManager == null) return;

        // Save the science reports before the vessel is recovered
        _scienceReportsCache[vesselObject.GlobalId] = [];
        foreach (var report in vesselObject.ScienceStorage.GetStoredResearchReports())
            _scienceReportsCache[vesselObject.GlobalId].Add(new ResearchReportDisplayBag(report));
    }

    /// <summary>
    /// When the vessel is recovered, add a new science report item to the UI list.
    /// The UI report item is already created by devs, we just need to add it to the list.
    /// </summary>
    [HarmonyPatch(typeof(FlightReportUIManager), "OnVesselRecovered")]
    [HarmonyPostfix]
    private static void OnVesselRecoveredUpdateScienceReports(MessageCenterMessage msg,
        FlightReportUIManager __instance, GameObjectPool<FlightReportResearchItem> ____researchItemPool,
        RectTransform ____researchParentTransform, List<FlightReportResearchItem> ____researchItems)
    {
        if (msg is not VesselRecoveredMessage vesselMessage) return;
        var cachedReports = _scienceReportsCache[vesselMessage.VesselID];
        if (cachedReports == null) return;

        var totalScienceValue = 0f;

        foreach (var reportDisplayBag in _scienceReportsCache[vesselMessage.VesselID])
        {
            var flightReportResearchItem = ____researchItemPool.FetchInstance();
            flightReportResearchItem.transform.SetParent(____researchParentTransform, true);
            flightReportResearchItem.Initialize(reportDisplayBag.ReportType == ScienceReportType.DataType
                    ? ExistingAssetsLoader.Instance.DataIcon
                    : ExistingAssetsLoader.Instance.SampleIcon,
                reportDisplayBag.DisplayName + "\n<size=12><uppercase>@ <color=#E7CA76>" +
                reportDisplayBag.CelestialBodyName +
                "</color> / " + reportDisplayBag.ResearchLocationName + "</uppercase></size>",
                (int)reportDisplayBag.ScienceValue);

            totalScienceValue += reportDisplayBag.ScienceValue;

            // UI Fixes
            FixFlightReportResearchItemUIStyle(flightReportResearchItem);

            ____researchItems.Add(flightReportResearchItem);
        }

        // Add total science value
        var totalScienceValueItem = ____researchItemPool.FetchInstance();
        totalScienceValueItem.transform.SetParent(____researchParentTransform, true);
        totalScienceValueItem.Initialize(ExistingAssetsLoader.Instance.ScienceIcon,
            $"<uppercase>{LocalizedStrings.TotalSciencePoints}</uppercase>",
            (int)totalScienceValue);
        FixFlightReportResearchItemUIStyle(totalScienceValueItem);
        ____researchItems.Add(totalScienceValueItem);

        // Cleanup
        _scienceReportsCache.Remove(vesselMessage.VesselID);
    }

    private static void FixFlightReportResearchItemUIStyle(FlightReportResearchItem flightReportResearchItem)
    {
        flightReportResearchItem.GetComponent<RectTransform>().localScale = Vector3.one;

        var horizontalLayoutGroup = flightReportResearchItem.GetComponent<HorizontalLayoutGroup>();
        horizontalLayoutGroup.childControlHeight = true;
        horizontalLayoutGroup.childControlWidth = true;
        horizontalLayoutGroup.childForceExpandWidth = false;
        horizontalLayoutGroup.childForceExpandHeight = true;
        horizontalLayoutGroup.spacing = 8;
        horizontalLayoutGroup.padding = new RectOffset(10, 10, 0, 0);

        var icon = flightReportResearchItem.transform.Find("Icon");
        icon.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
        var iconImage = icon.GetComponent<Image>();
        iconImage.color = new Color(0.6666667f, 0.6784314f, 1, 1);
        iconImage.preserveAspect = true;
        var layoutElement = icon.gameObject.GetOrAddComponent<LayoutElement>();
        layoutElement.flexibleWidth = 0;
        layoutElement.preferredWidth = 20;

        var background = flightReportResearchItem.transform.Find("Background");
        background.GetComponent<Graphic>().color = new Color(0, 0, 0, 0);

        var entryTitle = flightReportResearchItem.transform.Find("Entry Title").GetComponent<TextMeshProUGUI>();
        entryTitle.fontSize = 16;
        entryTitle.horizontalAlignment = HorizontalAlignmentOptions.Left;
        entryTitle.autoSizeTextContainer = true; // 0.6666667 0.6784314 1 1 color
        entryTitle.color = new Color(0.6666667f, 0.6784314f, 1, 1);
        var entryTitleLayoutElement = entryTitle.gameObject.AddComponent<LayoutElement>();
        entryTitleLayoutElement.flexibleWidth = 1;
        var entryTitleContentFitter = entryTitle.GetComponent<ContentSizeFitter>();
        entryTitleContentFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        entryTitleContentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
    }
}