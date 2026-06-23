using UnityEngine;
using XCharts.Runtime;
using PhosphorModeling;
using PhosphorSimulator;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Настраивает и наполняет один график XCharts по выбранному показателю:
    /// оси, стиль линии, регламентные линии и добавление точек по ходу сессии.
    /// </summary>
    [RequireComponent(typeof(LineChart))]
    public sealed class ChartBinder : MonoBehaviour
    {
        private static readonly Color32 RegulationColor = new Color32(220, 20, 60, 255);

        [SerializeField] private ChartMetric metric;
        [SerializeField] private LineChart chart;

        [Header("Шрифты")]
        [SerializeField] private int titleFontSize = 16;
        [SerializeField] private int axisFontSize = 11;

        private MetricDescriptor _descriptor;

        private void Awake()
        {
            if (chart == null) chart = GetComponent<LineChart>();
            _descriptor = ChartMetrics.Get(metric);
        }

        /// <summary>Готовит график к новой сессии: оси, стиль, регламентные линии.</summary>
        public void Configure(Scenario scenario, float maxHours)
        {
            if (chart == null) return;
            var descriptor = _descriptor ??= ChartMetrics.Get(metric);

            var title = chart.EnsureChartComponent<Title>();
            title.show = true;
            title.text = descriptor.Title;
            title.labelStyle.textStyle.fontSize = titleFontSize;

            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.numericFormatter = descriptor.Format;          // округление значения в подсказке
            chart.EnsureChartComponent<Legend>().show = false;

            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Value;
            xAxis.boundaryGap = false;
            xAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            xAxis.min = 0;
            // +5 % запаса справа, чтобы было видно, что линия завершилась, а не ушла за край.
            xAxis.max = (maxHours > 0 ? maxHours : 1) * 1.05;
            xAxis.maxCache = 0;
            xAxis.splitNumber = 5;                                 // меньше делений — подписи не сливаются
            xAxis.axisLabel.numericFormatter = "F1";               // время с одним знаком
            xAxis.axisLabel.textStyle.fontSize = axisFontSize;
            xAxis.axisName.show = true;
            xAxis.axisName.name = "t, ч";
            xAxis.axisName.labelStyle.textStyle.fontSize = axisFontSize;

            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;
            yAxis.min = descriptor.YMin;
            yAxis.max = descriptor.YMax;
            yAxis.splitNumber = 4;
            yAxis.axisLabel.numericFormatter = descriptor.Format;  // округление подписей Y
            yAxis.axisLabel.textStyle.fontSize = axisFontSize;
            yAxis.axisName.show = true;
            yAxis.axisName.name = descriptor.Unit;
            yAxis.axisName.labelStyle.textStyle.fontSize = axisFontSize;

            chart.RemoveData();
            var serie = chart.AddSerie<Line>(descriptor.Title);
            serie.symbol.show = false;
            serie.lineType = LineType.Smooth;
            serie.itemStyle.color = descriptor.Color;
            serie.EnsureComponent<AreaStyle>();
            serie.areaStyle.show = true;
            serie.areaStyle.opacity = 0.18f;
            serie.maxCache = 0;

            ConfigureRegulationLines(scenario, descriptor);
            chart.RefreshChart();
        }

        /// <summary>Добавляет точку (время в часах → значение показателя).</summary>
        public void AddPoint(double timeHours, SimulationStep step)
        {
            if (chart == null || _descriptor == null) return;
            chart.AddData(0, timeHours, _descriptor.Value(step));
        }

        private void ConfigureRegulationLines(Scenario scenario, MetricDescriptor descriptor)
        {
            var markLine = chart.EnsureChartComponent<MarkLine>();
            markLine.data.Clear();
            if (scenario == null)
            {
                markLine.show = false;
                return;
            }

            markLine.show = true;
            foreach (var limit in descriptor.Limits(scenario))
                markLine.data.Add(CreateLimitLine(limit));
        }

        private static MarkLineData CreateLimitLine(RegulationLimit limit)
        {
            var data = new MarkLineData
            {
                type = MarkLineType.Custom,
                name = limit.Label,
                yValue = limit.Value,
            };
            data.lineStyle.type = LineStyle.Type.Dashed;
            data.lineStyle.color = RegulationColor;
            data.startSymbol.show = false;
            data.endSymbol.show = false;
            data.label.show = true;
            data.label.formatter = "{b}";
            return data;
        }
    }
}
