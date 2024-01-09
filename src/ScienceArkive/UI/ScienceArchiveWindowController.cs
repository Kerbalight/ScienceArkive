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

    private bool _isInitialized;
    private bool _isWindowOpen;
    private CelestialBodyComponent _selectedCelestialBody;

    private VisualElement _rootElement;
    private VisualElement _detailElement;
    private PlanetListController _planetListController;
    private PlanetExperimentsDetailPanel _planetDetailController;

    private UIDocument _window;

    public Vector3? WindowPosition = null;

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
        logger.LogInfo("Enabling window");
        _window = GetComponent<UIDocument>();
        _rootElement = _window.rootVisualElement[0];
        _rootElement.CenterByDefault();

        IsWindowOpen = false;

        var closeButton = _rootElement.Q<Button>("close-button");
        closeButton.clicked += () => IsWindowOpen = false;

        // Left pane
        var planetList = _rootElement.Q<VisualElement>("planet-list");
        _planetListController = new PlanetListController(planetList);
        _planetListController.PlanetSelected += SetSelectedCelestialBody;

        // Right pane
        var planetDetailTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetExperimentsDetailPanel.uxml");
        _detailElement = planetDetailTemplate.Instantiate();
        _planetDetailController = new PlanetExperimentsDetailPanel(_detailElement);
        _detailElement.userData = _planetDetailController;
        _rootElement.Q<VisualElement>("main-content").Add(_detailElement);
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

        _rootElement.RegisterCallback<PointerUpEvent>(evt => { WindowPosition = _rootElement.transform.position; });
    }

    public void BuildUI()
    {
        logger.LogInfo("Building UI");
        _rootElement.transform.position = WindowPosition ?? _rootElement.transform.position;
        _planetListController.BuildPlanetList();
        SetSelectedCelestialBody(GameManager.Instance.Game.UniverseModel.GetAllCelestialBodies()[0]);
    }

    private void SetSelectedCelestialBody(CelestialBodyComponent selectedBody)
    {
        _selectedCelestialBody = selectedBody;
        _planetListController.SetSelectedCelestialBody(selectedBody);
        _planetDetailController.BindPlanet(selectedBody);
    }

    public void Refresh()
    {
        logger.LogInfo($"Refreshing window selected planet (body {_selectedCelestialBody?.DisplayName})");
        if (_selectedCelestialBody != null) SetSelectedCelestialBody(_selectedCelestialBody);
    }
}