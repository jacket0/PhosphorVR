namespace PhosphorModeling
{
    public class Scenario
    {
        public string Name                    { get; set; }
        public string Description             { get; set; }
        public int    TrainingDurationSeconds { get; set; }
        public int    DurationSeconds         { get; set; }

        // Регламентные диапазоны критериальных показателей
        public double ConcMin   { get; set; }  // мин. концентрация P₄, %
        public double ProdMin   { get; set; }  // мин. производительность, т/ч
        public double EnergyMax { get; set; }  // макс. энергопотребление, кВт
        public double TempMin   { get; set; }  // мин. температура расплава, °C
        public double TempMax   { get; set; }  // макс. температура расплава, °C

        // Экологические показатели (диссертация, табл. 4.6, ID=3 — фосфор)
        public double EcoCOMax  { get; set; }  // макс. CO в вентвыбросах, %
        public double EcoCO2Max { get; set; }  // макс. CO₂ в вентвыбросах, %
    }
}
