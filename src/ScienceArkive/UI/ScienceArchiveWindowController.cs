using BepInEx.Logging;
using JetBrains.Annotations;
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
    private CelestialBodyComponent? _selectedCelestialBody;

    public CelestialBodyComponent? SelectedCelestialBody
    {
        get => _selectedCelestialBody;
        set
        {
            _selectedCelestialBody = value;
            _planetDetailController.BindPlanet(value);
            _planetListController.SetSelectedCelestialBody(value);
        }
    }

    private VisualElement _rootElement = null!;
    private VisualElement _detailElement = null!;
    private PlanetListController _planetListController = null!;
    private PlanetExperimentsDetailPanel _planetDetailController = null!;

    private UIDocument _window = null!;

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

            // Not sure if this is the best place to do this. We need the UI to be built before we can load the data
            // into it.
            SaveManager.Instance.LoadDataIntoUI();

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
        _rootElement.StopWheelEventPropagation();

        IsWindowOpen = false;

        var closeButton = _rootElement.Q<Button>("close-button");
        closeButton.clicked += () => IsWindowOpen = false;

        // Left pane
        var planetList = _rootElement.Q<VisualElement>("planet-list");
        _planetListController = new PlanetListController(planetList);
        _planetListController.PlanetSelected += body => SelectedCelestialBody = body;

        // Right pane
        var planetDetailTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetExperimentsDetailPanel.uxml");
        _detailElement = planetDetailTemplate.Instantiate();
        _planetDetailController = new PlanetExperimentsDetailPanel(_detailElement);
        _detailElement.userData = _planetDetailController;
        _rootElement.Q<VisualElement>("main-content").Add(_detailElement);

        // Window drag
        _rootElement.RegisterCallback<PointerUpEvent>(OnWindowDraggedPointerUp);
    }

    /// <summary>
    ///     Initializes the window when it's first opened. This is done lazily to avoid
    ///     that assets are still not available.
    /// </summary>
    private void Initialize()
    {
        logger.LogInfo("Initializing window (first display)");
        _isInitialized = true;

        _rootElement.Q<VisualElement>("planet-icon").style.backgroundImage =
            new StyleBackground(ExistingAssetsLoader.Instance.PlanetIcon);
        _rootElement.Q<VisualElement>("window-icon").style.backgroundImage =
            new StyleBackground(ExistingAssetsLoader.Instance.ScienceIcon);
    }

    private void OnWindowDraggedPointerUp(PointerUpEvent evt)
    {
        logger.LogDebug($"Window position updated {_rootElement.transform.position}");
        WindowPosition = _rootElement.transform.position;
    }

    public void BuildUI()
    {
        logger.LogInfo("Building UI");
        _rootElement.transform.position = WindowPosition ?? _rootElement.transform.position;
        _planetListController.BuildPlanetList();

        SelectedCelestialBody = SelectedCelestialBody ?? _planetListController.DisplayedBodies.First();
    }

    public void Refresh()
    {
        logger.LogInfo($"Refreshing window selected planet (body {SelectedCelestialBody?.DisplayName})");
        if (SelectedCelestialBody != null) SelectedCelestialBody = SelectedCelestialBody;
    }
}