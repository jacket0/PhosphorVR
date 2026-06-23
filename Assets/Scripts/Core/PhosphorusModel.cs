using System;
using System.Collections.Generic;
using PhosphorModeling;

namespace PhosphorSimulator
{
    // ── Срез состояния модели на один шаг ──────────────────────────────────
    public sealed class SimulationStep
    {
        public double Time        { get; set; }
        public double C           { get; set; }   // концентрация продукта P4, %
        public double L_mpr       { get; set; }   // МПР, м
        public double Temperature { get; set; }   // °C
        public double R           { get; set; }   // мОм
        public double E           { get; set; }   // ЭДС, В
        public double U           { get; set; }   // напряжение, В
        public double Q           { get; set; }   // мощность, кВт
        public double W           { get; set; }   // накопленная энергия, кВт·ч
        public double MeltMass    { get; set; }   // масса расплава, кг
        public double Gprod       { get; set; }   // производительность по P4, кг/ч
        public double KPD_eff     { get; set; }   // текущий КПД
        public bool   DrainEvent  { get; set; }   // на этом шаге сработал слив
        public bool   FeedEvent   { get; set; }   // на этом шаге сработала загрузка порции сырья
        public double Eco_CO_pct  { get; set; }   // концентрация CO в вентвыбросах, %
        public double Eco_CO2_pct { get; set; }   // концентрация CO₂ в вентвыбросах, %
    }

    public sealed class PhosphorModel
    {
        // ════════════════════════════════════════════════════════════════════
        //                         ПАРАМЕТРЫ МОДЕЛИ
        // ════════════════════════════════════════════════════════════════════

        // ── 1. Геометрия печи ──────────────────────────────────────────────
        public double D    { get; set; } = 7;     // диаметр ванны, м
        public double H    { get; set; } = 4;     // высота печи, м
        public double D_el { get; set; } = 1.3;   // диаметр электрода, м

        // ── 2. Физические свойства материалов ──────────────────────────────
        // Rho_prod — плотность готового расплава для МАССОВОГО расчёта
        // (M = ρ·S·h). Берём 1750 кг/м³ — значение для жидкого фосфора /
        // фосфоро-силикатного расплава из commented-MathModel.
        public double Rho_prod { get; set; } = 1750;

        // Rho_lmpr_eff — ЭФФЕКТИВНАЯ плотность для динамики уровня в
        // формуле dL_мпр/dt: v_prod = (ΣG − Ks·K·I·η)/(ρ_eff·S). Не равна
        // физической ρ_prod: в реальности связь массового потока в ванну
        // и подъёма «электродного зазора» сложнее (часть массы уходит в
        // феррофосфор-фазу, часть — в шлак, электрод частично «следует»
        // за уровнем). Эмпирическое значение 7000 из исходного кода даёт
        // L_мпр-динамику, согласованную с тепловым балансом РКЗ-48.
        public double Rho_lmpr_eff { get; set; } = 7000;

        public double Rho_el   { get; set; } = 400;    // плотность электродной массы, кг/м³
        public double Craspl   { get; set; } = 0.279;  // теплоёмкость расплава, кДж/(кг·°C)

        // ── 3. Начальное состояние и цикл накопления / слива ───────────────
        // Масса расплава рассчитывается по формуле m_raspl = S · h · ρ:
        //   • H_max ≈ 1 м — допустимая высота слоя расплава (Богданов: до 0.7 м
        //     для штейновых печей; для РКЗ-48Ф высота столба расплава 0.8–1 м);
        //   • H_min — остаточная высота после выпуска шлака.
        // Mraspl_max и Mraspl_min — производные от H и Rho_prod.
        public double H_max     { get; set; } = 1.0;      // допустимая высота расплава, м
        public double H_min     { get; set; } = 0.8;      // остаточная высота после слива, м
        public double Mraspl_0  { get; set; } = 0;        // начальная масса (0 = взять из Mraspl_min)
        public double C_P4_0    { get; set; } = 98;       // начальная концентрация P4, %
        public double DropAtDrain { get; set; } = 0.6;    // ступенчатое падение C при сливе, %

        // Mraspl_max / Mraspl_min — derived из физики
        public double Mraspl_max => Rho_prod * S_bath * H_max;
        public double Mraspl_min => Rho_prod * S_bath * H_min;
        // Старое поле Mraspl сохраняем как алиас на Mraspl_max (используется
        // в производном TimeSliv для совместимости).
        public double Mraspl    => Mraspl_max;

        // ── 4. Номинальный режим (точка линеаризации R и E) ────────────────
        public double L_mpr_nom { get; set; } = 0.3;    // номинальное МПР, м
        public double T_nom     { get; set; } = 1500;   // номинальная температура, °C
        public double Tsmelt    { get; set; } = 1300;   // температура плавления, °C
        public double T0        { get; set; } = 25;     // температура окружающей среды, °C
        public double Cprod_nom { get; set; } = 99.0;   // номинальная чистота продукта, %

        // ── 5. Производительность и шихтовые коэффициенты ──────────────────
        // K, Ks — из материалов по фосфору (Ершова, презентация, формулы 1–2):
        //   G_prod = K · I · η_ID                              (формула 2)
        //   m_raspl · dC_prod/dt = ΣG_s − Ks · K · I · η_ID    (формула 1)
        public double K  { get; set; } = 0.0756;  // коэффициент выхода P4, кг/(кА·ч)
        public double Ks { get; set; } = 1.89;    // коэффициент шихты

        // ── 6. КПД как сглаженное случайное возмущение ────────────────────
        // Раз в KPD_changeInterval выбирается новая случайная «цель» в
        // [KPD_0, KPD_max], а текущий КПД ПЛАВНО к ней притягивается с
        // постоянной времени KPD_changeInterval. График КПД (и через него
        // производительности) выглядит как мягкое блуждание, а не как
        // зигзагообразные ступеньки.
        public double KPD_0              { get; set; } = 0.91;
        public double KPD_max            { get; set; } = 0.96;
        public double KPD_changeInterval { get; set; } = 0.5;   // 30 sim-минут (медленнее)

        // Сглаженный стохастический КПД: target меняется ступеньками,
        // _kpdCurrent релаксирует к нему.
        private double _kpdCurrent        = 0.935;
        private double _kpdTarget         = 0.935;
        private double _kpdNextChangeTime = -1.0;
        private double _kpdLastUpdateTime = double.NegativeInfinity;
        private readonly Random _kpdRng   = new Random(42);

        // ── 7. Электрические параметры ─────────────────────────────────────
        // R в CalcU умножается на (I_el/1000) — эффективные единицы мОм (V/кА).
        // Чувствительности повышены, чтобы R/E видимо реагировали на МПР, T, C.
        public double I_el    { get; set; } = 120000; // ток электрода, А
        public double Cos_phi { get; set; } = 0.91;
        public double R_nom   { get; set; } = 1.0;    // мОм
        public double E_nom   { get; set; } = 220.0;  // В
        public double K_Rl    { get; set; } = 1.5;    // мОм / м МПР
        public double K_Rt    { get; set; } = 5e-4;   // мОм / °C
        public double K_Rc    { get; set; } = 0.05;   // мОм / %
        public double K_Et    { get; set; } = 0.05;   // В / °C
        public double K_Ec    { get; set; } = 100.0;  // В, множитель ln(C/Cnom)

        // ── 8. Тепловые параметры ──────────────────────────────────────────
        public double P_melt   { get; set; } = 0.032; // удельн. сопротивление расплава, Ом·м
        public double A_raspl  { get; set; } = 8.82;  // теплоотдача через стены, Вт/(м²·°C)
        public double K_bottom { get; set; } = 60;    // теплоотдача через подину, Вт/(м²·°C)

        // ── 9. Расход электродной массы ────────────────────────────────────
        public double G_el { get; set; } = 35;        // кг/ч

        // ── 9a. Экологические показатели (выбросы CO / CO₂) ────────────────
        // Основной газовый поток P4+CO уходит в конденсатор (CO — товарный).
        // «Экология» по диссертации Ершовой (табл. 4.6, ID=3) — это
        // концентрация CO и CO₂ в ВЕНТИЛЯЦИОННОМ воздухе от печных бункеров,
        // куда просачивается лишь малая доля газовой фазы. Регламент: ≤ 1 %.
        // Молярные массы (для стехиометрии 2Ca3(PO4)2+6SiO2+10C→P4+6CaSiO3+10CO):
        private const double M_P4  = 124.0;   // г/моль
        private const double M_CO  = 28.0;    // г/моль
        private const double M_CO2 = 44.0;    // г/моль
        public double EcoLeakFactor_CO  { get; set; } = 0.03;   // доля стех. CO в вентиляцию (3 %)
        public double EcoLeakFactor_CO2 { get; set; } = 0.06;   // доля CO₂ в утечке
        public double EcoVentAirRate    { get; set; } = 90000;  // расход вентиляции от бункеров, кг/ч
        public double K_CO2_reduction   { get; set; } = 0.8;    // ф. 4.247: 80 % CO восстановлено

        // ── 10. Физические границы МПР (страховочный клампинг) ─────────────
        public double L_mpr_min { get; set; } = 0.05;
        public double L_mpr_max { get; set; } = 0.60;

        // ── 11. Параметры расчёта ──────────────────────────────────────────
        public double DtStep             { get; set; } = 0.01;    // шаг интегрирования, ч
        // Число порций сырья за один цикл слива. Tau (интервал между
        // загрузками) вычисляется как T_цикла / NPortionsPerCycle.
        public double NPortionsPerCycle  { get; set; } = 8;

        // Таймер импульсной загрузки сырья. Инициализируется в Advance при
        // первом вызове (если < 0 → присваиваем Tau, и первая порция падает
        // через интервал Tau от старта — так же, как в StepCaC2).
        private double _nextFeedTime = -1.0;

        // Tau (интервал подачи) — время доставки одной порции сырья.
        // Размер порции ≈ (M_max − M_min)/N (доля рабочего объёма ванны),
        // поэтому Tau = (размер порции)/ΣG = (M_max − M_min)/(N·ΣG).
        // ВАЖНО: Tau зависит ТОЛЬКО от темпа подачи ΣG, НЕ от чистого
        // накопления. Цикл подачи (доставка сырья) и цикл слива (удаление
        // шлака) — независимые процессы с разными периодами. Прежняя версия
        // делила на netRate = ΣG − Ks·G_P4; около баланса netRate→0 давал
        // Tau→∞, подача замирала, а потребление продолжалось → масса в ноль
        // → C<0 → Math.Log(C/Cnom)=NaN (источник краша при низкой подаче).
        private double ComputeTau(double sumG)
        {
            double cycleRange = Mraspl_max - Mraspl_min;
            if (sumG > 1.0)
                return cycleRange / (NPortionsPerCycle * sumG);
            return 10.0;   // ΣG ≈ 0 → подача фактически отключена
        }

        // ── 12. Производные свойства ───────────────────────────────────────
        public double S_bath   => Math.PI * Math.Pow(D / 2, 2);
        public double S_el     => Math.PI * (D_el/ 2) * (D_el/ 2);
        public double S_bottom => S_bath + S_el;
        // Номинальная производительность (для оценок типа TimeSliv) — берём KPD_0
        // как наименьший в диапазоне. Реальная Gprod каждого шага в Advance
        // считается через текущий случайный KPD.
        public double Gprod    => KPD_0 * I_el * K;
        public double TimeSliv => Mraspl / Gprod;

        // ════════════════════════════════════════════════════════════════════
        //                         КОНСТРУКТОРЫ
        // ════════════════════════════════════════════════════════════════════

        public PhosphorModel() { }

        public PhosphorModel(ModelParameters p)
        {
            if (p == null) throw new ArgumentNullException(nameof(p));
            D      = p.D;
            H      = p.H;
            D_el   = p.Del;
            I_el   = p.I * 1000.0;   // кА → А
            DtStep = p.dt;
            if (p.C_P4_0     > 0) C_P4_0     = p.C_P4_0;
            if (p.T_nom      > 0) T_nom      = p.T_nom;
            if (p.L_mpr_nom  > 0) L_mpr_nom  = p.L_mpr_nom;
            if (p.KPD_0      > 0) KPD_0      = p.KPD_0;
            if (p.KPD_max    > 0) KPD_max    = p.KPD_max;
            if (p.Cprod_nom  > 0) Cprod_nom  = p.Cprod_nom;
            if (p.T0         > 0) T0         = p.T0;
            if (p.Tsmelt     > 0) Tsmelt     = p.Tsmelt;
            if (p.Craspl     > 0) Craspl     = p.Craspl;
            if (p.A_raspl    > 0) A_raspl    = p.A_raspl;
            if (p.K_bottom   > 0) K_bottom   = p.K_bottom;
            if (p.G_el       > 0) G_el       = p.G_el;
            // Электрические параметры (точка линеаризации R/E и чувствительности).
            if (p.Cos_phi    > 0) Cos_phi    = p.Cos_phi;
            if (p.R_nom      > 0) R_nom      = p.R_nom;
            if (p.E_nom      > 0) E_nom      = p.E_nom;
            if (p.K_Rl       > 0) K_Rl       = p.K_Rl;
            if (p.K_Rt       > 0) K_Rt       = p.K_Rt;
            if (p.K_Rc       > 0) K_Rc       = p.K_Rc;
            if (p.K_Et       > 0) K_Et       = p.K_Et;
            if (p.K_Ec       > 0) K_Ec       = p.K_Ec;
            if (p.P_melt     > 0) P_melt     = p.P_melt;
            // Шихтовые коэффициенты выхода продукта.
            if (p.K          > 0) K          = p.K;
            if (p.Ks         > 0) Ks         = p.Ks;
            // Экологические показатели вентвыбросов.
            if (p.EcoLeakFactor_CO  > 0) EcoLeakFactor_CO  = p.EcoLeakFactor_CO;
            if (p.EcoLeakFactor_CO2 > 0) EcoLeakFactor_CO2 = p.EcoLeakFactor_CO2;
            if (p.EcoVentAirRate    > 0) EcoVentAirRate    = p.EcoVentAirRate;
            if (p.K_CO2_reduction   > 0) K_CO2_reduction   = p.K_CO2_reduction;
            // Геометрия и плотность определяют M_max/M_min через m = S·h·ρ.
            if (p.H_max      > 0) H_max      = p.H_max;
            if (p.H_min      > 0) H_min      = p.H_min;
            if (p.Rho_prod   > 0) Rho_prod   = p.Rho_prod;
            if (p.Rho_el     > 0) Rho_el     = p.Rho_el;
            // Начальная масса: если 0 — берём остаточную (после слива).
            Mraspl_0 = p.Mraspl_0 > 0 ? p.Mraspl_0 : Mraspl_min;
        }

        // ════════════════════════════════════════════════════════════════════
        //                         БАЗОВЫЕ ФОРМУЛЫ ИЗ ИСТОЧНИКА
        //         (R, E, U, Q, КПД во времени — формулы 4–7 презентации)
        // ════════════════════════════════════════════════════════════════════

        // КПД ПЛАВНО блуждает в диапазоне [KPD_0, KPD_max]. Раз в
        // KPD_changeInterval выбирается новая случайная цель, а текущее
        // значение релаксирует к ней по закону первого порядка:
        //   dKPD/dt = (target − KPD) / τ,   τ ≈ KPD_changeInterval.
        // Идемпотентно по повторному вызову с тем же time.
        public double GetKPD(double time)
        {
            // Инициализация при первом вызове.
            if (_kpdNextChangeTime < 0)
            {
                _kpdNextChangeTime = time + KPD_changeInterval;
                _kpdLastUpdateTime = time;
                _kpdTarget         = KPD_0 + _kpdRng.NextDouble() * (KPD_max - KPD_0);
            }

            // Новая цель — раз в KPD_changeInterval (while на случай большого dt).
            while (time >= _kpdNextChangeTime)
            {
                _kpdTarget          = KPD_0 + _kpdRng.NextDouble() * (KPD_max - KPD_0);
                _kpdNextChangeTime += KPD_changeInterval;
            }

            // Плавная релаксация current → target с постоянной времени = interval.
            double dt = time - _kpdLastUpdateTime;
            if (dt > 0)
            {
                double tau   = KPD_changeInterval;
                double alpha = 1.0 - Math.Exp(-dt / tau);
                _kpdCurrent += alpha * (_kpdTarget - _kpdCurrent);
                _kpdLastUpdateTime = time;
            }
            return _kpdCurrent;
        }

        public double CalcR(double lmpr, double Traspl, double Cprod) =>
            R_nom + K_Rl * (lmpr   - L_mpr_nom)
                  - K_Rt * (Traspl - T_nom)
                  + K_Rc * (Cprod  - Cprod_nom);

        public double CalcE(double Traspl, double Cprod) =>
            E_nom - K_Et * (Traspl - T_nom)
                  - K_Ec * Math.Log(Cprod / Cprod_nom);

        public double CalcU(double R, double E) => (I_el / 1000.0) * R + E;

        public double CalcQ(double U) => U * I_el * Cos_phi / 1000.0;

        // ════════════════════════════════════════════════════════════════════
        //                  ДИФФЕРЕНЦИАЛЬНЫЕ УРАВНЕНИЯ СОСТОЯНИЯ
        // ════════════════════════════════════════════════════════════════════

        // Фактическая производительность по P4 (кг/ч) — закон сохранения массы:
        // нельзя произвести P4 (и выгнать в газ Ks·G_P4 массы) больше, чем
        // обеспечивает поданное сырьё. Поэтому производительность ограничена
        // СНИЗУ двумя факторами:
        //   • электрическая мощность:  K · I · η         (сколько печь «может»);
        //   • подача сырья:            ΣG / Ks           (сколько сырьё «даёт»).
        // G_P4 = min(этих двух). При обильной подаче связывает электрика
        // (штатный режим, накопление шлака → слив). При недокорме связывает
        // подача — производство падает, масса не уходит в минус.
        // Это НЕ композиционное лимитирование (по P2O5/SiO2/C), а единственная
        // фундаментальная связь через шихтовый коэффициент Ks.
        public double EffectiveGprod(double sumGrate, double kpd)
        {
            double electrical = K * I_el * kpd;
            double feedLimited = Ks > 1e-9 ? sumGrate / Ks : electrical;
            return Math.Min(electrical, feedLimited);
        }

        // Концентрация продукта — формула 1 из презентации Ершовой:
        //   m_raspl · dC_prod/dt = ΣG_s − Ks · G_P4
        //   ⇒ dC/dt = (ΣG_s − Ks · G_P4) / m_raspl
        // где Ks·G_P4 — фактический газовый унос (consumption). В импульсной
        // подаче ΣG между порциями = 0, порция добавляется к C в Advance.
        private double Calc_dC(double sumG, double meltMass, double consumption)
        {
            double mass = meltMass > 1.0 ? meltMass : 1.0; // защита от деления на 0
            return (sumG - consumption) / mass;
        }

        // Масса расплава — массовый баланс ванны: dM/dt = ΣG − Ks·G_P4.
        // consumption = Ks·G_P4 (фактический, ограниченный подачей).
        private double Calc_dMelt(double sumG, double consumption)
        {
            return sumG - consumption;
        }

        // МПР — формула 4 из презентации: dL/dt = v_el − v_prod + K_ctrl·L_ctrl.
        // v_prod использует Rho_lmpr_eff (см. пояснение у поля). consumption —
        // фактический газовый унос, согласован с массовым балансом.
        private double Calc_dLmpr(ControlInputs controls, double sumG, double consumption)
        {
            double v_el   = G_el / (Rho_el * S_el);
            double v_prod = (sumG - consumption) / (Rho_lmpr_eff * S_bath);
            return v_el - v_prod + controls.K_ctrl * controls.L_ctrl;
        }

        // Температура — формула 5: тепловой баланс расплава.
        // M в знаменателе — живая масса (тепловая инерция следует за циклом
        // накопления/слива). mass > 1 — только страховка от деления на 0.
        private double Calc_dTemp(double Traspl, double lmpr, double meltMass)
        {
            double R_melt = P_melt * lmpr / S_bath;
            double mass   = meltMass > 1.0 ? meltMass : 1.0;
            return 3.6 * (I_el * I_el * R_melt
                         - A_raspl  * S_bath   * (Traspl - Tsmelt)
                         - K_bottom * S_bottom * (Traspl - T0))
                       / (mass * Craspl);
        }

        // Экологические показатели — концентрация CO/CO₂ в вентиляционном
        // воздухе печных бункеров (диссертация, табл. 4.6, формулы 4.245/4.247):
        //   • полный стех. поток CO = G_P4 · (10·M_CO / M_P4);
        //   • часть CO не восстановилась → CO₂ = CO·(1−k_red)·(M_CO2/M_CO);
        //   • в вентиляцию утекает доля EcoLeakFactor_* этих потоков;
        //   • концентрация = масса утечки / (вентвоздух + утечки) · 100 %.
        private (double co_pct, double co2_pct) CalcEco(double gp4_kgph)
        {
            double gCOtotal  = gp4_kgph * (10.0 * M_CO / M_P4);
            double gCO2total = gCOtotal * (1.0 - K_CO2_reduction) * (M_CO2 / M_CO);

            double gCOleak  = EcoLeakFactor_CO  * gCOtotal;
            double gCO2leak = EcoLeakFactor_CO2 * gCO2total;

            double ventTotal = EcoVentAirRate + gCOleak + gCO2leak;
            if (ventTotal < 1e-6) return (0, 0);
            return (100.0 * gCOleak / ventTotal, 100.0 * gCO2leak / ventTotal);
        }

        // ════════════════════════════════════════════════════════════════════
        //                     ЦИКЛ НАКОПЛЕНИЯ / СЛИВА
        // ════════════════════════════════════════════════════════════════════

        // Проверка триггера слива: при достижении Mraspl_max масса скачком
        // возвращается к Mraspl_min, выставляется флаг drainTriggered.
        private (double mass, bool drainTriggered) CheckDrainTrigger(double mass)
        {
            if (mass >= Mraspl_max) return (Mraspl_min, true);
            return (mass, false);
        }

        // ════════════════════════════════════════════════════════════════════
        //                              ШАГ МОДЕЛИ
        // ════════════════════════════════════════════════════════════════════

        public SimulationStep Advance(SimulationStep s, ControlInputs controls)
        {
            double kpd        = GetKPD(s.Time);
            double sumG_rate  = controls.G_fosforit + controls.G_kvarzit + controls.G_coks;
            double gprod      = EffectiveGprod(sumG_rate, kpd); // ограничена подачей
            double consumption = Ks * gprod;                    // газовый унос, кг/ч
            double Tau        = ComputeTau(sumG_rate);          // не хардкод, считается из темпа подачи

            // ── Импульсная подача сырья (по образцу StepCaC2) ─────────────
            // Первая порция планируется через Tau ч после старта; затем
            // каждые Tau ч в ванну сбрасывается порция G_rate·Tau (масса
            // одной порции каждого компонента).
            if (_nextFeedTime < 0.0) _nextFeedTime = Tau;

            bool   feedEvent = false;
            double pulseMass = 0.0;
            if (s.Time >= _nextFeedTime)
            {
                feedEvent     = true;
                pulseMass     = sumG_rate * Tau;
                _nextFeedTime += Tau;
            }

            // ── Масса расплава ───────────────────────────────────────────
            // dM/dt = ΣG − consumption, ΣG представлена дельтами:
            //   • между пульсами sumG=0 → непрерывно теряем consumption в час;
            //   • в момент пульса добавляется pulseMass (как ступенька).
            double dMelt_continuous = Calc_dMelt(0, consumption);
            double newMeltMass = s.MeltMass + pulseMass + DtStep * dMelt_continuous;
            bool   drainTriggered;
            (newMeltMass, drainTriggered) = CheckDrainTrigger(newMeltMass);

            // ── Концентрация (формула 1 из источника, импульсная версия) ──
            // dC = (pulse_mass)/m  +  (-consumption·dt)/m. Непрерывная часть
            // через Calc_dC с sumG=0; импульс — ступенькой.
            double dC_continuous = Calc_dC(0, s.MeltMass, consumption);
            double mass_for_pulse = s.MeltMass > 1.0 ? s.MeltMass : 1.0;
            double newC = s.C + (feedEvent ? pulseMass / mass_for_pulse : 0)
                              + DtStep * dC_continuous;
            if (drainTriggered) newC -= DropAtDrain;

            // ── МПР: использует среднюю скорость подачи (как StepMPR в CaC2-коде) ──
            // Между пульсами на масштабе Tau поведение L_мпр совпадает с
            // непрерывной моделью; делать L_мпр зубчатым излишне.
            // НЕТ скачка при сливе: добавление ступенчатого ΔL_мпр= +(H_max−H_min)
            // удваивало I²·R_melt и масса падала вдвое — T резко спайкала на
            // сотни градусов. Геометрический «возврат» уровня после слива в
            // среднем уже учтён непрерывным v_prod (он за цикл интегрирует
            // как раз ΔM/(ρ·S)).
            double newLmpr = s.L_mpr + DtStep * Calc_dLmpr(controls, sumG_rate, consumption);
            if (newLmpr < L_mpr_min) newLmpr = L_mpr_min;
            if (newLmpr > L_mpr_max) newLmpr = L_mpr_max;

            // ── Температура: без изменений ──────────────────────────────
            double newTemp = s.Temperature + DtStep * Calc_dTemp(s.Temperature, s.L_mpr, s.MeltMass);

            // ── Энергия за период ────────────────────────────────────────
            double newW = s.W + DtStep * CalcQ(CalcU(
                CalcR(s.L_mpr, s.Temperature, s.C),
                CalcE(s.Temperature, s.C)));

            // Производные на новом моменте
            double newTime  = s.Time + DtStep;
            double kpdNew   = GetKPD(newTime);
            double gprodNew = EffectiveGprod(sumG_rate, kpdNew); // ограничена подачей
            double Rnew     = CalcR(newLmpr, newTemp, newC);
            double Enew     = CalcE(newTemp, newC);
            double Unew     = CalcU(Rnew, Enew);
            var (coPct, co2Pct) = CalcEco(gprodNew);

            return new SimulationStep
            {
                Time        = newTime,
                C           = newC,
                L_mpr       = newLmpr,
                Temperature = newTemp,
                R           = Rnew,
                E           = Enew,
                U           = Unew,
                Q           = CalcQ(Unew),
                W           = newW,
                MeltMass    = newMeltMass,
                Gprod       = gprodNew,
                KPD_eff     = kpdNew,
                DrainEvent  = drainTriggered,
                FeedEvent   = feedEvent,
                Eco_CO_pct  = coPct,
                Eco_CO2_pct = co2Pct,
            };
        }


        // ════════════════════════════════════════════════════════════════════
        //                         ВХОДНЫЕ ТОЧКИ
        // ════════════════════════════════════════════════════════════════════

        public SimulationStep InitialState()
        {
            double kpd   = GetKPD(0);
            double gprod = kpd * I_el * K;
            double C0    = C_P4_0;
            double R     = CalcR(L_mpr_nom, T_nom, C0);
            double E     = CalcE(T_nom, C0);
            double U     = CalcU(R, E);
            var (coPct, co2Pct) = CalcEco(gprod);
            return new SimulationStep
            {
                Time        = 0,
                C           = C0,
                L_mpr       = L_mpr_nom,
                Temperature = T_nom,
                R           = R,
                E           = E,
                U           = U,
                Q           = CalcQ(U),
                W           = 0,
                MeltMass    = Mraspl_0,
                Gprod       = gprod,
                KPD_eff     = kpd,
                DrainEvent  = false,
                Eco_CO_pct  = coPct,
                Eco_CO2_pct = co2Pct,
            };
        }

        public List<SimulationStep> RunSimulation(ControlInputs controls)
        {
            var current = InitialState();
            var results = new List<SimulationStep>(capacity: (int)(TimeSliv / DtStep) + 1);

            while (current.Time < TimeSliv)
            {
                results.Add(current);
                current = Advance(current, controls);
            }

            results.Add(current);
            return results;
        }
    }
}
