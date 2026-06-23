using System.Collections.Generic;

namespace PhosphorTrainer.Networking
{
    /// <summary>
    /// Сценарий в том виде, как его присылает сервер инструктора: метаданные
    /// (колонки таблицы Scenarios) плюс полный EAV-набор параметров — оборудование,
    /// сырьё и технологический режим — для загрузки в мат-модель на клиенте.
    /// </summary>
    public sealed class ScenarioDto
    {
        public int id { get; set; }
        public string name { get; set; }
        public string time { get; set; }
        public int id_equipment { get; set; }
        public int id_material { get; set; }
        public int id_technological { get; set; }
        public List<ScenarioParamDto> parameters { get; set; }
    }

    /// <summary>
    /// Один параметр сценария: имя (ключ маппинга в модель) + номинал и
    /// регламентные границы (min/max могут отсутствовать).
    /// </summary>
    public sealed class ScenarioParamDto
    {
        public string name { get; set; }
        public double value { get; set; }
        public double? min { get; set; }
        public double? max { get; set; }
    }
}
