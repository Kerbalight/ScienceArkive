using SpaceWarp.API.Assets;
using UitkForKsp2.API;
using UnityEngine.UIElements;

namespace ScienceArkive.UI;

public class MainUIManager
{
    public static MainUIManager Instance { get; } = new();

    public UIDocument ArchiveWindowDocument { get; private set; } = null!;
    public ScienceArchiveWindowController ArchiveWindowController { get; private set; } = null!;

    public void Initialize()
    {
        InitializeScienceArchiveWindow();
    }

    public void ToggleUI(bool? isOpen = null)
    {
        ArchiveWindowController.IsWindowOpen = isOpen ?? !ArchiveWindowController.IsWindowOpen;
    }

    private void InitializeScienceArchiveWindow()
    {
        var windowOptions = new WindowOptions
        {
            WindowId = "ScienceArkive_MainWindow",
            // If null, it will be created under the main canvas.
            Parent = null,
            IsHidingEnabled = true,
            DisableGameInputForTextFields = true,
            MoveOptions = new MoveOptions
            {
                // Whether or not the window can be moved by dragging.
                IsMovingEnabled = true,
                CheckScreenBounds = false
            }
        };

        // Load the UI from the asset bundle
        var scienceArchiveWindowTemplate = AssetManager.GetAsset<VisualTreeAsset>(
            $"{ScienceArkivePlugin.ModGuid}/ScienceArkive_ui/ui/sciencearchivewindow/sciencearchivewindow.uxml");

        ArchiveWindowDocument = Window.Create(windowOptions, scienceArchiveWindowTemplate);
        // Add a controller for the UI to the window's game object
        ArchiveWindowController =
            ArchiveWindowDocument.gameObject.AddComponent<ScienceArchiveWindowController>();
    }
}