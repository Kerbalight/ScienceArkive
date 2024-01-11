using BepInEx.Logging;
using KSP.Game;
using KSP.Sim.impl;
using ScienceArkive.API.Extensions;
using ScienceArkive.Manager;
using ScienceArkive.UI.Loader;
using ScienceArkive.Utils;
using UnityEngine.UIElements;

namespace ScienceArkive.UI.Components;

public class PlanetListController
{
    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.PlanetListController");

    private VisualElement _root;
    private ScrollView _planetsList;

    public List<CelestialBodyComponent> DisplayedBodies { get; } = new();

    public event Action<CelestialBodyComponent>? PlanetSelected;

    public PlanetListController(VisualElement root)
    {
        _root = root;
        _planetsList = _root.Q<ScrollView>("planet-list");
        _planetsList.verticalScroller.valueChanged += OnListVerticalScrollChange;
        _planetsList.Clear();
    }

    private static void OnListVerticalScrollChange(float value)
    {
        MainUIManager.Instance.ArchiveWindowController.planetsListScrollPosition = value;
    }

    public void BuildPlanetList()
    {
        _Logger.LogDebug("building planet list");
        var gameInstance = GameManager.Instance.Game;
        var celestialBodies = gameInstance.UniverseModel.GetAllCelestialBodies();
        var displayedBodiesNames =
            ArchiveManager.Instance.GetCelestialBodiesNames(Settings.ShowOnlyVisitedPlanets.Value).ToArray();

        _planetsList.Clear();
        _planetsList.verticalScroller.value = MainUIManager.Instance.ArchiveWindowController.planetsListScrollPosition;
        DisplayedBodies.Clear();

        var planetMenuItemTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetMenuItem.uxml");
        foreach (var celestialBody in celestialBodies)
        {
            if (!celestialBody.isHomeWorld && !displayedBodiesNames.Contains(celestialBody.Name)) continue;

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
            DisplayedBodies.Add(celestialBody);
        }
    }

    public void SetSelectedCelestialBody(CelestialBodyComponent? selectedBody)
    {
        foreach (var menuItem in _planetsList.Children())
        {
            menuItem.Q<Button>("menu-button").RemoveFromClassList("menu-item__selected");
            if (menuItem.Q<Label>("name").text == selectedBody?.DisplayName)
                menuItem.Q<Button>("menu-button").AddToClassList("menu-item__selected");
        }
    }

    private void OnPlanetSelected(CelestialBodyComponent selectedBody)
    {
        _Logger.LogDebug("ScienceArkive: Selected " + selectedBody.Name);
        SetSelectedCelestialBody(selectedBody);
        PlanetSelected?.Invoke(selectedBody);
    }
}