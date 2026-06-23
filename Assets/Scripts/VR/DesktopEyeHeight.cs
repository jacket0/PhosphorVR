using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR;

namespace PhosphorTrainer.VR
{
    /// <summary>
    /// Даёт камере разумную высоту глаз при тесте на ПК без очков (редактор/симулятор
    /// без трекинга высоты), НЕ меняя поведение на реальном шлеме. На шлеме высоту
    /// головы даёт трекинг, поэтому Camera Offset остаётся 0 (режим Floor) — скрипт
    /// в это не вмешивается.
    ///
    /// Вешать на объект XR Origin (XR Rig).
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public sealed class DesktopEyeHeight : MonoBehaviour
    {
        [Tooltip("Высота глаз для теста без трекинга, м.")]
        [SerializeField] private float fallbackEyeHeight = 1.6f;

        [Tooltip("Ниже этой высоты головы считаем, что реального трекинга нет.")]
        [SerializeField] private float trackedThreshold = 0.3f;

        private XROrigin _origin;

        private void Awake() => _origin = GetComponent<XROrigin>();

        private void LateUpdate()
        {
            // Голова реально трекается (шлем/Link/симулятор с высотой) — не вмешиваемся.
            if (HasTrackedHeadHeight()) return;

            var offset = _origin != null ? _origin.CameraFloorOffsetObject : null;
            if (offset == null) return;

            var p = offset.transform.localPosition;
            if (!Mathf.Approximately(p.y, fallbackEyeHeight))
            {
                p.y = fallbackEyeHeight;
                offset.transform.localPosition = p;
            }
        }

        // Есть ли валидный трекинг высоты головы (centerEyePosition.y выше порога).
        private bool HasTrackedHeadHeight()
        {
            var head = InputDevices.GetDeviceAtXRNode(XRNode.Head);
            return head.isValid
                && head.TryGetFeatureValue(CommonUsages.centerEyePosition, out var pos)
                && pos.y > trackedThreshold;
        }
    }
}
