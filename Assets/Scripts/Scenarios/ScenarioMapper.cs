using System;
using System.Collections.Generic;
using System.Globalization;
using PhosphorModeling;
using PhosphorTrainer.Networking;

namespace PhosphorTrainer.Scenarios
{
    /// <summary>
    /// Преобразует серверный <see cref="ScenarioDto"/> (EAV-набор параметров) в
    /// модельные параметры печи <see cref="ModelParameters"/> и в регламент
    /// <see cref="Scenario"/>. Соответствие — по точному имени параметра:
    /// имена оборудования/сырья совпадают с полями модели, технологический
    /// режим задаёт начальное состояние (value) и регламентные границы (min/max).
    /// </summary>
    public static class ScenarioMapper
    {
        // Имена технологических параметров (русские, как в справочнике Parameters).
        private const string ParamConc       = "Концентрация P₄";
        private const string ParamTemp       = "Температура расплава";
        private const string ParamMpr        = "МПР";
        private const string ParamProd       = "Производительность";
        private const string ParamEnergy     = "Энергопотребление";
        private const string ParamCO         = "CO";
        private const string ParamCO2        = "CO₂";
        private const string ParamMeltHeight = "Нач. высота расплава";

        public static Scenario ToScenario(ScenarioDto dto)
        {
            var defaults = ScenarioData.GetScenarios()[0];
            if (dto == null) return defaults;

            var byName = IndexByName(dto.parameters);
            return new Scenario
            {
                Name        = dto.name,
                Description = defaults.Description,
                // Длительность одного моделирования фиксирована (модель ускорена, ≈2 мин);
                // из БД берём длительность обучения (поле time) — за неё моделирование
                // можно запускать несколько раз.
                DurationSeconds         = defaults.DurationSeconds,
                TrainingDurationSeconds = ParseSeconds(dto.time),
                ConcMin   = Min(byName, ParamConc,   defaults.ConcMin),
                ProdMin   = Min(byName, ParamProd,   defaults.ProdMin),
                EnergyMax = Max(byName, ParamEnergy, defaults.EnergyMax / 1000.0) * 1000.0, // МВт → кВт
                TempMin   = Min(byName, ParamTemp,   defaults.TempMin),
                TempMax   = Max(byName, ParamTemp,   defaults.TempMax),
                EcoCOMax  = Max(byName, ParamCO,     defaults.EcoCOMax),
                EcoCO2Max = Max(byName, ParamCO2,    defaults.EcoCO2Max),
            };
        }

        public static ModelParameters ToParameters(ScenarioDto dto)
        {
            var p = SimulationController.DefaultParameters();
            if (dto?.parameters == null) return p;

            foreach (var param in dto.parameters)
            {
                if (string.IsNullOrEmpty(param?.name)) continue;
                Apply(p, param.name, param.value);
            }
            return p;
        }

        // Соответствие имя параметра → поле модели. Имена оборудования и сырья
        // совпадают с полями PhosphorModel; технологический режим задаёт
        // начальное состояние печи своим номиналом (value).
        private static void Apply(ModelParameters p, string name, double value)
        {
            switch (name)
            {
                // Оборудование РКЗ-48
                case "D":                 p.D       = value; break;
                case "H":                 p.H       = value; break;
                case "D_el":              p.Del     = value; break;
                case "I":                 p.I       = value; break;
                case "U":                 p.U       = value; break;
                case "P":                 p.P       = value; break;
                case "Cos_phi":           p.Cos_phi = value; break;
                case "R_nom":             p.R_nom   = value; break;
                case "E_nom":             p.E_nom   = value; break;
                case "K_Rl":              p.K_Rl    = value; break;
                case "K_Rt":              p.K_Rt    = value; break;
                case "K_Rc":              p.K_Rc    = value; break;
                case "K_Et":              p.K_Et    = value; break;
                case "K_Ec":              p.K_Ec    = value; break;
                case "A_raspl":           p.A_raspl = value; break;
                case "K_bottom":          p.K_bottom = value; break;
                case "P_melt":            p.P_melt  = value; break;
                case "H_max":             p.H_max   = value; break;
                case "H_min":             p.H_min   = value; break;
                case ParamMeltHeight:     p.L_melt  = value; break; // нач. высота расплава — к H_max/H_min
                case "G_el":              p.G_el    = value; break;
                case "Rho_el":            p.Rho_el  = value; break;
                case "KPD_0":             p.KPD_0   = value; break;
                case "KPD_max":           p.KPD_max = value; break;
                case "EcoLeakFactor_CO":  p.EcoLeakFactor_CO  = value; break;
                case "EcoLeakFactor_CO2": p.EcoLeakFactor_CO2 = value; break;
                case "EcoVentAirRate":    p.EcoVentAirRate    = value; break;
                case "K_CO2_reduction":   p.K_CO2_reduction   = value; break;

                // Сырьё «Фосфоритная шихта»
                case "K":         p.K         = value; break;
                case "Ks":        p.Ks        = value; break;
                case "Cprod_nom": p.Cprod_nom = value; break;
                case "Craspl":    p.Craspl    = value; break;
                case "Rho_prod":  p.Rho_prod  = value; break;
                // Начальные расходы сырья для пульта (т/ч)
                case "G_fosforit": p.G_fosforit = value; break;
                case "G_kvarzit":  p.G_kvarzit  = value; break;
                case "G_coks":     p.G_coks     = value; break;

                // Технологический режим — начальное состояние печи (номинал)
                case ParamConc:       p.C_P4_0    = value; break;
                case ParamTemp:       p.T_nom     = value; break;
                case ParamMpr:        p.L_mpr_nom = value; break;
            }
        }

        private static IReadOnlyDictionary<string, ScenarioParamDto> IndexByName(IEnumerable<ScenarioParamDto> parameters)
        {
            var map = new Dictionary<string, ScenarioParamDto>();
            if (parameters != null)
                foreach (var param in parameters)
                    if (!string.IsNullOrEmpty(param?.name)) map[param.name] = param;
            return map;
        }

        private static double Min(IReadOnlyDictionary<string, ScenarioParamDto> map, string name, double fallback)
            => map.TryGetValue(name, out var p) && p.min.HasValue ? p.min.Value : fallback;

        private static double Max(IReadOnlyDictionary<string, ScenarioParamDto> map, string name, double fallback)
            => map.TryGetValue(name, out var p) && p.max.HasValue ? p.max.Value : fallback;

        // Поле time принимает формат "ЧЧ:ММ:СС" или число секунд.
        private static int ParseSeconds(string time)
        {
            if (string.IsNullOrWhiteSpace(time)) return 3600;
            if (TimeSpan.TryParse(time, CultureInfo.InvariantCulture, out var span)) return (int)span.TotalSeconds;
            if (int.TryParse(time, out var seconds)) return seconds;
            return 3600;
        }
    }
}
