using System.Collections.Generic;
using PhosphorTrainer.Networking;

namespace PhosphorTrainer.Scenarios
{
    /// <summary>
    /// Локальный запасной сценарий — когда сервер недоступен (оффлайн-тест).
    /// Параметры не задаются: <see cref="ScenarioMapper"/> подставит значения
    /// модели и регламента по умолчанию (соответствуют засеянному в БД режиму).
    /// </summary>
    public static class ScenarioCatalog
    {
        public static IReadOnlyList<ScenarioDto> Fallback() => new List<ScenarioDto>
        {
            new ScenarioDto
            {
                id = 0,
                name = "Регламентный режим (локально)",
                time = "01:00:00",
                id_equipment = 1,
                id_material = 1,
                id_technological = 1,
                parameters = new List<ScenarioParamDto>(),
            }
        };
    }
}
