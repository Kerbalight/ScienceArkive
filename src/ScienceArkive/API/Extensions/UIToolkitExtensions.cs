using BepInEx.Logging;
using KSP.Game;
using KSP.Input;
using UnityEngine.UIElements;

namespace ScienceArkive.API.Extensions;

public static class UIToolkitExtensions
{
    private static readonly ManualLogSource _Logger = Logger.CreateLogSource("ScienceArkive.UIToolkitExtensions");

    public static GameInstance Game => GameManager.Instance.Game;

    /// <summary>
    /// Stop the mouse events (scroll and click) from propagating to the game (e.g. zoom)
    /// </summary>
    /// <param name="element"></param>
    public static void StopMouseEventsPropagation(this VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(OnVisualElementPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnVisualElementPointerLeave);
    }

    private static void OnVisualElementPointerEnter(PointerEnterEvent evt)
    {
        Game.Input.Flight.CameraZoom.Disable();
        Game.Input.Flight.Interact.Disable();
        Game.Input.Flight.InteractAlt.Disable();
        Game.Input.Flight.InteractAlt2.Disable();

        Game.Input.MapView.cameraZoom.Disable();
        Game.Input.MapView.mousePrimary.Disable();
        Game.Input.MapView.mouseSecondary.Disable();
        Game.Input.MapView.mouseTertiary.Disable();
        Game.Input.MapView.mousePosition.Disable();

        Game.Input.VAB.cameraZoom.Disable();
        Game.Input.VAB.mousePrimary.Disable();
        Game.Input.VAB.mouseSecondary.Disable();
    }

    private static void OnVisualElementPointerLeave(PointerLeaveEvent evt)
    {
        Game.Input.Flight.CameraZoom.Enable();
        Game.Input.Flight.Interact.Enable();
        Game.Input.Flight.InteractAlt.Enable();
        Game.Input.Flight.InteractAlt2.Enable();

        Game.Input.MapView.cameraZoom.Enable();
        Game.Input.MapView.mousePrimary.Enable();
        Game.Input.MapView.mouseSecondary.Enable();
        Game.Input.MapView.mouseTertiary.Enable();
        Game.Input.MapView.mousePosition.Enable();

        Game.Input.VAB.cameraZoom.Enable();
        Game.Input.VAB.mousePrimary.Enable();
        Game.Input.VAB.mouseSecondary.Enable();
    }
}