using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using PhosphorTrainer.Networking;
using PhosphorTrainer.Session;

namespace PhosphorTrainer.Flow
{
    /// <summary>
    /// Панель входа на сцене тренажёра: подключается к серверу инструктора, авторизует
    /// обучаемого и сообщает об успехе через событие <see cref="LoggedIn"/>.
    /// </summary>
    public sealed class LoginController : MonoBehaviour
    {
        [Header("Поля ввода")]
        [SerializeField] private TMP_InputField ipField;
        [SerializeField] private TMP_InputField portField;
        [SerializeField] private TMP_InputField loginField;
        [SerializeField] private TMP_InputField passwordField;

        [Header("Кнопки")]
        [SerializeField] private Button enterButton;
        [SerializeField] private Button exitButton;

        [Header("Статус")]
        [SerializeField] private TMP_Text statusText;

        [Tooltip("Запрашивать у сервера сценарии пользователя (требует метод get_scenarios на сервере).")]
        [SerializeField] private bool fetchScenariosFromServer;

        [Tooltip("Тест без сервера: вход проходит сразу, без подключения и авторизации.")]
        [SerializeField] private bool offlineLogin;

        public event Action LoggedIn;

        private void Awake()
        {
            if (enterButton != null) enterButton.onClick.AddListener(OnEnterClicked);
            if (exitButton != null) exitButton.onClick.AddListener(OnExitClicked);
        }

        private void OnDestroy()
        {
            if (enterButton != null) enterButton.onClick.RemoveListener(OnEnterClicked);
            if (exitButton != null) exitButton.onClick.RemoveListener(OnExitClicked);
        }

        private async void OnEnterClicked() => await ConnectAndLoginAsync();

        private async Task ConnectAndLoginAsync()
        {
            if (offlineLogin)
            {
                SetStatus("Оффлайн-режим: вход без сервера.");
                LoggedIn?.Invoke();
                return;
            }

            if (!ServerEndpoint.TryParse(ipField != null ? ipField.text : null,
                                         portField != null ? portField.text : null,
                                         out var endpoint))
            {
                SetStatus("Некорректный адрес сервера.");
                return;
            }

            string login = loginField != null ? loginField.text : string.Empty;
            string password = passwordField != null ? passwordField.text : string.Empty;
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                SetStatus("Введите логин и пароль.");
                return;
            }

            SetInteractable(false);
            SetStatus($"Подключение к {endpoint}…");

            try
            {
                var result = await SessionManager.Instance.LoginAsync(endpoint, login, password, fetchScenariosFromServer);
                if (result.Success)
                {
                    SetStatus("Авторизация успешна.");
                    LoggedIn?.Invoke();
                    return;
                }
                SetStatus($"Ошибка входа: {result.Error}");
            }
            catch (Exception e)
            {
                SetStatus($"Не удалось подключиться: {e.Message}");
            }

            SetInteractable(true);
        }

        private void OnExitClicked()
        {
            Application.Quit();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        private void SetInteractable(bool value)
        {
            if (enterButton != null) enterButton.interactable = value;
        }

        private void SetStatus(string message)
        {
            if (statusText != null) statusText.text = message;
            Debug.Log($"[Login] {message}");
        }
    }
}
