using System.CodeDom.Compiler;
using System.Linq;

namespace Benchmarking.Common
{
    public static class OutputHelper
    {
        public static double[] DefaultPercentiles = {50.0, 90, 95, 99, 99.9, 99.99};
        
        public static void AppendHistogram(this IndentedTextWriter writer, 
            string title, double[] data, string unit, 
            string format = "0.####",
            double[] percentiles = null)
        {
            percentiles = percentiles ?? DefaultPercentiles;
            
            IndentedTextWriter AppendLine(string s) => writer.AppendLine(s);
            IndentedTextWriter AppendMetric(string name, double value) => writer.AppendMetric(name, value, unit);

            using (writer.Section($"{title}")) {
                if (data.Length == 0) {
                    AppendLine("No data.");
                    return;
                }

                AppendLine("Min .. Max:");
                using (writer.Indent()) {
                    AppendMetric("Min", data.Min());
                    AppendMetric("Avg", data.Average());
                    AppendMetric("Max", data.Max());
                }
                if (percentiles.Length > 0) {
                    AppendLine("Percentiles:");
                    using (writer.Indent()) {
                        var maxIndex = data.Length - 1;
                        foreach (var p in percentiles)
                            AppendMetric($"{p:#.##}%", data[(int) (maxIndex * p / 100)]);
                    }
                }
            }
        }
    }
}
