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
    ///     Unfortunately, I didn't find a way to stop the propagation of wheel events in UIToolkit to GameInput
    /// </summary>
    /// <param name="element"></param>
    public static void StopWheelEventPropagation(this VisualElement element)
    {
        element.RegisterCallback<PointerEnterEvent>(evt =>
        {
            // _Logger.LogInfo(
            //     $"PointerEnterEvent (flight={Game.Input.Flight.CameraZoom.enabled}, map={Game.Input.MapView.cameraZoom.enabled}, vab={Game.Input.VAB.cameraZoom.enabled})");
            Game.Input.Flight.CameraZoom.Disable();
            Game.Input.MapView.cameraZoom.Disable();
            Game.Input.VAB.cameraZoom.Disable();
        });
        element.RegisterCallback<PointerLeaveEvent>(evt =>
        {
            // _Logger.LogInfo("PointerLeaveEvent");
            Game.Input.Flight.CameraZoom.Enable();
            Game.Input.MapView.cameraZoom.Enable();
            Game.Input.VAB.cameraZoom.Enable();
        });
    }
}