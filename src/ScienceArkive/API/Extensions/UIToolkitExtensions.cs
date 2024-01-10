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
    /// Stop the wheel event from propagating to the game (e.g. zoom)
    /// </summary>
    /// <param name="element"></param>
    public static void StopWheelEventPropagation(this VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(OnVisualElementPointerEnter);
        element.RegisterCallback<PointerLeaveEvent>(OnVisualElementPointerLeave);
    }

    private static void OnVisualElementPointerEnter(PointerEnterEvent evt)
    {
        Game.Input.Flight.CameraZoom.Disable();
        Game.Input.MapView.cameraZoom.Disable();
        Game.Input.VAB.cameraZoom.Disable();
    }

    private static void OnVisualElementPointerLeave(PointerLeaveEvent evt)
    {
        Game.Input.Flight.CameraZoom.Enable();
        Game.Input.MapView.cameraZoom.Enable();
        Game.Input.VAB.cameraZoom.Enable();
    }
}