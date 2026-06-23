using UnityEngine;
using PhosphorModeling;
using PhosphorSimulator;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Координатор визуализации: подписывается на шаги модели и наполняет графики и
    /// цифровые индикаторы. Точки на графики добавляются с прореживанием по sim-времени.
    /// </summary>
    public sealed class SimulationHud : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;
        [SerializeField] private ChartBinder[] charts;
        [SerializeField] private IndicatorBinder[] indicators;
        [SerializeField] private EcoTableBinder ecoTable;

        [Tooltip("Минимальный интервал между точками графика, sim-часов.")]
        [SerializeField] private double chartSampleHours = 0.02;

        private double _lastSampleHours = -1;

        private void Awake()
        {
            if (simulation == null) simulation = FindFirstObjectByType<SimulationController>();
        }

        private void OnEnable()
        {
            if (simulation == null) simulation = FindFirstObjectByType<SimulationController>();
            if (simulation != null) simulation.OnStep += HandleStep;
            else Debug.LogWarning("[HUD] SimulationController не найден — индикаторы и графики не обновятся.");
        }

        private void OnDisable()
        {
            if (simulation != null) simulation.OnStep -= HandleStep;
        }

        /// <summary>Готовит графики и таблицу к новой сессии по сценарию.</summary>
        public void BeginSession(Scenario scenario)
        {
            EnsureBindings();
            float maxHours = (float)((scenario != null ? scenario.DurationSeconds : 120) * simulation.SimHoursPerRealSecond);
            foreach (var chart in charts)
                if (chart != null) chart.Configure(scenario, maxHours);
            if (ecoTable != null) ecoTable.Configure(scenario);
            _lastSampleHours = -1;
        }

        // Если массивы не заполнены в инспекторе — находим биндеры в сцене сами
        // (включая объекты, выключенные на старте).
        private void EnsureBindings()
        {
            if (charts == null || charts.Length == 0)
                charts = FindObjectsByType<ChartBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (indicators == null || indicators.Length == 0)
                indicators = FindObjectsByType<IndicatorBinder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (ecoTable == null)
                ecoTable = FindFirstObjectByType<EcoTableBinder>(FindObjectsInactive.Include);

            if (indicators.Length == 0)
                Debug.LogWarning("[HUD] IndicatorBinder в сцене не найдены — добавь их на поля мнемосхемы.");
        }

        private void HandleStep(SimulationStep step)
        {
            if (indicators != null)
                foreach (var indicator in indicators)
                    if (indicator != null) indicator.UpdateValue(step);

            if (ecoTable != null) ecoTable.UpdateValues(step);

            if (_lastSampleHours >= 0 && step.Time - _lastSampleHours < chartSampleHours) return;
            _lastSampleHours = step.Time;

            if (charts != null)
                foreach (var chart in charts)
                    if (chart != null) chart.AddPoint(step.Time, step);
        }
    }
}
