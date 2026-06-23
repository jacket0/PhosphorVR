namespace PhosphorTrainer.Networking
{
    /// <summary>Адрес сервера инструктора (хост и порт).</summary>
    public readonly struct ServerEndpoint
    {
        public const int DefaultPort = 5000;

        public string Host { get; }
        public int Port { get; }

        public ServerEndpoint(string host, int port)
        {
            Host = host;
            Port = port;
        }

        /// <summary>
        /// Разбирает адрес из полей ввода. Хост может содержать порт ("ip:port");
        /// если порт нигде не указан — берётся <see cref="DefaultPort"/>.
        /// </summary>
        public static bool TryParse(string host, string port, out ServerEndpoint endpoint)
        {
            endpoint = default;
            if (string.IsNullOrWhiteSpace(host)) return false;
            host = host.Trim();

            int parsedPort = DefaultPort;
            int colon = host.LastIndexOf(':');
            if (colon >= 0)
            {
                if (!int.TryParse(host.Substring(colon + 1), out parsedPort)) return false;
                host = host.Substring(0, colon);
            }
            else if (!string.IsNullOrWhiteSpace(port) && !int.TryParse(port.Trim(), out parsedPort))
            {
                return false;
            }

            if (host.Length == 0) return false;
            endpoint = new ServerEndpoint(host, parsedPort);
            return true;
        }

        public override string ToString() => $"{Host}:{Port}";
    }
}
