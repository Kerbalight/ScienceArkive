using System.Reflection;
using KSP.Game;
using KSP.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ScienceArkive.UI.Loader;

/// <summary>
///     Loads existing assets from the game, such as icons and prefabs.
/// </summary>
public class ExistingAssetsLoader
{
    public Sprite? SampleIcon { get; private set; }
    public Sprite? DataIcon { get; private set; }
    public Sprite? PlanetIcon { get; private set; }
    public Sprite? ScienceIcon { get; private set; }
    public Sprite? CheckIcon { get; private set; }

    public string ResearchInventoryPrefabPath { get; set; } =
        "Assets/UI/Prefabs/Research Report Inventory/ResearchReport-Data.prefab";

    public string BreadcrumbsControllerPath { get; set; } =
        "/GameManager/Default Game Instance(Clone)/UI Manager(Clone)/Main Canvas/GlobalHeader(Clone)/Canvas/GRP-Header/LeftSide/UI_Breadcrumbs";

    public static ExistingAssetsLoader Instance { get; } = new();

    private bool _isExistingLoaded;

    /// <summary>
    ///     Loads the required assets from game prefabs. Since this task is async (but does
    ///     not require the UI to be loaded), it can be started as soon as the plugin is initialized.
    /// </summary>
    public void StartLoadingPrefabAssets()
    {
        // Load the required assets
        _ = LoadAssetsFromPrefabs().ContinueWith(t =>
        {
            if (t.IsFaulted)
            {
                ScienceArkivePlugin.Instance.SWLogger.LogError($"Failed to load assets from prefabs: {t.Exception}");
                return;
            }

            ScienceArkivePlugin.Instance.SWLogger.LogInfo("Loaded assets");
        });
    }

    /// <summary>
    ///     Loads assets which requires the UI to be loaded.
    /// </summary>
    public void LoadAssetsFromExistingUI()
    {
        if (_isExistingLoaded)
        {
            ScienceArkivePlugin.Instance.SWLogger.LogDebug("Assets already loaded, skipping");
            return;
        }

        try
        {
            // Icons from UI
            var breadcrumbsController =
                GameObject.Find(BreadcrumbsControllerPath).GetComponent<BreadcrumbsController>();
            var breadcrumbsIcons = typeof(BreadcrumbsController)
                .GetField("_breadcrumbsIcons", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.GetValue(breadcrumbsController) as Dictionary<BreadcrumbsType, Sprite?>;

            PlanetIcon = breadcrumbsIcons?[BreadcrumbsType.Celestial];

            _isExistingLoaded = true;
        }
        catch (Exception e)
        {
            ScienceArkivePlugin.Instance.SWLogger.LogError($"Failed to load assets from existing UI: {e}");
        }
    }

    private async Task LoadAssetsFromPrefabs()
    {
        // Load the required assets from prefabs
        var researchInventoryPrefabHandle =
            GameManager.Instance.Assets.LoadAssetAsync<GameObject>(ResearchInventoryPrefabPath);
        await researchInventoryPrefabHandle.Task;

        var allTextures = researchInventoryPrefabHandle.Result.GetComponentsInChildren<Image>(true);
        //foreach (var texture in allTextures)
        //{
        //    ScienceArkivePlugin.Instance.SWLogger.LogInfo($"Texture: {texture.name}, sprite: {texture.sprite?.name}");
        //}

        SampleIcon = allTextures.First(t => t.sprite?.name == "ICO-Map-Asteroid-16x16").sprite;
        DataIcon = allTextures.First(t => t.sprite?.name == "ICO-Data-16").sprite;
        ScienceIcon = allTextures.First(t => t.sprite?.name == "ICO-ScienceJuice").sprite;
        CheckIcon = allTextures.First(t => t.sprite?.name == "ICO-Check").sprite;
    }
}