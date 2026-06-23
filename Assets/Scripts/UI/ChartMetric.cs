using System;
using System.Collections.Generic;
using UnityEngine;
using PhosphorModeling;
using PhosphorSimulator;

namespace PhosphorTrainer.UI
{
    /// <summary>Критериальный показатель, отображаемый на графике и индикаторе.</summary>
    public enum ChartMetric
    {
        Concentration,
        Temperature,
        Productivity,
        Energy,
        Mpr,
        MeltMass,
    }

    /// <summary>Регламентная граница показателя (значение + подпись для линии).</summary>
    public readonly struct RegulationLimit
    {
        public double Value { get; }
        public string Label { get; }

        public RegulationLimit(double value, string label)
        {
            Value = value;
            Label = label;
        }
    }

    /// <summary>
    /// Описание показателя: заголовок, единицы, цвет, диапазон оси Y, способ извлечения
    /// значения из шага модели и регламентные границы из сценария.
    /// </summary>
    public sealed class MetricDescriptor
    {
        public string Title { get; set; }
        public string Unit { get; set; }
        public string Format { get; set; }
        public Color32 Color { get; set; }
        public double YMin { get; set; }
        public double YMax { get; set; }
        public Func<SimulationStep, double> Value { get; set; }
        public Func<Scenario, IEnumerable<RegulationLimit>> Limits { get; set; }
    }

    /// <summary>Единый источник соответствия «показатель → представление».</summary>
    public static class ChartMetrics
    {
        public static MetricDescriptor Get(ChartMetric metric) => metric switch
        {
            ChartMetric.Concentration => new MetricDescriptor
            {
                Title = "Концентрация P₄",
                Unit = "%",
                Format = "0.0",
                Color = new Color32(46, 204, 113, 255),
                YMin = 90, YMax = 100,
                Value = s => s.C,
                Limits = sc => new[] { new RegulationLimit(sc.ConcMin, $"мин {sc.ConcMin:0.#} %") },
            },
            ChartMetric.Temperature => new MetricDescriptor
            {
                Title = "Температура расплава",
                Unit = "°C",
                Format = "0",
                Color = new Color32(231, 76, 60, 255),
                YMin = 1300, YMax = 1700,
                Value = s => s.Temperature,
                Limits = sc => new[]
                {
                    new RegulationLimit(sc.TempMin, $"мин {sc.TempMin:0}"),
                    new RegulationLimit(sc.TempMax, $"макс {sc.TempMax:0}"),
                },
            },
            ChartMetric.Productivity => new MetricDescriptor
            {
                Title = "Производительность по P₄",
                Unit = "т/ч",
                Format = "0.00",
                Color = new Color32(52, 152, 219, 255),
                YMin = 0, YMax = 15,
                Value = s => s.Gprod / 1000.0,
                Limits = sc => new[] { new RegulationLimit(sc.ProdMin, $"мин {sc.ProdMin:0.#}") },
            },
            ChartMetric.Energy => new MetricDescriptor
            {
                Title = "Энергопотребление",
                Unit = "МВт",
                Format = "0.0",
                Color = new Color32(155, 89, 182, 255),
                YMin = 30, YMax = 55,
                Value = s => s.Q / 1000.0,
                Limits = sc => new[] { new RegulationLimit(sc.EnergyMax / 1000.0, $"макс {sc.EnergyMax / 1000.0:0.#}") },
            },
            ChartMetric.Mpr => new MetricDescriptor
            {
                Title = "МПР",
                Unit = "м",
                Format = "0.00",
                Color = new Color32(26, 188, 156, 255),
                YMin = 0, YMax = 0.6,
                Value = s => s.L_mpr,
                Limits = sc => Array.Empty<RegulationLimit>(),
            },
            ChartMetric.MeltMass => new MetricDescriptor
            {
                Title = "Масса расплава",
                Unit = "т",
                Format = "0.0",
                Color = new Color32(230, 126, 34, 255),
                YMin = 0, YMax = 80,
                Value = s => s.MeltMass / 1000.0,
                Limits = sc => Array.Empty<RegulationLimit>(),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(metric), metric, null),
        };
    }
}
