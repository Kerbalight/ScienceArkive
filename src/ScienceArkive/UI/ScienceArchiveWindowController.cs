using BepInEx.Logging;
using KSP.Game;
using KSP.Messages;
using KSP.Sim.impl;
using KSP.UI.Binding;
using ScienceArkive.API.Extensions;
using ScienceArkive.Manager;
using ScienceArkive.UI.Components;
using ScienceArkive.UI.Loader;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;
using Logger = BepInEx.Logging.Logger;

namespace ScienceArkive.UI;

public class ScienceArchiveWindowController : MonoBehaviour
{
    private static readonly ManualLogSource logger = Logger.CreateLogSource("ScienceArkive.ScienceArchiveWindow");
    private VisualElement _detailElement;

    private bool _isInitialized;
    private bool _isWindowOpen;
    private VisualElement _planetsList;

    private VisualElement _rootElement;
    private CelestialBodyComponent _selectedCelestialBody;

    private UIDocument _window;

    /// <summary>
    ///     The state of the window. Setting this value will open or close the window.
    /// </summary>
    public bool IsWindowOpen
    {
        get => _isWindowOpen;
        set
        {
            _isWindowOpen = value;

            if (value && !_isInitialized) Initialize();

            // Set the display style of the root element to show or hide the window
            _rootElement.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            // Alternatively, you can deactivate the window game object to close the window and stop it from updating,
            // which is useful if you perform expensive operations in the window update loop. However, this will also
            // mean you will have to re-register any event handlers on the window elements when re-enabled in OnEnable.
            // gameObject.SetActive(value);

            // Update the Flight AppBar button state
            GameObject.Find(ScienceArkivePlugin.ToolbarFlightButtonID)
                ?.GetComponent<UIValue_WriteBool_Toggle>()
                ?.SetValue(value);

            // Update the OAB AppBar button state
            GameObject.Find(ScienceArkivePlugin.ToolbarOabButtonID)
                ?.GetComponent<UIValue_WriteBool_Toggle>()
                ?.SetValue(value);
        }
    }

    /// <summary>
    ///     Runs when the window is first created, and every time the window is re-enabled.
    /// </summary>
    private void OnEnable()
    {
        _window = GetComponent<UIDocument>();
        _rootElement = _window.rootVisualElement[0];
        _rootElement.CenterByDefault();

        IsWindowOpen = false;

        var closeButton = _rootElement.Q<Button>("close-button");
        closeButton.clicked += () => IsWindowOpen = false;

        var planetDetailTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetExperimentsDetailPanel.uxml");
        _detailElement = planetDetailTemplate.Instantiate();
        var planetDetailController = new PlanetExperimentsDetailPanel(_detailElement);
        _detailElement.userData = planetDetailController;

        // Right pane
        _rootElement.Q<VisualElement>("main-content").Add(_detailElement);

        // Messages
        ArchiveManager.Instance.OnPostGameLoadFinished += () => RebuildPlanetList();
        ArchiveManager.Instance.MessageCenter.Subscribe<ScienceEarnedOnRecoveryMessage>(_ => Refresh());
    }

    /// <summary>
    ///     Initializes the window when it's first opened. This is done lazily to avoid
    ///     that assets are still not available.
    /// </summary>
    private void Initialize()
    {
        logger.LogInfo("Initializing window");
        _isInitialized = true;

        _rootElement.Q<VisualElement>("planet-icon").style.backgroundImage =
            new StyleBackground(ExistingAssetsLoader.Instance.PlanetIcon);
        _rootElement.Q<VisualElement>("window-icon").style.backgroundImage =
            new StyleBackground(ExistingAssetsLoader.Instance.ScienceIcon);

        RebuildPlanetList();
    }

    private void RebuildPlanetList()
    {
        logger.LogInfo("Rebuilding planet list");
        var gameInstance = GameManager.Instance.Game;
        var celestialBodies = gameInstance.UniverseModel.GetAllCelestialBodies();

        var planetMenuItemTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetMenuItem.uxml");
        _planetsList = _rootElement.Q<VisualElement>("planet-list");
        _planetsList.StopWheelEventPropagation();
        _planetsList.Clear();
        foreach (var celestialBody in celestialBodies)
        {
            var isStar = celestialBody.IsStar;
            var isMoon = celestialBody.referenceBody != null && !celestialBody.referenceBody.IsStar;

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

        SetSelectedCelestialBody(celestialBodies[0]);
    }

    private void SetSelectedCelestialBody(CelestialBodyComponent selectedBody)
    {
        _selectedCelestialBody = selectedBody;

        var planetLabel = _rootElement.Q<Label>("planet-name");
        planetLabel.text = selectedBody.DisplayName;
        var planetEntryController = (PlanetExperimentsDetailPanel)_detailElement.userData;
        planetEntryController.BindPlanet(selectedBody);

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
    }

    private void Refresh()
    {
        logger.LogInfo($"Refreshing window selected planet (body {_selectedCelestialBody?.DisplayName})");
        if (_selectedCelestialBody != null) SetSelectedCelestialBody(_selectedCelestialBody);
    }
}