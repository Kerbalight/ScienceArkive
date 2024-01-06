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

namespace ScienceArkive.UI
{
    public class ScienceArchiveWindowController : MonoBehaviour
    {

        private UIDocument _window;
        private bool _isWindowOpen = false;

        private VisualElement _rootElement;

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

            var planetEntryTemplate = UIToolkitElement.Load("ScienceArchiveWindow/SciencePlanetEntry.uxml");
            var planetsView = _rootElement.Q<ScrollView>("planets-scroll");
            //planetsView.itemsSource = celestialBodies;
            foreach (var celestialBody in celestialBodies)
            {
                var planetEntry = planetEntryTemplate.Instantiate();
                var planetEntryController = new SciencePlanetEntryController(planetEntry);
                
                planetEntryController.BindPlanet(celestialBody);
                planetEntry.userData = planetEntryController;
                planetsView.Add(planetEntry);
            }
            //planetsList.makeItem = () =>
            //{
            //    var planetTemplateInstance = planetTemplate.Instantiate();
            //    planetTemplateInstance.userData = new SciencePlanetEntryController(planetTemplateInstance);
            //    return planetTemplateInstance;
            //};
            //planetsList.bindItem = (element, index) =>
            //{
            //    var planetEntryController = (SciencePlanetEntryController)element.userData;
            //    var celestialBody = celestialBodies[index];
            //    planetEntryController.BindPlanet(celestialBodies[index]);
            //};
        }

        //private void InitExperimentsList()
        //{
        //    // Load the experiment entry template from the asset bundle
        //    var experimentEntryUxml = UIToolkitElement.Load("ScienceArchiveWindow/ScienceExperimentEntry.uxml");

        //    // Load the submitted reports from the game
        //    var gameInstance = GameManager.Instance.Game;
        //    gameInstance.SessionManager.TryGetMyAgencySubmittedResearchReports(out var submittedReports);

        //    var experimentsList = _rootElement.Q<ListView>("experiments-list");
        //    experimentsList.makeItem = () =>
        //    {
        //        var entryTemplate = experimentEntryUxml.Instantiate();
        //        entryTemplate.userData = new ScienceExperimentEntryController(entryTemplate);
        //        return entryTemplate;
        //    };
        //    experimentsList.bindItem = (element, index) =>
        //    {
        //        var entryController = (ScienceExperimentEntryController)element.userData;
        //        entryController.BindExperiment(submittedReports[index]);
        //    };
        //    experimentsList.itemsSource = submittedReports;
        //    experimentsList.fixedItemHeight = 40;
            
        //}
    }
}