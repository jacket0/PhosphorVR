namespace PhosphorTrainer.Networking
{
    /// <summary>Результат авторизации на сервере инструктора.</summary>
    public readonly struct AuthorizationResult
    {
        public bool Success { get; }
        public int UserId { get; }
        public string Error { get; }

        private AuthorizationResult(bool success, int userId, string error)
        {
            Success = success;
            UserId = userId;
            Error = error;
        }

        public static AuthorizationResult Ok(int userId) => new AuthorizationResult(true, userId, null);
        public static AuthorizationResult Fail(string error) => new AuthorizationResult(false, 0, error);
    }
}
