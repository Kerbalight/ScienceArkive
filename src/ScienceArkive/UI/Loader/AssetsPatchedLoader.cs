using KSP.Game;
using KSP.Game.Science;
using KSP.UI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace ScienceArkive.UI.Loader
{
     public class AssetsPatchedLoader
    {
        public Sprite SampleIcon { get; private set; }
        public Sprite DataIcon { get; private set; }
        public Sprite PlanetIcon { get; private set; }
        public Sprite ScienceIcon { get; private set; }
        public Sprite CheckIcon { get; private set; }

        public string ResearchInventoryPrefabPath { get; set;  } = "Assets/UI/Prefabs/Research Report Inventory/ResearchReport-Data.prefab";
        public string TrackingStationPath { get; set; } = "/GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/MapView(Clone)/MapUI/GRP-ObjectPicker/ObjectPicker/GRP-Body/GRP-Sorting-Groups/BTN-Sort-CB";
        public string BreadcrumbsControllerPath { get; set; } = "/GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/GRP-Header/LeftSide/UI_Breadcrumbs";
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

        public void LoadAssetsFromExistingUI()
        {
            // Icons from UI
            //PlanetIcon = GameObject.Find(TrackingStationPath).GetComponentInChildren<Image>().sprite;
            var breadcrumbsController = GameObject.Find(BreadcrumbsControllerPath).GetComponent<BreadcrumbsController>();
            var breadcrumbsIcons = typeof(BreadcrumbsController)
                .GetField("_breadcrumbsIcons", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(breadcrumbsController) as Dictionary<BreadcrumbsType, Sprite>;

            PlanetIcon = breadcrumbsIcons[BreadcrumbsType.Celestial];
        }

        private async Task LoadRequiredAssets()
        {
            // Load the required assets from prefabs
            var researchInventoryPrefabHandle = GameManager.Instance.Assets.LoadAssetAsync<GameObject>(ResearchInventoryPrefabPath);
            await researchInventoryPrefabHandle.Task;

            var allTextures = researchInventoryPrefabHandle.Result.GetComponentsInChildren<Image>(true);
            foreach (var texture in allTextures)
            {
                ScienceArkivePlugin.Instance.SWLogger.LogInfo($"Texture: {texture.name}, sprite: {texture.sprite?.name}");
            }

            SampleIcon = allTextures.First(t => t.sprite?.name == "ICO-Map-Asteroid-16x16").sprite;
            DataIcon = allTextures.First(t => t.sprite?.name == "ICO-Data-16").sprite;
            ScienceIcon = allTextures.First(t => t.sprite?.name == "ICO-ScienceJuice").sprite;
            CheckIcon = allTextures.First(t => t.sprite?.name == "ICO-Check").sprite;
            ScienceArkivePlugin.Instance.SWLogger.LogInfo($"Loaded icons");
        }
    }
}
