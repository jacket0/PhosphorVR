using System;

namespace PhosphorModeling
{
    public static class Validator
    {
        public static double ValidatePositiveDouble(string value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"Параметр '{parameterName}' не может быть пустым");

            value = value.Replace('.', ',');

            if (!double.TryParse(value, out double result) || result <= 0)
                throw new ArgumentException($"Некорректное значение параметра '{parameterName}': {value}. Значение должно быть положительным числом.");

            return result;
        }
    }
}
