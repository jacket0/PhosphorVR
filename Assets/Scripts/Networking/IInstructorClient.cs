using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PhosphorModeling;

namespace PhosphorTrainer.Networking
{
    /// <summary>
    /// Клиент связи с сервером инструктора (TCP, JSON-сообщения с разделителем '\n').
    /// Соединение открывается на время диалога и закрывается явно.
    /// </summary>
    public interface IInstructorClient : IDisposable
    {
        bool IsConnected { get; }

        Task ConnectAsync(CancellationToken cancellationToken = default);
        void Disconnect();

        Task<AuthorizationResult> AuthorizeAsync(string login, string password, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ScenarioDto>> GetScenariosAsync(int userId, CancellationToken cancellationToken = default);
        Task<bool> UploadProtocolAsync(Protocol protocol, CancellationToken cancellationToken = default);
    }
}
