using System;
using System.Collections.Generic;

namespace PhosphorModeling
{
    public static class DefaultData
    {
        public static readonly string[] Components = {
            "P₂O₅", "SiO₂", "Al₂O₃", "CaO", "MgO", "H₂O", "Fe₂O₃",
            "C", "F", "C/C", "N", "Зола", "R₂O"
        };

        public static Dictionary<string, Dictionary<string, double>> GetDefaultComposition()
        {
            var data = new Dictionary<string, Dictionary<string, double>>();

            var defaultRows = new object[][]
            {
                new object[] { "Фосфорит", 0.73, 0.09, 0.02, 0.05, 0.02, 0.02, 0.02, 0.01, 0.01, 0.00, 0.00, 0.02, 0.01 },
                new object[] { "Кварцит", 0.01, 0.95, 0.01, 0.01, 0.01, 0.00, 0.00, 0.00, 0.00, 0.00, 0.00, 0.01, 0.00 },
                new object[] { "Кокс", 0.01, 0.04, 0.02, 0.01, 0.01, 0.01, 0.00, 0.85, 0.00, 0.00, 0.00, 0.04, 0.01 },
                new object[] { "Электродная масса", 0.00, 0.01, 0.01, 0.01, 0.01, 0.01, 0.00, 0.92, 0.00, 0.00, 0.00, 0.02, 0.01 },
            };

            foreach (var row in defaultRows)
            {
                var flowName = row[0].ToString();
                var componentsDict = new Dictionary<string, double>();
                for (int i = 0; i < Components.Length; i++)
                {
                    componentsDict[Components[i]] = Convert.ToDouble(row[i + 1]);
                }
                data[flowName] = componentsDict;
            }

            return data;
        }
    }
}