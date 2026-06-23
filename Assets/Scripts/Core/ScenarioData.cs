using System.Collections.Generic;

namespace PhosphorModeling
{
    public static class ScenarioData
    {
        public static List<Scenario> GetScenarios() => new List<Scenario>
        {
            new Scenario
            {
                Name                    = "Регламентный режим РКЗ-48",
                Description             = "Поддерживайте регламентные параметры фосфорной печи в нормальном режиме работы. " +
                                          "Управляйте расходами сырья и положением электрода так, чтобы все четыре " +
                                          "критериальных показателя оставались в допустимых регламентных диапазонах.",
                TrainingDurationSeconds = 3600,
                DurationSeconds         = 120,
                // Регламентные диапазоны
                ConcMin                 = 96.0,   // концентрация P₄ не ниже 96 %
                ProdMin                 = 8.0,    // производительность не ниже 8,0 т/ч
                EnergyMax               = 47000,  // потребляемая мощность не выше 47 МВт
                TempMin                 = 1400,   // температура расплава не ниже 1400 °C
                TempMax                 = 1600,   // температура расплава не выше 1600 °C
                EcoCOMax                = 1.0,    // CO в вентвыбросах не выше 1 %
                EcoCO2Max               = 1.0,    // CO₂ в вентвыбросах не выше 1 %
            },
        };
    }
}
