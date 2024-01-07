using KSP.Game;
using KSP.Game.Science;
using UnityEngine;
using UnityEngine.UI;

namespace ScienceArkive.UI.Loader
{
    public class AssetsPatchedLoader
    {
        public Sprite SampleIcon { get; private set; }
        public Sprite DataIcon { get; private set; }
        public Sprite PlanetIcon { get; private set; }

        public string ResearchInventoryPrefabPath { get; set;  } = "Assets/UI/Prefabs/Research Report Inventory/ResearchReport-Data.prefab";
        public string TrackingStationPath { get; set; } = "/GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/MapView(Clone)/MapUI/GRP-ObjectPicker/ObjectPicker/GRP-Body/ScrollView-Sort-CelestialBodies/Viewport/Content/Kerbol/Content/AccordianContent/Kerbin/AccordionToggle/GRP-ObjectEntry/BODY-Entry/GRP-LeftAlign/ICO-Object/IconCelestialBody";

        public static AssetsPatchedLoader Instance { get; } = new();

        public void StartLoadingAssets()
        {
            // Load the required assets
            _ = LoadRequiredAssets().ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    ScienceArkivePlugin.Instance.SWLogger.LogError($"Failed to load assets: {t.Exception}");
                    return;
                }

                ScienceArkivePlugin.Instance.SWLogger.LogInfo($"Loaded assets");
            });
        }

        private async Task LoadRequiredAssets()
        {
            // Icons from UI
            PlanetIcon = GameManager.Instance.Game.UI.NotificationPlanetIcon;

            // Load the required assets
            var researchInventoryPrefabHandle = GameManager.Instance.Assets.LoadAssetAsync<GameObject>(ResearchInventoryPrefabPath);
            await researchInventoryPrefabHandle.Task;

            var allTextures = researchInventoryPrefabHandle.Result.GetChild("GRP-Row-One").GetComponentsInChildren<Image>();
            SampleIcon = allTextures.First(t => t.sprite.name == "ICO-Map-Asteroid-16x16").sprite;
            DataIcon = allTextures.First(t => t.sprite.name == "ICO-Data-16").sprite;
            ScienceArkivePlugin.Instance.SWLogger.LogInfo($"Loaded icons {SampleIcon.name}");
        }
    }
}
