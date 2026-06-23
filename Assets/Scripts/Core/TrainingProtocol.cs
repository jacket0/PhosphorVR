using System;
using System.Collections.Generic;

namespace PhosphorModeling
{
    /// <summary>
    /// Запись об одном прогоне моделирования внутри учебной сессии.
    /// Содержит управляющие воздействия и финальные критериальные показатели.
    /// </summary>
    public class SimulationRecord
    {
        public int      RunNumber          { get; set; }
        public DateTime RunTime            { get; set; }

        // Управляющие воздействия
        public double PhosphoriteFlow      { get; set; }  // т/ч
        public double QuartzFlow           { get; set; }  // т/ч
        public double CokeFlow             { get; set; }  // т/ч
        public double ElectrodeMovement    { get; set; }  // м

        // Критериальные показатели (финальные значения симуляции)
        public double Concentration        { get; set; }  // %
        public double Productivity         { get; set; }  // т/ч
        public double EnergyConsumption    { get; set; }  // кВт
        public double Temperature          { get; set; }  // °C
        public double EcoCO                { get; set; }  // CO в вентвыбросах, %
        public double EcoCO2               { get; set; }  // CO₂ в вентвыбросах, %

        // Соответствие регламенту
        public bool ConcOk    { get; set; }
        public bool ProdOk    { get; set; }
        public bool EnergyOk  { get; set; }
        public bool TempOk    { get; set; }
        public bool EcoOk     { get; set; }
        public bool AllOk     => ConcOk && ProdOk && EnergyOk && TempOk && EcoOk;
    }

    /// <summary>
    /// Протокол всей учебной сессии. Содержит набор записей по каждому
    /// проведённому прогону моделирования. Может использоваться для
    /// сохранения в БД и отображения итогов обучения.
    /// </summary>
    public class TrainingProtocol
    {
        public string   ScenarioName     { get; set; }
        public DateTime SessionStarted   { get; set; }
        public DateTime SessionEnded     { get; set; }
        public bool     TerminatedEarly  { get; set; }

        public TimeSpan Duration => SessionEnded > SessionStarted
            ? SessionEnded - SessionStarted
            : TimeSpan.Zero;

        public List<SimulationRecord> Records { get; } = new List<SimulationRecord>();

        /// <summary>
        /// Добавляет запись о прогоне моделирования в протокол.
        /// </summary>
        public void AddRecord(
            int runNumber,
            double phosphoriteFlow, double quartzFlow, double cokeFlow, double electrodeMove,
            double concentration, double productivity, double energy, double temperature,
            double ecoCO, double ecoCO2,
            Scenario scenario)
        {
            Records.Add(new SimulationRecord
            {
                RunNumber           = runNumber,
                RunTime             = DateTime.Now,
                PhosphoriteFlow     = phosphoriteFlow,
                QuartzFlow          = quartzFlow,
                CokeFlow            = cokeFlow,
                ElectrodeMovement   = electrodeMove,
                Concentration       = concentration,
                Productivity        = productivity,
                EnergyConsumption   = energy,
                Temperature         = temperature,
                EcoCO               = ecoCO,
                EcoCO2              = ecoCO2,
                ConcOk    = concentration >= scenario.ConcMin,
                ProdOk    = productivity  >= scenario.ProdMin,
                EnergyOk  = energy        <= scenario.EnergyMax,
                TempOk    = temperature   >= scenario.TempMin && temperature <= scenario.TempMax,
                EcoOk     = ecoCO <= scenario.EcoCOMax && ecoCO2 <= scenario.EcoCO2Max,
            });
        }
    }
}
