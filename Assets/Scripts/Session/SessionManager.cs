using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using PhosphorModeling;
using PhosphorTrainer.Networking;

namespace PhosphorTrainer.Session
{
    /// <summary>
    /// Состояние сессии обучения, переживающее смену сцен: адрес сервера,
    /// идентификатор пользователя и назначенные ему сценарии. Соединение с
    /// сервером инструктора открывается на время диалога и закрывается сразу.
    /// </summary>
    public sealed class SessionManager : MonoBehaviour
    {
        private static SessionManager _instance;

        public static SessionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var host = new GameObject(nameof(SessionManager));
                    _instance = host.AddComponent<SessionManager>();
                    DontDestroyOnLoad(host);
                }
                return _instance;
            }
        }

        public ServerEndpoint Endpoint { get; private set; }
        public int UserId { get; private set; }
        public string Login { get; private set; }
        public IReadOnlyList<ScenarioDto> Scenarios { get; private set; } = Array.Empty<ScenarioDto>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Подключается к серверу, авторизует обучаемого и (опционально) загружает
        /// назначенные сценарии. Возвращает результат авторизации.
        /// </summary>
        public async Task<AuthorizationResult> LoginAsync(ServerEndpoint endpoint, string login, string password, bool fetchScenarios)
        {
            Endpoint = endpoint;
            Login = login;

            using var client = new InstructorClient(endpoint);
            await client.ConnectAsync();

            var auth = await client.AuthorizeAsync(login, password);
            if (!auth.Success)
                return auth;

            UserId = auth.UserId;

            if (fetchScenarios)
            {
                try { Scenarios = await client.GetScenariosAsync(UserId); }
                catch (Exception e) { Debug.LogWarning($"[Session] Сценарии не получены: {e.Message}"); }
            }

            return auth;
        }

        /// <summary>Открывает соединение, отправляет протокол обучения и закрывает его.</summary>
        public async Task<bool> UploadProtocolAsync(Protocol protocol)
        {
            if (protocol == null) return false;
            try
            {
                using var client = new InstructorClient(Endpoint);
                await client.ConnectAsync();
                return await client.UploadProtocolAsync(protocol);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Session] Протокол не отправлен: {e.Message}");
                return false;
            }
        }
    }
}
