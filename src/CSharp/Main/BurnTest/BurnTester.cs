using System;
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using Benchmarking.Common;

namespace GCBurn.BurnTest 
{
    public class BurnTester
    {
        public const int AllocationSequenceLength = 1 << 20; // 2^20, i.e. ~ 1M items; must be a power of 2!
        public const double TimeSamplerFrequency = 1_000_000;
        public const double TimeSamplerUnitInSeconds = 1 / TimeSamplerFrequency;
        public static TimeSpan DefaultDuration = TimeSpan.FromSeconds(10);
        public static double DefaultMaxTime = 1000 * TimeSamplerFrequency; // 1000 seconds
        public static double DefaultMaxSize = 1 << 17; // 128KB
        
        public TimeSpan Duration = DefaultDuration;
        public StdRandom Random = new StdRandom(123); 
        public Func<StdRandom, IDistribution> CreateSizeSampler = Samplers.CreateStandardSizeSampler; 
        public Func<StdRandom, IDistribution> CreateTimeSampler = Samplers.CreateStandardTimeSampler;
        public double MaxTime = DefaultMaxTime;
        public double MaxSize = DefaultMaxSize;
        public long StaticSetSize = 0;
        public IndentedTextWriter Writer = new IndentedTextWriter(Console.Out, "  ");
        
        private bool _isInitialized;
        private object[][] _staticSet;
        private (int ArraySize, sbyte BucketIndex, sbyte GenerationIndex)[] _allocations;
        private int[] _startIndexes;
        
        public static BurnTester New(long staticSetSize) => new BurnTester() {
            StaticSetSize = staticSetSize
        };
        
        public static BurnTester NewWarmup(long staticSetSize) => new BurnTester() {
            StaticSetSize = staticSetSize,
            Duration = TimeSpan.FromSeconds(1), 
            Writer = new IndentedTextWriter(TextWriter.Null),
        };

        public void TryInitialize()
        {
            if (_isInitialized)
                return;
            
            _allocations = new (int, sbyte, sbyte) [AllocationSequenceLength];
            var sizeSampler = CreateSizeSampler(Random).Truncate(1, MaxSize);
            var timeSampler = CreateTimeSampler(Random).Truncate(0, MaxTime);
            var releaseCycleTimeInSeconds = 1.0 * GarbageHolder.TicksPerReleaseCycle / Stopwatch.Frequency;
            var timeFactor = TimeSamplerUnitInSeconds / releaseCycleTimeInSeconds;
            for (var i = 0; i < AllocationSequenceLength; i++) {
                var size = (int) sizeSampler.Sample();
                var arraySize = GarbageAllocator.ByteSizeToArraySize(size);
                
                var time = timeSampler.Sample();
                var timeStr = ((long) (time * timeFactor)).ToString();
                var bucketIndex = (sbyte) (timeStr.Length - 1);
                var generationIndex = (sbyte) (timeStr[0] - '0');
                
                _allocations[i] = (arraySize, bucketIndex, generationIndex);
            }

            _startIndexes = Enumerable.Range(0, ParallelRunner.ThreadCount)
                .Select(_ => Random.Next() % AllocationSequenceLength)
                .ToArray();
            
            // Creating _staticSet
            var startOffset = Random.Next() % AllocationSequenceLength;
            _staticSet = ParallelRunner.New(i => new SetAllocator(
                    StaticSetSize / ParallelRunner.ThreadCount,
                    _allocations,
                    (_startIndexes[i] + startOffset) % AllocationSequenceLength)
                )
                .Run()
                .SelectMany(a => a.Set)
                .ToArray();
            
            GC.Collect(); // To make it uniform w/ Go test

            _isInitialized = true;
        }

        public void Run()
        {
            TryInitialize();
            
            var testDuration = Duration.TotalSeconds;
            var threadCount = ParallelRunner.ThreadCount;
            using (Writer.Section($"Test settings:")) {
                Writer.AppendMetric("Duration", testDuration, "s");
                Writer.AppendMetric("Thread count", threadCount, "");
                using (Writer.Section($"Static set:")) {
                    Writer.AppendMetric("Total size", StaticSetSize / Sizes.GB, "GB");
                    Writer.AppendMetric("Object count", _staticSet.Sum(a => (long) a.Length) / Sizes.Mega, "M");
                }
            }

            var runner = ParallelRunner.New(i => new GarbageAllocator(Duration, _allocations, _startIndexes[i]));
            var gcCounts = GCInfo.GetGCCounts();
            var memUsedBefore = GC.GetTotalMemory(false);
            var startTimestamp = Stopwatch.GetTimestamp();
            var allocators = runner.Run();
            var memUsedAfter = GC.GetTotalMemory(false);

            Writer.AppendLine();

            // Normalizing GCPauses (converting time to ms, removing offset)
            var ticksToMilliseconds = 1000.0 / Stopwatch.Frequency;
            foreach (var a in allocators) {
                a.GCPauses = a.GCPauses
                    .Select(p => p
                        .MoveBy(-startTimestamp)
                        .ExpandBy(-0.5)
                        .ScaleBy(ticksToMilliseconds))
                    .ToCanonicalSorted()
                    .ToList();
            }

            // Computing thread & global pauses
            var allPauses = allocators.SelectMany(a => a.GCPauses);
            var duration = allPauses.Max(p => p.End) / 1000; // In seconds (as testDuration)
            var threadPauses = allocators
                .Select(a => a.GCPauses
                    .Select(p => p.Size)
                    .OrderBy(d => d)
                    .ToArray())
                .ToArray();
            var pauseCountPerSecondPerThread = allocators
                .SelectMany(a => a.GCPauses
                    .GroupBy(p => Math.Floor(p.Start / 1000))
                    .Select(g => (double) g.Count())
                )
                .OrderBy(c => c)
                .ToArray();
            var globalPauses = allocators
                .Select(a => a.GCPauses)
                .IntersectSortedTuples()
                .Select(p => p.Size)
                .OrderBy(d => d)
                .ToArray();
            var allocationSizes = _allocations
                .Select(a => (double) GarbageAllocator.ArraySizeToByteSize(a.ArraySize))
                .OrderBy(s => s)
                .ToArray();
            var allocationHoldDurations = _allocations
                .Select(a => Math.Pow(10, a.BucketIndex) * a.GenerationIndex 
                    * GarbageHolder.TicksPerReleaseCycle * ticksToMilliseconds)
                .OrderBy(s => s)
                .ToArray();

            Writer.AppendMetric("Actual duration", duration, "s");
            using (Writer.Section($"Allocation speed:")) {
                Writer.AppendMetric("Operations per second", allocators.Sum(a => a.AllocationCount) / duration / Sizes.Mega, "M/s");
                Writer.AppendMetric("Bytes per second", allocators.Sum(a => a.ByteCount)  / duration / Sizes.GB, "GB/s");
                using (Writer.Section($"Allocation stats:")) {
                    Writer.AppendHistogram("Size:", allocationSizes, "B");
                    Writer.AppendHistogram("Hold duration:", allocationHoldDurations, "ms");
                }
            }
            using (Writer.Section($"GC stats:")) {
                Writer.AppendValue("RAM used", $"{(memUsedBefore / Sizes.GB):0.###} -> {(memUsedAfter / Sizes.GB):0.###} GB");
                if (!GCSettings.IsServerGC) {
                    // This data doesn't seem to be correct when ServerGC is on
                    using (Writer.Section($"GC rate:")) {
                        foreach (var (c, i) in gcCounts.WithIndexes())
                            Writer.AppendMetric($"Gen{i}, # per second", c / duration, "/s");
                    }
                }
                using (Writer.Section($"Thread pauses:")) {
                    Writer.AppendMetric("% of time frozen", threadPauses.SelectMany(p => p).Sum() / 1000 / threadCount / duration * 100, "%");
                    Writer.AppendHistogram("# per second:", pauseCountPerSecondPerThread, "/s");
                }
                using (Writer.Section($"Global pauses:")) {
                    Writer.AppendMetric("% of time frozen", globalPauses.Sum() / 1000 / duration * 100, "%");
                    Writer.AppendMetric("# per second", globalPauses.Length / duration, "/s");
                    Writer.AppendHistogram("Pause duration:", globalPauses, "ms");
                }
            }
            Writer.AppendLine();
        }
    }
}
