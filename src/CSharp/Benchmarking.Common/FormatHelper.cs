namespace Benchmarking.Common
{
    public static class FormatHelper
    {
        public static string ToString(this object value, string unit) =>
            value != null ? (value + " " + unit) : "n/a";
        
        public static string ToString(this int? value, string format, string unit) =>
            value.HasValue ? (value.Value.ToString(format) + " " + unit) : "n/a";
        
        public static string ToString(this long? value, string format, string unit) =>
            value.HasValue ? (value.Value.ToString(format) + " " + unit) : "n/a";
        
        public static string ToString(this double? value, string format, string unit) =>
            value.HasValue ? (value.Value.ToString(format) + " " + unit) : "n/a";
        
    }
}
