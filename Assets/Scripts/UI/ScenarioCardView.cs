using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PhosphorTrainer.Networking;
using PhosphorTrainer.Scenarios;

namespace PhosphorTrainer.UI
{
    /// <summary>Карточка сценария в списке выбора: показывает название и сообщает о выборе.</summary>
    public sealed class ScenarioCardView : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameLabel;
        [SerializeField] private TMP_Text timeLabel;
        [SerializeField] private TMP_Text concLabel;
        [SerializeField] private TMP_Text prodLabel;
        [SerializeField] private TMP_Text energyLabel;
        [SerializeField] private TMP_Text temperatureLabel;
        [SerializeField] private Button selectButton;
        [Header("Подсветка (опционально)")]
        [SerializeField] private Image background;
        [SerializeField] private Color normalColor = new Color(0.16f, 0.16f, 0.16f, 1f);
        [SerializeField] private Color selectedColor = new Color(0.20f, 0.45f, 0.75f, 1f);

        private Action<ScenarioCardView> _onSelected;

        public ScenarioDto Scenario { get; private set; }

        private void Awake()
        {
            if (selectButton == null) selectButton = GetComponent<Button>();
            if (selectButton != null) selectButton.onClick.AddListener(RaiseSelected);
        }

        private void OnDestroy()
        {
            if (selectButton != null) selectButton.onClick.RemoveListener(RaiseSelected);
        }

        public void Bind(ScenarioDto scenario, Action<ScenarioCardView> onSelected)
        {
            Scenario = scenario;
            _onSelected = onSelected;
            if (nameLabel != null) nameLabel.text = scenario != null ? scenario.name : string.Empty;

            // Регламент карточки берём из того же маппера, что и запуск сессии,
            // чтобы показанные пределы совпадали с реально применяемыми.
            var regime = ScenarioMapper.ToScenario(scenario);
            var ci = CultureInfo.InvariantCulture;
            if (timeLabel        != null) timeLabel.text        = scenario != null ? scenario.time : string.Empty;
            if (concLabel        != null) concLabel.text        = $"P₄ ≥ {regime.ConcMin.ToString("0.#", ci)} %";
            if (prodLabel        != null) prodLabel.text        = $"≥ {regime.ProdMin.ToString("0.##", ci)} т/ч";
            if (energyLabel      != null) energyLabel.text      = $"≤ {(regime.EnergyMax / 1000.0).ToString("0.#", ci)} МВт";
            if (temperatureLabel != null) temperatureLabel.text = $"{regime.TempMin.ToString("0", ci)}–{regime.TempMax.ToString("0", ci)} °C";

            SetSelected(false);
        }

        public void SetSelected(bool selected)
        {
            if (background != null) background.color = selected ? selectedColor : normalColor;
        }

        private void RaiseSelected() => _onSelected?.Invoke(this);
    }
}
