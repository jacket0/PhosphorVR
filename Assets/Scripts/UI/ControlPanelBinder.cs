using System;
using UnityEngine;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Связывает регуляторы пульта с управляющими воздействиями модели:
    /// расходы сырья (т/ч) и движение электрода (знак — направление, модуль — скорость, м/ч).
    /// </summary>
    public sealed class ControlPanelBinder : MonoBehaviour
    {
        [SerializeField] private SimulationController simulation;

        [Header("Регуляторы")]
        [SerializeField] private ValueStepper phosphoriteStepper;
        [SerializeField] private ValueStepper quartzStepper;
        [SerializeField] private ValueStepper cokeStepper;
        [SerializeField] private ValueStepper electrodeStepper;

        private void Awake()
        {
            if (simulation == null) simulation = FindFirstObjectByType<SimulationController>();
        }

        private void OnEnable()
        {
            Bind(phosphoriteStepper, OnPhosphorite);
            Bind(quartzStepper, OnQuartz);
            Bind(cokeStepper, OnCoke);
            Bind(electrodeStepper, OnElectrode);
            if (simulation != null) simulation.SessionStarted += SyncSteppersFromModel;
        }

        private void OnDisable()
        {
            Unbind(phosphoriteStepper, OnPhosphorite);
            Unbind(quartzStepper, OnQuartz);
            Unbind(cokeStepper, OnCoke);
            Unbind(electrodeStepper, OnElectrode);
            if (simulation != null) simulation.SessionStarted -= SyncSteppersFromModel;
        }

        // На старте сессии модель уже получила начальные расходы из сценария (БД);
        // подтягиваем их в степперы без обратного уведомления, чтобы пульт показывал
        // значения из БД, а оператор мог их менять.
        private void SyncSteppersFromModel()
        {
            if (simulation == null) return;
            if (phosphoriteStepper != null) phosphoriteStepper.SetValue(simulation.FosforiteFlow, notify: false);
            if (quartzStepper != null) quartzStepper.SetValue(simulation.QuartzFlow, notify: false);
            if (cokeStepper != null) cokeStepper.SetValue(simulation.CokeFlow, notify: false);
        }

        private void Start() => ApplyAll();

        /// <summary>Передаёт текущие значения всех регуляторов в модель.</summary>
        public void ApplyAll()
        {
            if (simulation == null) return;
            if (phosphoriteStepper != null) simulation.FosforiteFlow = phosphoriteStepper.Value;
            if (quartzStepper != null) simulation.QuartzFlow = quartzStepper.Value;
            if (cokeStepper != null) simulation.CokeFlow = cokeStepper.Value;
            if (electrodeStepper != null) ApplyElectrode(electrodeStepper.Value);
        }

        private void OnPhosphorite(double value) { if (simulation != null) simulation.FosforiteFlow = value; }
        private void OnQuartz(double value) { if (simulation != null) simulation.QuartzFlow = value; }
        private void OnCoke(double value) { if (simulation != null) simulation.CokeFlow = value; }
        private void OnElectrode(double valueMm) => ApplyElectrode(valueMm);

        // Пульт оперирует миллиметрами; в модель значение уходит в метрах (÷1000).
        // Знак задаёт направление, модуль — скорость перемещения электрода (м/ч).
        private void ApplyElectrode(double valueMm)
        {
            if (simulation == null) return;
            double meters = valueMm / 1000.0;
            simulation.ElectrodeDir = meters > 1e-9 ? 1 : (meters < -1e-9 ? -1 : 0);
            simulation.ElectrodeRate = Math.Abs(meters);
        }

        private static void Bind(ValueStepper stepper, Action<double> handler)
        {
            if (stepper != null) stepper.ValueChanged += handler;
        }

        private static void Unbind(ValueStepper stepper, Action<double> handler)
        {
            if (stepper != null) stepper.ValueChanged -= handler;
        }
    }
}
