using System.Reflection;
using BepInEx;
using JetBrains.Annotations;
using ScienceArkive.Manager;
using ScienceArkive.UI;
using ScienceArkive.UI.Loader;
using SpaceWarp;
using SpaceWarp.API.Assets;
using SpaceWarp.API.Mods;
using SpaceWarp.API.UI.Appbar;
using UitkForKsp2.API;
using UnityEngine;
using UnityEngine.UIElements;

namespace ScienceArkive;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency(SpaceWarpPlugin.ModGuid, SpaceWarpPlugin.ModVer)]
public class ScienceArkivePlugin : BaseSpaceWarpPlugin
{
    // Useful in case some other mod wants to use this mod a dependency
    [PublicAPI] public const string ModGuid = MyPluginInfo.PLUGIN_GUID;
    [PublicAPI] public const string ModName = MyPluginInfo.PLUGIN_NAME;
    [PublicAPI] public const string ModVer = MyPluginInfo.PLUGIN_VERSION;

    // AppBar button IDs
    internal const string ToolbarFlightButtonID = "BTN-ScienceArkiveFlight";
    internal const string ToolbarOabButtonID = "BTN-ScienceArkiveOAB";
    internal const string ToolbarKscButtonID = "BTN-ScienceArkiveKSC";

    /// Singleton instance of the plugin class
    [PublicAPI]
    public static ScienceArkivePlugin Instance { get; set; } = null!;

    /// <summary>
    ///     Runs when the mod is first initialized.
    /// </summary>
    public override void OnInitialized()
    {
        base.OnInitialized();

        Instance = this;

        // Load all the other assemblies used by this mod
        LoadAssemblies();

        // Initialize the UI
        MainUIManager.Instance.Initialize();

        Logger.LogInfo("OnInitialized: Registering Appbar buttons");

        // Register Flight AppBar button
        Appbar.RegisterAppButton(
            ModName,
            ToolbarFlightButtonID,
            AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
            isOpen => MainUIManager.Instance.ToggleUI(isOpen)
        );

        // Register OAB AppBar Button
        Appbar.RegisterOABAppButton(
            ModName,
            ToolbarOabButtonID,
            AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
            isOpen => MainUIManager.Instance.ToggleUI(isOpen)
        );

        // Register KSC AppBar Button
        Appbar.RegisterKSCAppButton(
            ModName,
            ToolbarKscButtonID,
            AssetManager.GetAsset<Texture2D>($"{ModGuid}/images/icon.png"),
            () => MainUIManager.Instance.ToggleUI()
        );

        // Patches
        //Harmony.CreateAndPatchAll(typeof(ScienceRegionsPatches));
        ExistingAssetsLoader.Instance.StartLoadingPrefabAssets();

        // Messages subscribe
        MessageListener.Instance.SubscribeToMessages();

        // Setup config (settings)
        Utils.Settings.SetupConfig();

        // Save manager
        SaveManager.Instance.Register();
    }

    private static void SetupConfig()
    {
    }

    /// <summary>
    ///     Loads all the assemblies for the mod.
    /// </summary>
    private static void LoadAssemblies()
    {
        // Load the Unity project assembly
        var currentFolder = new FileInfo(Assembly.GetExecutingAssembly().Location).Directory!.FullName;
        var unityAssembly = Assembly.LoadFrom(Path.Combine(currentFolder, "ScienceArkive.Unity.dll"));
        // Register any custom UI controls from the loaded assembly
        CustomControls.RegisterFromAssembly(unityAssembly);
    }
}