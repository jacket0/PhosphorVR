namespace PhosphorSimulator
{
    /// <summary>
    /// Управляющие воздействия оператора.
    /// Могут меняться в процессе работы симулятора.
    /// </summary>
    public sealed class ControlInputs
    {
        public double G_fosforit { get; set; } = 16500.0; // средняя скорость подачи фосфорита, кг/ч
        public double G_kvarzit  { get; set; } = 8200.0;  // средняя скорость подачи кварцита, кг/ч
        public double G_coks     { get; set; } = 5300.0;  // средняя скорость подачи кокса, кг/ч
        public double K_ctrl     { get; set; } = 0;       // направление электрода: -1/0/+1
        public double L_ctrl     { get; set; } = 0.05;    // скорость перемещения электрода, м/ч

        // ВНИМАНИЕ: Tau (интервал между порциями) теперь не хардкодится,
        // а вычисляется в PhosphorModel из времени цикла слива по формуле
        //   Tau = T_цикла / N,  где T_цикла = ΔM/(ΣG − Ks·K·I·η).
    }
}
