using System.Globalization;
using TMPro;
using UnityEngine;
using PhosphorModeling;
using PhosphorSimulator;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Таблица экологических показателей: текущие концентрации CO/CO₂ из модели и их
    /// предельные значения из регламента сценария. Превышение подсвечивается цветом.
    /// </summary>
    public sealed class EcoTableBinder : MonoBehaviour
    {
        [Header("CO")]
        [SerializeField] private TMP_Text coCurrent;
        [SerializeField] private TMP_Text coLimit;

        [Header("CO₂")]
        [SerializeField] private TMP_Text co2Current;
        [SerializeField] private TMP_Text co2Limit;

        [Header("Формат и подсветка")]
        [SerializeField] private string format = "0.00";
        [SerializeField] private string unit = " %";
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color exceededColor = new Color(0.86f, 0.20f, 0.18f);

        private double _coLimit = double.MaxValue;
        private double _co2Limit = double.MaxValue;

        /// <summary>Заполняет предельные значения из регламента сценария.</summary>
        public void Configure(Scenario scenario)
        {
            _coLimit = scenario != null ? scenario.EcoCOMax : double.MaxValue;
            _co2Limit = scenario != null ? scenario.EcoCO2Max : double.MaxValue;
            if (coLimit != null) coLimit.text = Format(_coLimit);
            if (co2Limit != null) co2Limit.text = Format(_co2Limit);
        }

        /// <summary>Обновляет текущие значения и подсветку по шагу модели.</summary>
        public void UpdateValues(SimulationStep step)
        {
            Apply(coCurrent, step.Eco_CO_pct, _coLimit);
            Apply(co2Current, step.Eco_CO2_pct, _co2Limit);
        }

        private void Apply(TMP_Text label, double value, double limit)
        {
            if (label == null) return;
            label.text = Format(value);
            label.color = value > limit ? exceededColor : normalColor;
        }

        private string Format(double value) =>
            value.ToString(format, CultureInfo.InvariantCulture) + unit;
    }
}
