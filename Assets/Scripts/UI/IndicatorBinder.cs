using System.Globalization;
using TMPro;
using UnityEngine;
using PhosphorModeling;
using PhosphorSimulator;

namespace PhosphorTrainer.UI
{
    /// <summary>Цифровое поле вывода текущего значения показателя на мнемосхеме/пульте.</summary>
    public sealed class IndicatorBinder : MonoBehaviour
    {
        [SerializeField] private ChartMetric metric;
        [SerializeField] private TMP_Text valueLabel;
        [SerializeField] private bool showUnit = true;

        private MetricDescriptor _descriptor;

        private void Awake()
        {
            _descriptor = ChartMetrics.Get(metric);
            if (valueLabel == null) valueLabel = GetComponent<TMP_Text>();
            if (valueLabel == null)
                Debug.LogWarning($"[Indicator] {name}: не задан Value Label (TMP). Повесь компонент на сам текст значения или перетащи текст в поле Value Label.", this);
        }

        public void UpdateValue(SimulationStep step)
        {
            if (valueLabel == null || _descriptor == null) return;
            string value = _descriptor.Value(step).ToString(_descriptor.Format, CultureInfo.InvariantCulture);
            valueLabel.text = showUnit ? $"{value} {_descriptor.Unit}" : value;
        }
    }
}
