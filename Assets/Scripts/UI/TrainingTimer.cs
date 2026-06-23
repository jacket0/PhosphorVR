using System;
using TMPro;
using UnityEngine;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Обратный отсчёт времени ОБУЧЕНИЯ (длительность из сценария, напр. 01:00:00) в
    /// реальном времени: показывает, сколько обучаемому осталось учиться. Не связан со
    /// временем моделирования — тикает непрерывно, пока идёт сессия за пультом.
    /// </summary>
    public sealed class TrainingTimer : MonoBehaviour
    {
        [SerializeField] private TMP_Text label;

        private double _remainingSeconds;
        private bool _running;

        /// <summary>Время обучения вышло (счётчик дошёл до нуля).</summary>
        public event Action Elapsed;

        public bool IsRunning => _running;

        /// <summary>Запускает обратный отсчёт на заданное число секунд.</summary>
        public void Begin(double seconds)
        {
            _remainingSeconds = seconds > 0 ? seconds : 0;
            _running = true;
            Render();
        }

        /// <summary>Останавливает отсчёт (например, при завершении обучения).</summary>
        public void StopTimer() => _running = false;

        private void Update()
        {
            if (!_running) return;

            _remainingSeconds -= Time.deltaTime;
            if (_remainingSeconds <= 0)
            {
                _remainingSeconds = 0;
                _running = false;
                Render();
                Elapsed?.Invoke();
                return;
            }

            Render();
        }

        private void Render()
        {
            if (label == null) return;
            var span = TimeSpan.FromSeconds(Math.Ceiling(Math.Max(0, _remainingSeconds)));
            label.text = $"{(int)span.TotalHours:00}:{span.Minutes:00}:{span.Seconds:00}";
        }
    }
}
