using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Slider used for debug options in <see cref="ARDebugMenu"/>.
    /// </summary>
    public class DebugSlider : Slider
    {
        /// <summary>
        /// Use this callback to detect pointer down events.
        /// </summary>
        /// <param name="eventData">The pointer down event data.</param>
        public override void OnPointerDown(PointerEventData eventData)
        {
            base.OnPointerDown(eventData);
            if (interactable)
            {
                // Toggle the value between 0 and 1 when the slider is tapped
                value = (value == 1) ? 0 : 1;
                // Notify listeners about the value change
                onValueChanged.Invoke(value);
            }
        }
    }
}
