using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Переиспользуемый регулятор «больше/меньше»: кнопки вверх/вниз, подпись значения,
    /// настраиваемые шаг и диапазон. Сообщает об изменении через <see cref="ValueChanged"/>.
    /// </summary>
    public sealed class ValueStepper : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private Button increaseButton;
        [SerializeField] private Button decreaseButton;
        [SerializeField] private TMP_Text valueLabel;

        [Header("Диапазон")]
        [SerializeField] private double step = 0.5;
        [SerializeField] private double minValue = 0;
        [SerializeField] private double maxValue = 30;
        [SerializeField] private double initialValue;
        [SerializeField] private string format = "0.0";

        public event Action<double> ValueChanged;
        public double Value { get; private set; }

        private void Awake()
        {
            Value = Clamp(initialValue);
            if (increaseButton != null) increaseButton.onClick.AddListener(Increase);
            if (decreaseButton != null) decreaseButton.onClick.AddListener(Decrease);
            UpdateLabel();
        }

        private void OnDestroy()
        {
            if (increaseButton != null) increaseButton.onClick.RemoveListener(Increase);
            if (decreaseButton != null) decreaseButton.onClick.RemoveListener(Decrease);
        }

        public void Increase() => SetValue(Value + step);
        public void Decrease() => SetValue(Value - step);

        public void SetValue(double value, bool notify = true)
        {
            Value = Clamp(value);
            UpdateLabel();
            if (notify) ValueChanged?.Invoke(Value);
        }

        private double Clamp(double value) =>
            value < minValue ? minValue : (value > maxValue ? maxValue : value);

        private void UpdateLabel()
        {
            if (valueLabel != null) valueLabel.text = Value.ToString(format, CultureInfo.InvariantCulture);
        }
    }
}
