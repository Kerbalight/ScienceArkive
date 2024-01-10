using BepInEx.Configuration;
using ScienceArkive.UI;

namespace ScienceArkive.Utils;

public static class Settings
{
    public enum DiscoverablesDisplayMode
    {
        /// Show all discoverables, regardless of whether they have been reached or not
        All,

        /// Show all the discoverables, but the ones that have not been reached are greyed out ("???")
        Censored,

        /// Show only the discoverables that have been reached
        Discovered
    }

    private static ScienceArkivePlugin Plugin => ScienceArkivePlugin.Instance;

    // Spoilers
    public static ConfigEntry<bool> ShowOnlyVisitedPlanets { get; private set; } = null!;
    public static ConfigEntry<bool> ShowOnlyUnlockedExperiments { get; private set; } = null!;
    public static ConfigEntry<DiscoverablesDisplayMode> DiscoverablesDisplay { get; private set; } = null!;

    public static void SetupConfig()
    {
        // Spoilers
        ShowOnlyVisitedPlanets = Plugin.Config.Bind(
            "Spoilers",
            "Show only visited planets",
            true, // Hide planets that have not been visited
            "If true, only planets that have been visited will be shown in the list."
        );
        ShowOnlyVisitedPlanets.SettingChanged += OnSettingChanged;

        ShowOnlyUnlockedExperiments = Plugin.Config.Bind(
            "Spoilers",
            "Show only unlocked experiments",
            true, // Hide experiments that have not been unlocked
            "If true, only experiments contained in Tech Tree Nodes that have been unlocked will be shown in the list."
        );
        ShowOnlyUnlockedExperiments.SettingChanged += OnSettingChanged;

        DiscoverablesDisplay = Plugin.Config.Bind(
            "Spoilers",
            "Discoverables display mode",
            DiscoverablesDisplayMode.Censored,
            "How to display the discoverables in the list. \n" +
            "All: show all discoverables, regardless of whether they have been reached or not. \n" +
            "Censored: show all the discoverables, but the ones that have not been reached are greyed out (\"???\"). \n" +
            "Discovered: show only the discoverables that have been reached."
        );
        DiscoverablesDisplay.SettingChanged += OnSettingChanged;
    }

    /// <summary>
    /// Refresh the UI when a setting is changed.
    /// </summary>
    private static void OnSettingChanged(object sender, EventArgs e)
    {
        MainUIManager.Instance.Refresh();
    }
}