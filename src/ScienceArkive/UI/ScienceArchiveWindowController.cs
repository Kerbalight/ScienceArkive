using System.Collections;
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

    // UI State
    private bool _isWindowOpen;
    private CelestialBodyComponent? _selectedCelestialBody;
    public Vector3? WindowPosition = null;
    public readonly Dictionary<string, bool> CollapsedExperiments = new();
    public float detailScrollPosition = 0f;
    public float planetsListScrollPosition = 0f;

    public bool IsDirty { get; set; }
    private IEnumerator? _uiRefreshTask;

    public CelestialBodyComponent? SelectedCelestialBody
    {
        get => _selectedCelestialBody;
        set
        {
            _selectedCelestialBody = value;
            PlanetExperimentsDetail.BindPlanet(value);
            PlanetList.SetSelectedCelestialBody(value);
        }
    }

    private VisualElement _rootElement = null!;
    private VisualElement _detailElement = null!;
    public PlanetListController PlanetList { get; private set; } = null!;
    public PlanetExperimentsDetailPanel PlanetExperimentsDetail { get; private set; } = null!;

    private UIDocument _window = null!;


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

            HandleRunRefreshTask();

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

    private void HandleRunRefreshTask()
    {
        switch (IsWindowOpen)
        {
            case true when _uiRefreshTask == null:
                _uiRefreshTask = RunRefreshTask();
                StartCoroutine(_uiRefreshTask);
                break;
            case false when _uiRefreshTask != null:
                StopCoroutine(_uiRefreshTask);
                _uiRefreshTask = null;
                break;
        }
    }

    /// <summary>
    /// Updates the UI every periodically in background. Only if the window is open and
    /// it is marked as dirty.
    /// </summary>
    /// <returns></returns>
    private IEnumerator RunRefreshTask()
    {
        while (true)
        {
            if (!IsWindowOpen)
            {
                logger.LogInfo("UI is closed, stopping refresh task.");
                break;
            }

            if (IsDirty)
            {
                logger.LogDebug("Dirty UI: Refreshing");
                Refresh();
            }

            yield return new WaitForSeconds(1);
        }
    }

    /// <summary>
    ///     Runs when the window is first created, and every time the window is re-enabled.
    /// </summary>
    private void OnEnable()
    {
        logger.LogInfo("Enabling main window for the archive");
        _window = GetComponent<UIDocument>();
        _rootElement = _window.rootVisualElement[0];
        _rootElement.CenterByDefault();
        _rootElement.StopMouseEventsPropagation();

        IsWindowOpen = false;

        var closeButton = _rootElement.Q<Button>("close-button");
        closeButton.clicked += () => IsWindowOpen = false;

        // Left pane
        var planetList = _rootElement.Q<VisualElement>("planet-list");
        PlanetList = new PlanetListController(planetList);
        PlanetList.PlanetSelected += body =>
        {
            MainUIManager.Instance.ArchiveWindowController.detailScrollPosition = 0f;
            SelectedCelestialBody = body;
        };

        // Right pane
        var planetDetailTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetExperimentsDetailPanel.uxml");
        _detailElement = planetDetailTemplate.Instantiate();
        PlanetExperimentsDetail = new PlanetExperimentsDetailPanel(_detailElement);
        _detailElement.userData = PlanetExperimentsDetail;
        _rootElement.Q<VisualElement>("main-content").Add(_detailElement);

        // Window drag
        _rootElement.RegisterCallback<PointerUpEvent>(OnWindowDraggedPointerUp);
    }

    /// <summary>
    ///     Initializes the window when it's first opened. This is done lazily to avoid
    ///     that assets are still not available.
    /// </summary>
    public void Initialize()
    {
        logger.LogDebug("Initializing window (first display)");
        _isInitialized = true;

        _rootElement.Q<VisualElement>("planet-icon").style.backgroundImage =
            new StyleBackground(ExistingAssetsLoader.Instance.PlanetIcon);
        _rootElement.Q<VisualElement>("window-icon").style.backgroundImage =
            new StyleBackground(ExistingAssetsLoader.Instance.ScienceIcon);

        PlanetList.BuildPlanetList();
        SelectedCelestialBody = PlanetList.DisplayedBodies.FirstOrDefault();
    }

    private void OnWindowDraggedPointerUp(PointerUpEvent evt)
    {
        WindowPosition = _rootElement.transform.position;
    }

    public void ReloadAfterSaveLoad()
    {
        _rootElement.transform.position = WindowPosition ?? _rootElement.transform.position;
        SelectedCelestialBody = SelectedCelestialBody ?? PlanetList.DisplayedBodies.FirstOrDefault();
        Refresh();
    }

    public void Refresh()
    {
        IsDirty = false;

        PlanetList.Refresh();
        PlanetExperimentsDetail.Refresh();
    }
}