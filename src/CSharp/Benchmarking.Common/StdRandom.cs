namespace Benchmarking.Common
{
    public class StdRandom
    {
        public const long Modulo = 2147483647; // 2^31 - 1, 8th Mersenne prime
        public const long Multiplier = 48271; // Prime, same as in C++11 minstd_rand

        public int Seed { get; private set; }

        public StdRandom(int seed) => Seed = seed;
        public override string ToString() => $"{GetType().Name}({Seed})";

        // Returns [0 ... Modulo - 1]
        public int Next()
        {
            Seed = (int) ((Seed * Multiplier) % Modulo);
            return Seed; 
        }

        public double NextDouble() => (double) Next() / Modulo;
    }
}
