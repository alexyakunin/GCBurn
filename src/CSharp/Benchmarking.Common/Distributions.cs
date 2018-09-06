using System;
using System.Collections.Generic;
using System.Linq;

namespace Benchmarking.Common
{
    public interface IDistribution
    {
        double Sample();
    }

    public sealed class TransformedDistribution : IDistribution
    {
        public IDistribution Source { get; } 
        public Func<double, double> Transformer { get; }

        public TransformedDistribution(IDistribution source, Func<double, double> transformer)
        {
            Source = source;
            Transformer = transformer;
        }

        public double Sample() => Transformer.Invoke(Source.Sample());
    }

    public abstract class DistributionBase : IDistribution
    {
        public StdRandom Random { get; }

        protected DistributionBase(StdRandom random) => Random = random;

        public abstract double Sample();
    }

    public sealed class CustomDistribution : DistributionBase
    {
        public Func<StdRandom, double> Sampler { get; }

        public CustomDistribution(StdRandom random, Func<StdRandom, double> sampler) : base(random) => Sampler = sampler;

        public override double Sample() => Sampler.Invoke(Random);
    }

    public sealed class NormalDistribution : DistributionBase
    {
        public double Mean { get; }
        public double StdDev { get; }

        public NormalDistribution(StdRandom random, double mean, double stdDev) : base(random)
        {
            Mean = mean;
            StdDev = stdDev;
        }

        public override double Sample() => Sample(Random, Mean, StdDev);

        public static double Sample(StdRandom random, double mean = 0, double stdev = 0)
        {
            while (true) {
                if (PolarTransform(random.NextDouble(), random.NextDouble(), out var x, out var _))
                    return mean + stdev * x;
            }
        }

        private static bool PolarTransform(double x01, double y01, out double x, out double y)
        {
            var xn11 = 2.0 * x01 - 1.0;
            var yn11 = 2.0 * y01 - 1.0;
            var r2 = xn11 * xn11 + yn11 * yn11;
            if (r2 >= 1.0 || r2 == 0.0) {
                x = 0.0;
                y = 0.0;
                return false;
            }
            var factor = Math.Sqrt(-2.0 * Math.Log(r2) / r2);
            x = xn11 * factor;
            y = yn11 * factor;
            return true;
        }
    }

    public sealed class LogNormalDistribution : DistributionBase
    {
        public double Mu { get; }
        public double Sigma { get; }

        public LogNormalDistribution(StdRandom random, double mu, double sigma) : base(random)
        {
            Mu = mu;
            Sigma = sigma;
        }

        public override double Sample() => Sample(Random, Mu, Sigma);

        public static double Sample(StdRandom random, double mu, double sigma) => 
            Math.Exp(NormalDistribution.Sample(random, mu, sigma));
    }

    public sealed class CombinedDistribution : DistributionBase
    {
        public (double Weight, IDistribution Distribution)[] Distributions { get; }

        public CombinedDistribution(StdRandom random, IEnumerable<(double Weight, IDistribution Distribution)> distributions)
            : this(random, distributions.ToArray()) {} 

        public CombinedDistribution(StdRandom random, params (double Weight, IDistribution Distribution)[] distributions) 
            : base(random)
        {
            if (distributions.Length < 1)
                throw new ArgumentException("At least one distribution must be provided.");
            if (Math.Abs(distributions.Sum(p => p.Weight) - 1) > 1e-8)
                throw new ArgumentException("Weights must sum up to 1.");
            Distributions = distributions;
        }

        public override double Sample()
        {
            // Obviously could be done in O(log(n)), but we don't expect to have too many of distributions, so...
            var expectedWeight = Random.NextDouble();
            var weight = Distributions[0].Weight;
            var index = 0;
            while (expectedWeight >= weight)
                weight += Distributions[++index].Weight;
            if (index >= Distributions.Length)
                index = Distributions.Length - 1;
            return Distributions[index].Distribution.Sample();
        }
    }
}
