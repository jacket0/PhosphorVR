using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using PhosphorTrainer.Networking;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Экран выбора сценария: строит карточки из полученного списка, хранит выбор и
    /// сообщает о подтверждении через <see cref="ScenarioConfirmed"/>.
    /// </summary>
    public sealed class ScenarioPickController : MonoBehaviour
    {
        [SerializeField] private Transform content;
        [SerializeField] private ScenarioCardView cardPrefab;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button closeButton;

        public event Action<ScenarioDto> ScenarioConfirmed;
        public event Action BackRequested;

        private readonly List<ScenarioCardView> _cards = new List<ScenarioCardView>();
        private ScenarioCardView _selectedCard;

        private void Awake()
        {
            if (confirmButton != null) confirmButton.onClick.AddListener(Confirm);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnDestroy()
        {
            if (confirmButton != null) confirmButton.onClick.RemoveListener(Confirm);
            if (closeButton != null) closeButton.onClick.RemoveListener(OnCloseClicked);
        }

        // Возврат в главное меню (экран входа) — обрабатывает MainSceneController.
        private void OnCloseClicked() => BackRequested?.Invoke();

        /// <summary>Перестраивает список карточек по полученным сценариям.</summary>
        public void Populate(IReadOnlyList<ScenarioDto> scenarios)
        {
            Clear();
            if (cardPrefab == null || content == null)
            {
                Debug.LogWarning("[ScenarioPick] Не назначен префаб карточки или контейнер Content.");
                return;
            }

            if (scenarios != null)
            {
                foreach (var scenario in scenarios)
                {
                    var card = Instantiate(cardPrefab, content);
                    card.Bind(scenario, OnCardSelected);
                    _cards.Add(card);
                }
            }

            if (_cards.Count > 0) OnCardSelected(_cards[0]);
            UpdateConfirm();
        }

        private void OnCardSelected(ScenarioCardView card)
        {
            if (_selectedCard != null) _selectedCard.SetSelected(false);
            _selectedCard = card;
            if (_selectedCard != null) _selectedCard.SetSelected(true);
            UpdateConfirm();
        }

        private void UpdateConfirm()
        {
            if (confirmButton != null) confirmButton.interactable = _selectedCard != null;
        }

        private void Confirm()
        {
            if (_selectedCard != null) ScenarioConfirmed?.Invoke(_selectedCard.Scenario);
        }

        private void Clear()
        {
            foreach (var card in _cards)
                if (card != null) Destroy(card.gameObject);
            _cards.Clear();
            _selectedCard = null;
        }
    }
}
