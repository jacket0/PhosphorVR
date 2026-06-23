using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using XCharts.Runtime;

namespace ChartKit
{
    /// <summary>
    /// Переносимый компонент линейного графика на базе XCharts: фиксированные границы
    /// осей и регламентные линии-уставки прямо на графике. Не зависит от какой-либо
    /// модели — всё настраивается в инспекторе, данные подаются вызовом <see cref="AddPoint"/>.
    ///
    /// Использование:
    ///   1. Повесить на объект с компонентом <c>LineChart</c> (XCharts).
    ///   2. Задать заголовок, единицы, границы осей и список регламентов в инспекторе.
    ///   3. В рантайме звать <see cref="AddPoint"/>; при необходимости — <see cref="Clear"/>,
    ///      <see cref="SetYRange"/>, <see cref="SetXRange"/>, <see cref="SetRegulations"/>.
    /// </summary>
    [RequireComponent(typeof(LineChart))]
    public sealed class RangeLineChart : MonoBehaviour
    {
        /// <summary>Регламентная граница: горизонтальная линия на уровне <see cref="value"/>.</summary>
        [System.Serializable]
        public struct RegulationLine
        {
            public string label;   // подпись у линии ("мин 96 %")
            public float value;    // уровень по оси Y
            public Color color;    // цвет линии; прозрачный (a=0) → берётся общий regulationColor
        }

        [Header("Заголовок и формат")]
        [SerializeField] private string title = "График";
        [SerializeField] private string yUnit = "";
        [SerializeField] private string xAxisName = "t";
        [Tooltip("Формат подписей значений (C# numeric format): 0.0, 0.00, F1 …")]
        [SerializeField] private string valueFormat = "0.0";

        [Header("Границы оси Y (видимое окно)")]
        [SerializeField] private float yMin = 0f;
        [SerializeField] private float yMax = 100f;
        [SerializeField] private int ySplitNumber = 4;

        [Header("Границы оси X")]
        [SerializeField] private float xMin = 0f;
        [SerializeField] private float xMax = 10f;
        [Tooltip("Запас справа от xMax, доля (0.05 = +5%), чтобы конец линии не упирался в край.")]
        [SerializeField] private float xHeadroom = 0.05f;
        [SerializeField] private int xSplitNumber = 5;
        [SerializeField] private string xValueFormat = "F1";

        [Header("Стиль линии")]
        [SerializeField] private Color lineColor = new Color(0.18f, 0.80f, 0.44f, 1f);
        [SerializeField] private bool smooth = true;
        [SerializeField] private bool fillArea = true;
        [SerializeField, Range(0f, 1f)] private float areaOpacity = 0.18f;

        [Header("Регламентные границы (рисуются на графике)")]
        [SerializeField] private Color regulationColor = new Color32(220, 20, 60, 255);
        [SerializeField] private List<RegulationLine> regulations = new List<RegulationLine>();

        [Header("Шрифты")]
        [SerializeField] private int titleFontSize = 16;
        [SerializeField] private int axisFontSize = 11;

        [Header("Поведение")]
        [Tooltip("Применить настройки и построить оси/линию автоматически на старте.")]
        [SerializeField] private bool configureOnStart = true;

        [SerializeField] private LineChart chart;

        private bool _configured;

        private void Awake()
        {
            if (chart == null) chart = GetComponent<LineChart>();
        }

        private void Start()
        {
            if (configureOnStart) Configure();
        }

        /// <summary>Строит оси, линию и регламентные границы по текущим настройкам.</summary>
        public void Configure()
        {
            if (chart == null) chart = GetComponent<LineChart>();
            if (chart == null) return;

            var titleComp = chart.EnsureChartComponent<Title>();
            titleComp.show = !string.IsNullOrEmpty(title);
            titleComp.text = title;
            titleComp.labelStyle.textStyle.fontSize = titleFontSize;

            var tooltip = chart.EnsureChartComponent<Tooltip>();
            tooltip.show = true;
            tooltip.numericFormatter = valueFormat;
            chart.EnsureChartComponent<Legend>().show = false;

            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.type = Axis.AxisType.Value;
            xAxis.boundaryGap = false;
            xAxis.minMaxType = Axis.AxisMinMaxType.Custom;   // ручные границы, без автоподбора
            xAxis.min = xMin;
            xAxis.max = xMax + Mathf.Abs(xMax - xMin) * xHeadroom;
            xAxis.maxCache = 0;
            xAxis.splitNumber = xSplitNumber;
            xAxis.axisLabel.numericFormatter = xValueFormat;
            xAxis.axisLabel.textStyle.fontSize = axisFontSize;
            xAxis.axisName.show = !string.IsNullOrEmpty(xAxisName);
            xAxis.axisName.name = xAxisName;
            xAxis.axisName.labelStyle.textStyle.fontSize = axisFontSize;

            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.type = Axis.AxisType.Value;
            yAxis.minMaxType = Axis.AxisMinMaxType.Custom;   // ← фиксированный диапазон Y
            yAxis.min = yMin;
            yAxis.max = yMax;
            yAxis.splitNumber = ySplitNumber;
            yAxis.axisLabel.numericFormatter = valueFormat;
            yAxis.axisLabel.textStyle.fontSize = axisFontSize;
            yAxis.axisName.show = !string.IsNullOrEmpty(yUnit);
            yAxis.axisName.name = yUnit;
            yAxis.axisName.labelStyle.textStyle.fontSize = axisFontSize;

            chart.RemoveData();
            var serie = chart.AddSerie<Line>(title);
            serie.symbol.show = false;
            serie.lineType = smooth ? LineType.Smooth : LineType.Normal;
            serie.itemStyle.color = lineColor;
            serie.EnsureComponent<AreaStyle>();
            serie.areaStyle.show = fillArea;
            serie.areaStyle.opacity = areaOpacity;
            serie.maxCache = 0;

            ApplyRegulationLines();
            _configured = true;
            chart.RefreshChart();
        }

        /// <summary>Добавляет точку (x, y) к линии.</summary>
        public void AddPoint(double x, double y)
        {
            if (chart == null) return;
            if (!_configured) Configure();
            chart.AddData(0, x, y);
        }

        /// <summary>Очищает точки линии (оси и регламент сохраняются).</summary>
        public void Clear()
        {
            if (chart == null) return;
            chart.ClearData();
        }

        /// <summary>Меняет границы оси Y и перерисовывает.</summary>
        public void SetYRange(float min, float max)
        {
            yMin = min; yMax = max;
            if (!_configured) { Configure(); return; }
            var yAxis = chart.EnsureChartComponent<YAxis>();
            yAxis.min = min; yAxis.max = max;
            chart.RefreshChart();
        }

        /// <summary>Меняет границы оси X (xHeadroom применяется к max) и перерисовывает.</summary>
        public void SetXRange(float min, float max)
        {
            xMin = min; xMax = max;
            if (!_configured) { Configure(); return; }
            var xAxis = chart.EnsureChartComponent<XAxis>();
            xAxis.min = min;
            xAxis.max = max + Mathf.Abs(max - min) * xHeadroom;
            chart.RefreshChart();
        }

        /// <summary>Полностью заменяет набор регламентных линий и перерисовывает.</summary>
        public void SetRegulations(IEnumerable<RegulationLine> lines)
        {
            regulations = lines != null ? new List<RegulationLine>(lines) : new List<RegulationLine>();
            if (!_configured) { Configure(); return; }
            ApplyRegulationLines();
            chart.RefreshChart();
        }

        /// <summary>Добавляет одну регламентную линию и перерисовывает.</summary>
        public void AddRegulation(string label, float value, Color? color = null)
        {
            regulations.Add(new RegulationLine { label = label, value = value, color = color ?? default });
            if (!_configured) { Configure(); return; }
            ApplyRegulationLines();
            chart.RefreshChart();
        }

        private void ApplyRegulationLines()
        {
            var markLine = chart.EnsureChartComponent<MarkLine>();
            markLine.data.Clear();
            markLine.show = regulations != null && regulations.Count > 0;
            if (regulations == null) return;

            foreach (var limit in regulations)
                markLine.data.Add(CreateLimitLine(limit));
        }

        private MarkLineData CreateLimitLine(RegulationLine limit)
        {
            var data = new MarkLineData
            {
                type = MarkLineType.Custom,
                name = limit.label,
                yValue = limit.value,   // ← высота горизонтальной линии-уставки
            };
            data.lineStyle.type = LineStyle.Type.Dashed;
            data.lineStyle.color = limit.color.a > 0f ? limit.color : regulationColor;
            data.startSymbol.show = false;
            data.endSymbol.show = false;
            data.label.show = !string.IsNullOrEmpty(limit.label);
            data.label.formatter = "{b}";   // {b} = name (подпись линии)
            return data;
        }

        // Формат значения наружу — на случай, если нужно показать ту же точность в UI.
        public string Format(double value) => value.ToString(valueFormat, CultureInfo.InvariantCulture);
    }
}
