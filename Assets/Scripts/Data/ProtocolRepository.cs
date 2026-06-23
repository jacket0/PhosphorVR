// ВНИМАНИЕ: этот файл компилируется ТОЛЬКО когда задан scripting-define
// PHOSPHOR_SQLITE (Project Settings → Player → Scripting Define Symbols).
// Пока пакет sqlite-net не установлен — файл неактивен и НЕ ломает сборку.
// Порядок включения см. в UNITY_HANDOFF.md, раздел «SQLite».
#if PHOSPHOR_SQLITE
using System;
using System.Collections.Generic;
using System.Linq;
using SQLite;                 // пакет sqlite-net (sqlite-net-pcl) + нативный e_sqlite3
using PhosphorModeling;

namespace PhosphorTrainer.Data
{
    // ── Таблицы БД (аннотации sqlite-net держим ОТДЕЛЬНО от чистого ядра) ──
    // Доменные Protocol/ProtocolData из ядра остаются POCO без зависимостей;
    // здесь — их «строковые» представления для SQLite, и маппинг между ними.

    [Table("Users")]
    public class DbUser
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public string Name { get; set; }
        public string Role { get; set; }   // напр. "оператор-обучаемый"
    }

    [Table("Scenarios")]
    public class DbScenario
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        public string Name { get; set; }
        public double ConcMin { get; set; }
        public double ProdMin { get; set; }
        public double EnergyMax { get; set; }
        public double TempMin { get; set; }
        public double TempMax { get; set; }
        public double EcoCOMax { get; set; }
        public double EcoCO2Max { get; set; }
    }

    [Table("Protocols")]
    public class DbProtocol
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int IdUser { get; set; }
        [Indexed] public int IdScenario { get; set; }
        public string Name { get; set; }
        public DateTime Date { get; set; }
        public DateTime EndTime { get; set; }
        public double TrainingDurationSeconds { get; set; }
    }

    [Table("ProtocolData")]
    public class DbProtocolData
    {
        [PrimaryKey, AutoIncrement] public int Id { get; set; }
        [Indexed] public int IdProtocol { get; set; }
        public double Time { get; set; }
        public double FosforitFeedValue { get; set; }
        public double QuartzitFeedValue { get; set; }
        public double CokeFeedValue { get; set; }
        public double ElectrodeMovingValue { get; set; }
        public double ConcPhosphorValue { get; set; }
        public double ProductivityValue { get; set; }
        public double EnergyConsumeValue { get; set; }
        public double TrasplValue { get; set; }
    }

    /// <summary>
    /// Доступ к SQLite-БД тренажёра. Переиспользуется и в Unity, и в WinForms.
    /// В Unity путь к файлу обычно Application.persistentDataPath + "/phosphor.db".
    /// </summary>
    public class ProtocolRepository : IDisposable
    {
        private readonly SQLiteConnection _db;

        public ProtocolRepository(string dbPath)
        {
            _db = new SQLiteConnection(dbPath);
            _db.CreateTable<DbUser>();
            _db.CreateTable<DbScenario>();
            _db.CreateTable<DbProtocol>();
            _db.CreateTable<DbProtocolData>();
        }

        // ── Пользователи ─────────────────────────────────────────────────
        public int EnsureUser(string name, string role = "обучаемый")
        {
            var existing = _db.Table<DbUser>().FirstOrDefault(u => u.Name == name);
            if (existing != null) return existing.Id;
            var u = new DbUser { Name = name, Role = role };
            _db.Insert(u);
            return u.Id;
        }

        // ── Сценарии (синхронизация регламента из ScenarioData) ──────────
        public int EnsureScenario(Scenario s)
        {
            var existing = _db.Table<DbScenario>().FirstOrDefault(x => x.Name == s.Name);
            if (existing != null) return existing.Id;
            var row = new DbScenario
            {
                Name = s.Name,
                ConcMin = s.ConcMin, ProdMin = s.ProdMin, EnergyMax = s.EnergyMax,
                TempMin = s.TempMin, TempMax = s.TempMax,
                EcoCOMax = s.EcoCOMax, EcoCO2Max = s.EcoCO2Max,
            };
            _db.Insert(row);
            return row.Id;
        }

        // ── Сохранение протокола (шапка + временной ряд одной транзакцией) ──
        public int SaveProtocol(Protocol p, int idUser, int idScenario)
        {
            int protocolId = 0;
            _db.RunInTransaction(() =>
            {
                var head = new DbProtocol
                {
                    IdUser = idUser,
                    IdScenario = idScenario,
                    Name = p.Name,
                    Date = p.Date,
                    EndTime = p.EndTime,
                    TrainingDurationSeconds = p.TrainingDurationSeconds,
                };
                _db.Insert(head);
                protocolId = head.Id;

                var rows = p.DataPoints.Select(d => new DbProtocolData
                {
                    IdProtocol = protocolId,
                    Time = d.Time,
                    FosforitFeedValue = d.FosforitFeedValue,
                    QuartzitFeedValue = d.QuartzitFeedValue,
                    CokeFeedValue = d.CokeFeedValue,
                    ElectrodeMovingValue = d.ElectrodeMovingValue,
                    ConcPhosphorValue = d.ConcPhosphorValue,
                    ProductivityValue = d.ProductivityValue,
                    EnergyConsumeValue = d.EnergyConsumeValue,
                    TrasplValue = d.TrasplValue,
                }).ToList();
                _db.InsertAll(rows);
            });
            return protocolId;
        }

        // ── Чтение ───────────────────────────────────────────────────────
        public List<DbProtocol> GetProtocols(int idUser) =>
            _db.Table<DbProtocol>().Where(x => x.IdUser == idUser)
               .OrderByDescending(x => x.Date).ToList();

        public List<DbProtocol> GetAllProtocols() =>
            _db.Table<DbProtocol>().OrderByDescending(x => x.Date).ToList();

        public List<DbProtocolData> GetProtocolData(int idProtocol) =>
            _db.Table<DbProtocolData>().Where(x => x.IdProtocol == idProtocol)
               .OrderBy(x => x.Time).ToList();

        public void Dispose() => _db?.Dispose();
    }
}
#endif
