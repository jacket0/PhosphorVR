using System;
using System.Collections.Generic;
using UnityEngine;
using PhosphorModeling;
using PhosphorSimulator;

namespace PhosphorTrainer
{
    /// <summary>
    /// Драйвер математической модели фосфорной печи для Unity.
    /// Кадрово-независимый: продвигает модель по sim-времени пропорционально
    /// реальному времени (как timerTrainer_Tick + AdvanceStep в WinForms-версии).
    ///
    /// Вход (управление оператора) задаётся через публичные свойства
    /// FosforiteFlow/QuartzFlow/CokeFlow (т/ч) и ElectrodeDir/ElectrodeRate.
    /// Выход читается из CurrentStep или через событие OnStep.
    ///
    /// Визуализация (3D-сцена, графики, индикаторы) подписывается на события
    /// OnStep / OnDrain / OnFeed и НЕ связана с моделью напрямую.
    /// </summary>
    public class SimulationController : MonoBehaviour
    {
        // ── Темп симуляции ───────────────────────────────────────────────
        // 1 реальная секунда = SimHoursPerRealSecond sim-часов (в WinForms
        // это SimStepHours = 0.05 за тик ~1 с). Сценарий 120 с = 6 sim-часов.
        [Tooltip("Сколько модельных часов проходит за 1 реальную секунду")]
        public double SimHoursPerRealSecond = 0.05;

        [Tooltip("Пауза симуляции (управление можно менять, время стоит)")]
        public bool Paused = false;

        [Tooltip("Интервал выборки точек протокола обучения, sim-часов (0 — каждый шаг модели).")]
        public double ProtocolSampleHours = 0.02;

        // ── Управляющие воздействия оператора (правятся из UI/VR) ────────
        [Header("Управление (т/ч и электрод)")]
        public double FosforiteFlow = 16.5;   // т/ч
        public double QuartzFlow    = 8.2;    // т/ч
        public double CokeFlow      = 4.5;    // т/ч
        [Tooltip("Направление электрода: -1 опустить, 0 держать, +1 поднять")]
        public int    ElectrodeDir  = 0;
        [Tooltip("Скорость перемещения электрода, м/ч")]
        public double ElectrodeRate = 0.0;

        // ── Текущее состояние модели (только чтение для UI) ──────────────
        public SimulationStep CurrentStep { get; private set; }
        public Scenario CurrentScenario   { get; private set; }
        public bool Running { get; private set; }
        public double ElapsedSimHours => CurrentStep?.Time ?? 0.0;

        // История шагов для графиков (накапливается за сессию).
        public IReadOnlyList<SimulationStep> History => _history;
        private readonly List<SimulationStep> _history = new List<SimulationStep>();

        // ── События для визуализации ─────────────────────────────────────
        public event Action<SimulationStep> OnStep;   // каждый продвинутый шаг
        public event Action<SimulationStep> OnDrain;  // сработал слив
        public event Action<SimulationStep> OnFeed;   // сработала загрузка порции
        public event Action OnSessionEnded;
        public event Action SessionStarted;           // новая сессия началась (расходы уже выставлены)

        // ── Внутреннее ───────────────────────────────────────────────────
        private PhosphorModel _model;
        private ModelParameters _params;
        private double _simTimeTarget;     // до какого sim-времени догонять
        private double _sessionLimitHours; // длительность сессии в sim-часах
        private int _drainCount, _feedCount;
        public int DrainCount => _drainCount;
        public int FeedCount  => _feedCount;

        // Точки последнего прогона моделирования (вся траектория с выборкой по
        // sim-времени). Протокол ОБУЧЕНИЯ собирается на уровне MainSceneController
        // из них и реального времени за пультом, а не на каждый прогон.
        public IReadOnlyList<ProtocolData> LastRunPoints => _runPoints;
        private readonly List<ProtocolData> _runPoints = new List<ProtocolData>();
        private double _lastProtocolSampleHours;

        /// <summary>Создаёт параметры по умолчанию (как CreateDefaultParameters в WinForms).</summary>
        public static ModelParameters DefaultParameters() => new ModelParameters
        {
            H = 4, D = 7, Del = 1.3,
            I = 120, U = 456, P = 48,
            C_P4_0 = 98, T_nom = 1500, L_mpr_nom = 0.3,
            L_melt = 0.11, KPD_0 = 0.91,
            T0 = 25, Tsmelt = 1300, Craspl = 0.279,
            A_raspl = 8.82, K_bottom = 60, G_el = 35,
            H_max = 1.0, H_min = 0.6, Rho_prod = 1750,
            Mraspl_0 = 0, dt = 0.001,
        };

        private void Awake()
        {
            _params = DefaultParameters();
        }

        /// <summary>Запустить новый прогон моделирования по сценарию (параметры — опционально).</summary>
        public void StartSession(Scenario scenario, ModelParameters parameters = null)
        {
            _params = parameters ?? _params ?? DefaultParameters();

            // Начальные расходы сырья из сценария (БД) → стартовые значения пульта.
            if (_params.G_fosforit > 0) FosforiteFlow = _params.G_fosforit;
            if (_params.G_kvarzit  > 0) QuartzFlow    = _params.G_kvarzit;
            if (_params.G_coks     > 0) CokeFlow      = _params.G_coks;

            CurrentScenario = scenario;
            _model = new PhosphorModel(_params);
            CurrentStep = _model.InitialState();

            _history.Clear();
            _history.Add(CurrentStep);
            _drainCount = 0;
            _feedCount = 0;
            _simTimeTarget = 0;
            _sessionLimitHours = (scenario != null ? scenario.DurationSeconds : 120) * SimHoursPerRealSecond;

            // Накопление точек протокола начинаем заново — с начальной точки прогона.
            _runPoints.Clear();
            _lastProtocolSampleHours = -1;
            RecordRunPoint(CurrentStep, force: true);

            Running = true;
            SessionStarted?.Invoke();
            OnStep?.Invoke(CurrentStep);
        }

        public void StopSession()
        {
            if (!Running) return;
            Running = false;
            RecordRunPoint(CurrentStep, force: true);   // гарантируем финальную точку
            OnSessionEnded?.Invoke();
        }

        private void Update()
        {
            if (!Running || Paused || _model == null) return;

            // Догоняем модельное время до целевого, продвигая по DtStep.
            _simTimeTarget += Time.deltaTime * SimHoursPerRealSecond;
            if (_simTimeTarget > _sessionLimitHours)
                _simTimeTarget = _sessionLimitHours;

            var controls = new ControlInputs
            {
                G_fosforit = FosforiteFlow * 1000.0,
                G_kvarzit  = QuartzFlow    * 1000.0,
                G_coks     = CokeFlow      * 1000.0,
                K_ctrl     = ElectrodeDir,
                L_ctrl     = ElectrodeRate,
            };

            // Цикл интегрирования: пока не догнали target (шаг модели = DtStep).
            // Ограничим число шагов за кадр, чтобы не зависнуть при лагах.
            int guard = 0;
            while (CurrentStep.Time < _simTimeTarget - _model.DtStep * 0.5 && guard++ < 100000)
            {
                CurrentStep = _model.Advance(CurrentStep, controls);
                _history.Add(CurrentStep);
                RecordRunPoint(CurrentStep, force: false);   // выборка точек протокола по sim-времени

                if (CurrentStep.DrainEvent) { _drainCount++; OnDrain?.Invoke(CurrentStep); }
                if (CurrentStep.FeedEvent)  { _feedCount++;  OnFeed?.Invoke(CurrentStep); }
            }

            OnStep?.Invoke(CurrentStep);

            // _simTimeTarget жёстко ограничен _sessionLimitHours сверху, поэтому условие
            // срабатывает надёжно (в отличие от сравнения накопленного CurrentStep.Time).
            if (_simTimeTarget >= _sessionLimitHours)
                StopSession();
        }

        // Накапливает точку прогона в протокол с прореживанием по sim-времени
        // (force = записать обязательно: начальная и финальная точки).
        private void RecordRunPoint(SimulationStep s, bool force)
        {
            if (!force && _lastProtocolSampleHours >= 0 &&
                s.Time - _lastProtocolSampleHours < ProtocolSampleHours)
                return;
            _lastProtocolSampleHours = s.Time;
            _runPoints.Add(BuildDataPoint(s));
        }

        // Точка протокола из текущего шага и управляющих воздействий.
        private ProtocolData BuildDataPoint(SimulationStep s) => new ProtocolData
        {
            Time              = s.Time,
            FosforitFeedValue = FosforiteFlow,
            QuartzitFeedValue = QuartzFlow,
            CokeFeedValue     = CokeFlow,
            ElectrodeMovingValue = ElectrodeDir * ElectrodeRate,
            ConcPhosphorValue = s.C,
            ProductivityValue = s.Gprod / 1000.0,
            EnergyConsumeValue = s.Q,
            TrasplValue       = s.Temperature,
            MprValue          = s.L_mpr,
            MrasplValue       = s.MeltMass,
            CoValue           = s.Eco_CO_pct,
            Co2Value          = s.Eco_CO2_pct,
        };
    }
}
