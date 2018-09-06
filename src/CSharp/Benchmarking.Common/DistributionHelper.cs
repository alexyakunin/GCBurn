using System;

namespace Benchmarking.Common 
{
    public static class DistributionHelper
    {
        public static IDistribution Transform(this IDistribution source, Func<double, double> transformer) =>
            new TransformedDistribution(source, transformer);

        public static IDistribution ReflectNegatives(this IDistribution source) =>
            source.Transform(x => x < 0 ? -x : x);

        public static IDistribution Truncate(this IDistribution source, double min, double max) =>
            source.Transform(x => Math.Max(min, Math.Min(max, x)));

        public static IDistribution TruncateMin(this IDistribution source, double min) =>
            source.Transform(x => Math.Max(min, x));

        public static IDistribution TruncateMax(this IDistribution source, double max) =>
            source.Transform(x => Math.Min(max, x));

        public static IDistribution Quantize(this IDistribution source, double quanta) =>
            source.Transform(x => Math.Floor(x / quanta));
    }
}
