using System;
using System.Collections.Generic;

namespace PhosphorModeling
{
    /// <summary>
    /// Протокол проведения обучения — содержит сводную информацию о сессии
    /// </summary>
    public class Protocol
    {
        public int Id { get; set; }
        public string Name { get; set; }           // название сценария
        public DateTime Date { get; set; }         // дата начала
        public DateTime EndTime { get; set; }      // время окончания
        public int IdUser { get; set; }            // идентификатор пользователя
        public int IdScenario { get; set; }        // идентификатор сценария
        public double TrainingDurationSeconds { get; set; }  // фактическая длительность обучения
        public List<ProtocolData> DataPoints { get; set; } = new List<ProtocolData>();
    }

    /// <summary>
    /// Данные протокола — критериальные показатели на каждом шаге моделирования
    /// </summary>
    public class ProtocolData
    {
        public int Id { get; set; }
        public double Time { get; set; }                  // время, ч
        public double FosforitFeedValue { get; set; }     // расход фосфорита, т/ч
        public double QuartzitFeedValue { get; set; }     // расход кварцита, т/ч
        public double CokeFeedValue { get; set; }         // расход кокса, т/ч
        public double ElectrodeMovingValue { get; set; }  // скорость движения электрода, м/ч
        public double ConcPhosphorValue { get; set; }     // концентрация P₄, %
        public double ProductivityValue { get; set; }     // производительность, т/ч
        public double EnergyConsumeValue { get; set; }    // энергопотребление, кВт
        public double TrasplValue { get; set; }           // температура расплава, °C
        public double MprValue { get; set; }              // МПР, м
        public double MrasplValue { get; set; }           // масса расплава, кг
        public double CoValue { get; set; }               // CO в вентвыбросах, %
        public double Co2Value { get; set; }              // CO₂ в вентвыбросах, %
    }

    /// <summary>
    /// Менеджер протоколов — создание и сохранение протоколов обучения
    /// </summary>
    public static class ProtocolManager
    {
        private static readonly List<Protocol> _protocols = new List<Protocol>();

        /// <summary>
        /// Создать новый протокол для сессии обучения
        /// </summary>
        public static Protocol CreateProtocol(string scenarioName, int idUser, int idScenario, double trainingDurationSeconds)
        {
            var protocol = new Protocol
            {
                Id = _protocols.Count + 1,
                Name = scenarioName,
                Date = DateTime.Now,
                IdUser = idUser,
                IdScenario = idScenario,
                TrainingDurationSeconds = trainingDurationSeconds
            };
            _protocols.Add(protocol);
            return protocol;
        }

        /// <summary>
        /// Добавить точку данных в протокол
        /// </summary>
        public static void AddDataPoint(Protocol protocol, ProtocolData data)
        {
            if (protocol != null)
            {
                data.Id = protocol.DataPoints.Count + 1;
                protocol.DataPoints.Add(data);
            }
        }

        /// <summary>
        /// Завершить протокол — установить время окончания
        /// </summary>
        public static void FinalizeProtocol(Protocol protocol)
        {
            if (protocol != null)
            {
                protocol.EndTime = DateTime.Now;
                protocol.TrainingDurationSeconds = (protocol.EndTime - protocol.Date).TotalSeconds;
            }
        }

        /// <summary>
        /// Получить все протоколы
        /// </summary>
        public static List<Protocol> GetAllProtocols() => new List<Protocol>(_protocols);

        /// <summary>
        /// Получить последний протокол
        /// </summary>
        public static Protocol GetLastProtocol() => _protocols.Count > 0 ? _protocols[_protocols.Count - 1] : null;

        /// <summary>
        /// Очистить все протоколы
        /// </summary>
        public static void ClearAll() => _protocols.Clear();
    }
}
