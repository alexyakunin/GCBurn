using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Benchmarking.Common
{
    public sealed class Benchmark
    {
        public const int DefaultIterationCount = 10;
        public static readonly Action<bool> DefaultSubtractAction = isWarmup => {};
        
        public string Title { get; set; }
        public Action<bool> Action { get; set; }
        public Action<bool> SubtractAction { get; set; } = DefaultSubtractAction;
        public double SubtractActionFactor { get; set; } = 1;
        public bool MustWarmup { get; set; } = true;
        public bool MustGC { get; set; } = false;
        
        public List<TimeSpan> Timings { get; set; } = new List<TimeSpan>();

        public static Benchmark Run(string title, Action<bool> action,
            Action<bool> subtractAction = null, double subtractActionFactor = 1,
            bool mustWarmup = true, bool mustGC = false,
            int iterationCount = DefaultIterationCount)
        {
            return new Benchmark(title, action) {
                SubtractAction = subtractAction ?? DefaultSubtractAction,
                SubtractActionFactor = subtractActionFactor,
                MustWarmup = mustWarmup,
                MustGC = mustGC,
            }.Run(iterationCount);
        }

        public Benchmark(string title, Action<bool> action = null)
        {
            Title = title;
            Action = action;
        }

        public Benchmark Run(int iteractionCount = DefaultIterationCount)
        {
            for (int i = 0; i < iteractionCount; i++)
                RunOnce();
            return this;
        }

        private void RunOnce()
        {
            if (MustWarmup) {
                Action.Invoke(true);
                SubtractAction?.Invoke(true);
                MustWarmup = false;
            }
            if (MustGC)
                GC.Collect();

            var elapsed = TimeSpan.Zero;
            
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            try {
                Action.Invoke(false);
            }
            finally {
                stopwatch.Stop();
                elapsed += stopwatch.Elapsed;
            }

            if (SubtractAction != null) {
                if (MustGC)
                    GC.Collect();
                stopwatch.Reset();
                stopwatch.Start();
                try {
                    SubtractAction.Invoke(false);
                }
                finally {
                    stopwatch.Stop();
                    elapsed -= stopwatch.Elapsed * SubtractActionFactor;
                }
            }

            Timings.Add(elapsed);
        }

        public override string ToString()
        {
            if (!Timings.Any())
                return $"{Title}: n/a";
            var count = Timings.Count;
            var midPointL = (count - 1) / 2;
            var midPointR = count / 2;
            var min = Timings.Min().TotalMilliseconds;
            var max = Timings.Max().TotalMilliseconds;
            var avg = Timings.Average(t => t.TotalMilliseconds);
            var p50 = (Timings[midPointL] + Timings[midPointR]).TotalMilliseconds / 2;
            return $"{Title}: {avg:0.000}ms avg -- [{min:0.000}ms ... {p50:0.000}ms ... {max:0.000}ms] in {count} run(s)";
        }
    }
}
