﻿using BepInEx.Logging;
using KSP.Game;
using KSP.Sim.impl;
using ScienceArkive.API.Extensions;
using ScienceArkive.UI.Loader;
using UnityEngine.UIElements;

namespace ScienceArkive.UI.Components;

public class PlanetListController
{
    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.PlanetListController");

    private VisualElement _root;
    private VisualElement _planetsList;

    public event Action<CelestialBodyComponent> PlanetSelected;

    public PlanetListController(VisualElement root)
    {
        _root = root;
        _planetsList = _root.Q<VisualElement>("planet-list");
        _planetsList.StopWheelEventPropagation();
        _planetsList.Clear();
    }

    public void BuildPlanetList()
    {
        _Logger.LogInfo("building planet list");
        var gameInstance = GameManager.Instance.Game;
        var celestialBodies = gameInstance.UniverseModel.GetAllCelestialBodies();

        _planetsList.Clear();
        var planetMenuItemTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetMenuItem.uxml");
        foreach (var celestialBody in celestialBodies)
        {
            var isStar = celestialBody.IsStar;
            var isMoon = celestialBody.referenceBody is { IsStar: false };

            var menuItem = planetMenuItemTemplate.Instantiate();
            menuItem.Q<Label>("name").text = celestialBody.DisplayName;
            if (isMoon) menuItem.style.marginLeft = 20;

            if (!isStar)
                menuItem.Q<VisualElement>("planet-icon").style.backgroundImage =
                    new StyleBackground(ExistingAssetsLoader.Instance.PlanetIcon);

            menuItem.Q<Button>("menu-button").RegisterCallback<ClickEvent>(_ => OnPlanetSelected(celestialBody));
            menuItem.style.height = 41;
            _planetsList.Add(menuItem);
        }
    }

    public void SetSelectedCelestialBody(CelestialBodyComponent selectedBody)
    {
        foreach (var menuItem in _planetsList.Children())
        {
            menuItem.Q<Button>("menu-button").RemoveFromClassList("menu-item__selected");
            if (menuItem.Q<Label>("name").text == selectedBody.DisplayName)
                menuItem.Q<Button>("menu-button").AddToClassList("menu-item__selected");
        }
    }

    private void OnPlanetSelected(CelestialBodyComponent selectedBody)
    {
        //logger.LogInfo("ScienceArkive: Selected " + selectedBody.Name);
        SetSelectedCelestialBody(selectedBody);
        PlanetSelected?.Invoke(selectedBody);
    }
}