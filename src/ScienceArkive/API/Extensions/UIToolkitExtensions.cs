using UnityEngine.UIElements;

namespace ScienceArkive.API.Extensions;

public static class UIToolkitExtensions
{
    /// <summary>
    ///     Unfortunately, I didn't find a way to stop the propagation of wheel events in UIToolkit to GameInput
    /// </summary>
    /// <param name="element"></param>
    public static void StopWheelEventPropagation(this VisualElement element)
    {
        //element.RegisterCallback<WheelEvent>(evt =>
        //{
        //    UitkForKsp2.API.Extensions.DisableGameInputOnFocus
        //}, TrickleDown.TrickleDown);
    }
}