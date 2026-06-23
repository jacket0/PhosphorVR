using TMPro;
using UnityEngine;

namespace PhosphorTrainer.UI
{
    /// <summary>
    /// Делает поле ввода активной целью экранной клавиатуры при получении фокуса,
    /// чтобы один общий OSK-keyboard заполнял несколько полей (IP, порт, логин, пароль).
    /// </summary>
    [RequireComponent(typeof(TMP_InputField))]
    public sealed class KeyboardInputTarget : MonoBehaviour
    {
        [SerializeField] private KeyboardScript keyboard;
        [SerializeField] private TMP_InputField field;

        private void Awake()
        {
            if (field == null) field = GetComponent<TMP_InputField>();
        }

        private void OnEnable()
        {
            if (field != null) field.onSelect.AddListener(HandleSelect);
        }

        private void OnDisable()
        {
            if (field != null) field.onSelect.RemoveListener(HandleSelect);
        }

        private void HandleSelect(string _)
        {
            if (keyboard != null) keyboard.SetTarget(field);
        }
    }
}
