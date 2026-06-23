using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhosphorModeling;

namespace PhosphorTrainer.Networking
{
    /// <summary>
    /// Связь с сервером инструктора по TCP. Сообщения — JSON-строки, разделённые
    /// '\n'; ответ сопоставляется запросу по полю request_id.
    /// </summary>
    public sealed class InstructorClient : IInstructorClient
    {
        private readonly ServerEndpoint _endpoint;
        private readonly int _connectTimeoutMs;
        private readonly int _responseTimeoutMs;

        private readonly byte[] _buffer = new byte[4096];
        private readonly char[] _chars = new char[4096];
        private readonly Decoder _decoder = Encoding.UTF8.GetDecoder();
        private string _pending = string.Empty;

        private TcpClient _client;
        private NetworkStream _stream;

        public InstructorClient(ServerEndpoint endpoint, int connectTimeoutMs = 5000, int responseTimeoutMs = 5000)
        {
            _endpoint = endpoint;
            _connectTimeoutMs = connectTimeoutMs;
            _responseTimeoutMs = responseTimeoutMs;
        }

        public bool IsConnected => _client is { Connected: true };

        public async Task ConnectAsync(CancellationToken cancellationToken = default)
        {
            Disconnect();
            _client = new TcpClient();
            try
            {
                var connectTask = _client.ConnectAsync(_endpoint.Host, _endpoint.Port);
                if (await Task.WhenAny(connectTask, Task.Delay(_connectTimeoutMs, cancellationToken)) != connectTask)
                    throw new TimeoutException($"Превышено время подключения к {_endpoint}.");

                await connectTask;
                _stream = _client.GetStream();
                _pending = string.Empty;
                _decoder.Reset();
            }
            catch
            {
                Disconnect();
                throw;
            }
        }

        public void Disconnect()
        {
            _stream?.Dispose();
            _client?.Close();
            _stream = null;
            _client = null;
        }

        public void Dispose() => Disconnect();

        public async Task<AuthorizationResult> AuthorizeAsync(string login, string password, CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("authorization", new Dictionary<string, object>
            {
                ["login"] = login,
                ["password"] = password,
            }, cancellationToken);

            if (!(response.Value<bool?>("success") ?? false))
                return AuthorizationResult.Fail(response.Value<string>("error") ?? "Отказано в доступе.");

            return AuthorizationResult.Ok(response.Value<int?>("user_id") ?? 0);
        }

        public async Task<IReadOnlyList<ScenarioDto>> GetScenariosAsync(int userId, CancellationToken cancellationToken = default)
        {
            var response = await SendRequestAsync("get_scenarios", new Dictionary<string, object>
            {
                ["user_id"] = userId,
            }, cancellationToken);

            bool success = response.Value<bool?>("success") ?? false;
            if (success && response["scenarios"] is JArray scenarios)
                return scenarios.ToObject<List<ScenarioDto>>() ?? new List<ScenarioDto>();

            return Array.Empty<ScenarioDto>();
        }

        public async Task<bool> UploadProtocolAsync(Protocol protocol, CancellationToken cancellationToken = default)
        {
            if (protocol == null) return false;

            var response = await SendRequestAsync("save_protocol", new Dictionary<string, object>
            {
                ["user_id"] = protocol.IdUser,
                ["protocol"] = JObject.FromObject(protocol),
            }, cancellationToken);

            return response.Value<bool?>("success") ?? false;
        }

        private async Task<JObject> SendRequestAsync(string method, IDictionary<string, object> payload, CancellationToken cancellationToken)
        {
            if (_stream == null)
                throw new InvalidOperationException("Нет соединения с сервером инструктора.");

            string requestId = Guid.NewGuid().ToString("N");
            payload["method"] = method;
            payload["request_id"] = requestId;

            byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(payload, Formatting.None) + "\n");
            await _stream.WriteAsync(bytes, 0, bytes.Length, cancellationToken);
            await _stream.FlushAsync(cancellationToken);

            return await ReadResponseAsync(requestId, cancellationToken);
        }

        private async Task<JObject> ReadResponseAsync(string requestId, CancellationToken cancellationToken)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(_responseTimeoutMs);

            while (true)
            {
                string line = ExtractLine();
                if (line != null)
                {
                    JObject json = TryParseObject(line);
                    if (json != null && json.Value<string>("request_id") == requestId)
                        return json;
                    continue;
                }

                int read = await _stream.ReadAsync(_buffer, 0, _buffer.Length, cts.Token);
                if (read == 0)
                    throw new IOException("Сервер закрыл соединение.");

                int count = _decoder.GetChars(_buffer, 0, read, _chars, 0);
                _pending += new string(_chars, 0, count);
            }
        }

        private string ExtractLine()
        {
            while (true)
            {
                int newLine = _pending.IndexOf('\n');
                if (newLine < 0) return null;

                string line = _pending.Substring(0, newLine).Trim();
                _pending = _pending.Substring(newLine + 1);
                if (line.Length > 0) return line;
            }
        }

        private static JObject TryParseObject(string line)
        {
            try { return JObject.Parse(line); }
            catch (JsonException) { return null; }
        }
    }
}
