namespace PhosphorModeling
{
    public class ModelParameters
    {
        // ── Геометрия оборудования ────────────────────────────────────────
        public double H      { get; set; }  // высота ванны, м
        public double D      { get; set; }  // диаметр ванны, м
        public double Del    { get; set; }  // диаметр электрода, м

        // ── Электрические параметры ───────────────────────────────────────
        public double I       { get; set; }  // номинальный ток электрода, кА
        public double U       { get; set; }  // рабочее напряжение, В (справочно)
        public double P       { get; set; }  // мощность трансформатора, МВА (справочно)
        public double Cos_phi { get; set; }  // коэффициент мощности
        public double R_nom   { get; set; }  // номинальное сопротивление ванны, мОм
        public double E_nom   { get; set; }  // номинальная ЭДС, В
        public double K_Rl    { get; set; }  // чувствительность R к МПР, мОм/м
        public double K_Rt    { get; set; }  // чувствительность R к температуре, мОм/°C
        public double K_Rc    { get; set; }  // чувствительность R к концентрации, мОм/%
        public double K_Et    { get; set; }  // чувствительность E к температуре, В/°C
        public double K_Ec    { get; set; }  // множитель E по ln(C/Cnom), В
        public double P_melt  { get; set; }  // удельное сопротивление расплава, Ом·м

        // ── Начальное состояние расплава ──────────────────────────────────
        public double C_P4_0    { get; set; }  // начальная концентрация P₄, %
        public double T_nom     { get; set; }  // номинальная температура, °C
        public double L_mpr_nom { get; set; }  // номинальное МПР, м
        public double L_melt    { get; set; }  // начальная высота расплава, м
        public double Mraspl_0  { get; set; }  // начальная масса расплава, кг
        public double KPD_0     { get; set; }  // нижняя граница КПД
        public double KPD_max   { get; set; }  // верхняя граница КПД
        public double Cprod_nom { get; set; }  // номинальная чистота продукта, %

        // ── Шихтовые коэффициенты выхода продукта ─────────────────────────
        public double K  { get; set; }  // коэффициент выхода P₄, кг/(кА·ч)
        public double Ks { get; set; }  // коэффициент шихты

        // ── Начальные расходы сырья для пульта (т/ч) ──────────────────────
        public double G_fosforit { get; set; }  // начальный расход фосфорита, т/ч
        public double G_kvarzit  { get; set; }  // начальный расход кварцита, т/ч
        public double G_coks     { get; set; }  // начальный расход кокса, т/ч

        // ── Цикл «накопление – слив» и электрод ───────────────────────────
        // Масса расплава теперь рассчитывается по формуле m_raspl = S·h·ρ:
        //   • H_max — допустимая высота расплава, м (~1 м для РКЗ-48);
        //   • H_min — остаточная высота после слива шлака, м.
        // Rho_prod (плотность готового расплава) и S_bath берутся из модели.
        public double H_max      { get; set; }  // допустимая высота расплава, м
        public double H_min      { get; set; }  // остаточная высота после слива, м
        public double Rho_prod   { get; set; }  // плотность расплава, кг/м³
        public double Rho_el     { get; set; }  // плотность электродной массы, кг/м³
        public double G_el       { get; set; }  // суммарный расход электродной массы по печи, кг/ч

        // ── Тепловые параметры ────────────────────────────────────────────
        public double T0       { get; set; }  // температура окружающей среды, °C
        public double Tsmelt   { get; set; }  // температура зоны плавления, °C
        public double Craspl   { get; set; }  // теплоёмкость расплава, кДж/(кг·°C)
        public double A_raspl  { get; set; }  // коэф. теплоотдачи зона плавления → ОС
        public double K_bottom { get; set; }  // коэф. теплоотдачи расплав → зона плавления

        // ── Экологические показатели (выбросы CO / CO₂) ───────────────────
        public double EcoLeakFactor_CO  { get; set; }  // доля стех. CO в вентиляцию
        public double EcoLeakFactor_CO2 { get; set; }  // доля CO₂ в утечке
        public double EcoVentAirRate    { get; set; }  // расход вентиляции от бункеров, кг/ч
        public double K_CO2_reduction   { get; set; }  // доля восстановленного CO

        // ── Параметры расчёта ─────────────────────────────────────────────
        public double dt { get; set; }  // шаг интегрирования, ч
    }
}
