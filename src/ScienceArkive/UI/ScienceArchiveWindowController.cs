using KSP.UI.Binding;
using UnityEngine;
using UnityEngine.UIElements;
using UitkForKsp2.API;
using KSP.Game;
using SpaceWarp.API.Assets;
using ScienceArkive.UI.Components;
using KSP.Game.Science;
using HoudiniEngineUnity;
using KSP.Sim;
using UnityEngine.AddressableAssets;
using ScienceArkive.UI.Loader;
using KSP.Sim.impl;
using static ProFlareAtlas;
using System;

namespace ScienceArkive.UI
{
    public class ScienceArchiveWindowController : MonoBehaviour
    {

        private UIDocument _window;
        private bool _isWindowOpen = false;

        private VisualElement _rootElement;
        private VisualElement _planetsList;
        private VisualElement _detailElement;

        /// <summary>
        /// The state of the window. Setting this value will open or close the window.
        /// </summary>
        public bool IsWindowOpen
        {
            get => _isWindowOpen;
            set
            {
                _isWindowOpen = value;

                if (value)
                {
                    InitArchive();
                }

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
        /// Runs when the window is first created, and every time the window is re-enabled.
        /// </summary>
        private void OnEnable()
        {
            _window = GetComponent<UIDocument>();
            _rootElement = _window.rootVisualElement[0];
            _rootElement.CenterByDefault();

            IsWindowOpen = false;

            var closeButton = _rootElement.Q<Button>("close-button");
            closeButton.clicked += () => IsWindowOpen = false;

            var planetEntryTemplate = UIToolkitElement.Load("ScienceArchiveWindow/SciencePlanetEntry.uxml");
            var planetEntry = planetEntryTemplate.Instantiate();
            var planetEntryController = new SciencePlanetEntryController(planetEntry);
            planetEntry.userData = planetEntryController;
            _detailElement = planetEntry;
            _rootElement.Q<VisualElement>("detail-scroll").Add(_detailElement);

            _detailElement.Q<VisualElement>("planet-icon").style.backgroundImage = new StyleBackground(AssetsPatchedLoader.Instance.PlanetIcon);

            var gameInstance = GameManager.Instance?.Game;
            if (gameInstance == null)
            {
                ScienceArkivePlugin.Instance.SWLogger.LogInfo("ScienceArkive: GameManager.Instance.Game is null");
                return;
            }

            gameInstance.AgencyManager.TryGetMyAgencyEntry(out var agencyEntry);
            gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var submittedReports);

            var agencyName = agencyEntry?.AgencyName ?? "Unknown Agency";
        }

        private void InitArchive()
        {
            var gameInstance = GameManager.Instance.Game;
            var celestialBodies = gameInstance.UniverseModel.GetAllCelestialBodies();

            var planetMenuItemTemplate = UIToolkitElement.Load("ScienceArchiveWindow/PlanetMenuItem.uxml");
            _planetsList = _rootElement.Q<VisualElement>("planet-list");
            _planetsList.Clear();
            foreach (var celestialBody in celestialBodies)
            {
                var menuItem = planetMenuItemTemplate.Instantiate();
                menuItem.Q<Label>("name").text = celestialBody.DisplayName;
                menuItem.Q<Button>("menu-button").RegisterCallback<ClickEvent>(_ => OnPlanetSelected(celestialBody));
                menuItem.style.height = 40;
                _planetsList.Add(menuItem);
            }
        }

        void OnPlanetSelected(CelestialBodyComponent selectedBody)
        {
            ScienceArkivePlugin.Instance.SWLogger.LogInfo("ScienceArkive: Selected " + selectedBody.Name);

            var planetLabel = _rootElement.Q<Label>("planet-name");
            planetLabel.text = selectedBody.DisplayName;
            var planetEntryController = (SciencePlanetEntryController)_detailElement.userData;
            planetEntryController.BindPlanet(selectedBody);
        }
    }
}