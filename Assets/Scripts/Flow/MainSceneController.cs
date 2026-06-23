using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PhosphorModeling;
using PhosphorTrainer.Networking;
using PhosphorTrainer.Scenarios;
using PhosphorTrainer.Session;
using PhosphorTrainer.UI;

namespace PhosphorTrainer.Flow
{
    /// <summary>
    /// Координатор сцены: вход → выбор сценария → пульт. Обучение — это всё время за
    /// пультом по выбранному сценарию (внутри обучаемый может запускать моделирование
    /// несколько раз). По кнопке «Завершить обучение» формируется ОДИН протокол
    /// обучения (реальное время за пультом + данные последнего моделирования) и
    /// отправляется инструктору.
    /// </summary>
    public sealed class MainSceneController : MonoBehaviour
    {
        [Header("Экраны")]
        [SerializeField] private LoginController login;
        [SerializeField] private ScenarioPickController scenarioPick;
        [SerializeField] private GameObject loginRoot;
        [SerializeField] private GameObject scenarioPickRoot;
        [SerializeField] private GameObject pulpitRoot;

        [Header("Тренажёр")]
        [SerializeField] private SimulationController simulation;
        [SerializeField] private SimulationHud hud;
        [SerializeField] private Button startButton;
        [SerializeField] private Button finishButton;
        [SerializeField] private TrainingTimer trainingTimer;

        private ScenarioDto _selectedScenario;
        private DateTime _trainingStarted;
        private bool _trainingActive;

        private void Awake()
        {
            if (simulation == null) simulation = FindFirstObjectByType<SimulationController>();
            if (login == null) login = FindFirstObjectByType<LoginController>();
            if (scenarioPick == null) scenarioPick = FindFirstObjectByType<ScenarioPickController>();
            if (startButton != null) startButton.onClick.AddListener(StartSession);
            if (finishButton != null) finishButton.onClick.AddListener(FinishTraining);

            if (login != null) login.LoggedIn += ShowScenarioPick;
            else Debug.LogWarning("[MainScene] LoginController не найден — выбор сценария не откроется после входа.");

            if (scenarioPick != null) scenarioPick.ScenarioConfirmed += OnScenarioConfirmed;
            else Debug.LogWarning("[MainScene] ScenarioPickController не найден.");

            if (scenarioPick != null) scenarioPick.BackRequested += ShowLogin;

            // Время обучения вышло → автоматически завершаем обучение.
            if (trainingTimer != null) trainingTimer.Elapsed += FinishTraining;

            ShowLogin();
        }

        private void OnDestroy()
        {
            if (startButton != null) startButton.onClick.RemoveListener(StartSession);
            if (finishButton != null) finishButton.onClick.RemoveListener(FinishTraining);
            if (login != null) login.LoggedIn -= ShowScenarioPick;
            if (scenarioPick != null) scenarioPick.ScenarioConfirmed -= OnScenarioConfirmed;
            if (scenarioPick != null) scenarioPick.BackRequested -= ShowLogin;
            if (trainingTimer != null) trainingTimer.Elapsed -= FinishTraining;
        }

        private void ShowLogin() => SetScreens(loginActive: true, pickActive: false, pulpitActive: false);

        private void ShowScenarioPick()
        {
            Debug.Log("[MainScene] Вход выполнен — открываю выбор сценария.");
            var scenarios = SessionManager.Instance.Scenarios;
            if (scenarios == null || scenarios.Count == 0) scenarios = ScenarioCatalog.Fallback();
            if (scenarioPick != null) scenarioPick.Populate(scenarios);
            SetScreens(loginActive: false, pickActive: true, pulpitActive: false);
        }

        private void OnScenarioConfirmed(ScenarioDto scenario)
        {
            _selectedScenario = scenario;
            Debug.Log($"[MainScene] Выбран сценарий «{scenario?.name}» — показываю пульт.");
            // Обучение начинается с момента входа за пульт.
            _trainingStarted = DateTime.Now;
            _trainingActive = true;

            // Запускаем обратный отсчёт времени обучения (длительность из сценария).
            if (trainingTimer != null)
                trainingTimer.Begin(ScenarioMapper.ToScenario(scenario).TrainingDurationSeconds);

            SetScreens(loginActive: false, pickActive: false, pulpitActive: true);
        }

        private void SetScreens(bool loginActive, bool pickActive, bool pulpitActive)
        {
            if (loginRoot != null) loginRoot.SetActive(loginActive);
            else Debug.LogWarning("[MainScene] Login Root не назначен.");
            if (scenarioPickRoot != null) scenarioPickRoot.SetActive(pickActive);
            else Debug.LogWarning("[MainScene] Scenario Pick Root не назначен.");
            if (pulpitRoot != null) pulpitRoot.SetActive(pulpitActive);
            else Debug.LogWarning("[MainScene] Pulpit Root не назначен.");
        }

        /// <summary>Запускает прогон моделирования (кнопка «Запустить моделирование»).</summary>
        public void StartSession()
        {
            if (simulation == null || simulation.Running) return;

            var scenario = ScenarioMapper.ToScenario(_selectedScenario);
            var parameters = ScenarioMapper.ToParameters(_selectedScenario);

            if (hud != null) hud.BeginSession(scenario);
            simulation.StartSession(scenario, parameters);
        }

        /// <summary>
        /// Завершает обучение: останавливает моделирование, формирует и отправляет
        /// протокол обучения (время за пультом + данные последнего моделирования),
        /// возвращает к выбору сценария.
        /// </summary>
        public async void FinishTraining()
        {
            if (!_trainingActive || simulation == null) return;
            _trainingActive = false;

            if (trainingTimer != null) trainingTimer.StopTimer();
            if (simulation.Running) simulation.StopSession();

            var points = simulation.LastRunPoints;
            var end = DateTime.Now;
            var protocol = new Protocol
            {
                Name = _selectedScenario?.name ?? "Обучение",
                IdUser = SessionManager.Instance.UserId,
                IdScenario = _selectedScenario != null ? _selectedScenario.id : 0,
                Date = _trainingStarted,
                EndTime = end,
                TrainingDurationSeconds = (end - _trainingStarted).TotalSeconds,
                DataPoints = points != null && points.Count > 0
                    ? new List<ProtocolData>(points)
                    : new List<ProtocolData>(),
            };

            await SessionManager.Instance.UploadProtocolAsync(protocol);
            ShowScenarioPick();
        }
    }
}
