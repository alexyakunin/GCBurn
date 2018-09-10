namespace Benchmarking.Common
{
    public static class ArgumentHelper
    {
        public static double ParseRelativeValue(string value, double defaultValue, bool negativeSubtractsFromDefault = false)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
                return defaultValue;

            var offset = 0.0;
            var unit = 1.0;
            if (value.EndsWith('%')) {
                value = value.Substring(0, value.Length - 1).Trim();
                unit = defaultValue / 100;
            }
            else if (value.EndsWith("pct")) {
                value = value.Substring(0, value.Length - 3).Trim();
                unit = defaultValue / 100;
            }
            else if (value.EndsWith("pts")) {
                value = value.Substring(0, value.Length - 3).Trim();
                unit = defaultValue / 1000;
            }
            if (value.StartsWith('-') && negativeSubtractsFromDefault) {
                offset = defaultValue;
            }
            return offset + double.Parse(value) * unit;
        }
    }
}
