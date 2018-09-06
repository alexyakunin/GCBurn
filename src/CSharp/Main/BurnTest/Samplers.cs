using System;
using Benchmarking.Common;

namespace GCBurn.BurnTest
{
    public static class Samplers
    {
        public static IDistribution CreateStandardSizeSampler(StdRandom random)
        {
            // Unit of size = 1 byte

            // Small objects: mean = 32B
            var dTypical = new NormalDistribution(random, 32, 64);
            // Large objects, the actual size is 2^n, i.e. mean = 2KB
            var dLarge = new NormalDistribution(random, 11, 1).Transform(x => Math.Pow(2, x)); 
            // Extra large objects, the actual size is 2^n, i.e. mean = 64KB
            var dxLarge = new NormalDistribution(random, 16, 1).Transform(x => Math.Pow(2, x)); 
                    
            var combined = new CombinedDistribution(random, 
                (0.99,   dTypical),
                (0.0099, dLarge),
                (0.0001, dxLarge));
            return combined.TruncateMin(0);
        }

        public static IDistribution CreateStandardTimeSampler(StdRandom random)
        {
            // Unit of time = 1 microsecond, see BurnTester.TimeSamplerFrequency
            var dMethod = new NormalDistribution(random, 0, 0.1).ReflectNegatives(); // ~ Method or a few
            var dRequest = new NormalDistribution(random, 0, 100_000).ReflectNegatives(); // ~ Request or a few
            var dLongLiving = new NormalDistribution(random, 10_000_000, 10_000_000);
            var combined = new CombinedDistribution(random, 
                (0.950, dMethod),
                (0.049, dRequest),
                (0.001, dLongLiving));
            return combined.TruncateMin(0);
        }
    }
}
