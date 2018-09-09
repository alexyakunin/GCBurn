using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using System.Threading;
using Benchmarking.Common;
using GCBurn.BurnTest;

namespace GCBurn.SpeedTest 
{
    public class SpeedTester
    {
        public const int PassCount = 10;
        public static TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(1);
        public TimeSpan Duration = DefaultDuration;
        public int ThreadCount = Environment.ProcessorCount;
        public IndentedTextWriter Writer = new IndentedTextWriter(Console.Out, "  ");
        
        public static SpeedTester New() => new SpeedTester();
        public static SpeedTester NewWarmup() => new SpeedTester() {
            Duration = TimeSpan.FromSeconds(1), 
            Writer = new IndentedTextWriter(TextWriter.Null),
        };
        
        public void Run()
        {
            var duration = Duration.TotalSeconds;
            using (Writer.Section($"Test settings:")) {
                Writer.AppendMetric("Duration", duration * 1000, "ms");
                Writer.AppendMetric("Thread count", ThreadCount, "");
                Writer.AppendMetric("Unit size", UnitAllocator.UnitSize, "B");
            }
            Writer.AppendLine();

            var totalCount = 0L;
            for (var pass = 0; pass < PassCount; pass++) {
                var allocators = Enumerable.Range(0, ThreadCount)
                    .Select(i => new UnitAllocator())
                    .ToArray();
                var threads = allocators
                    .Select(a => new Thread(() => a.Run(Duration)))
                    .ToArray();

                GC.Collect();
                foreach (var thread in threads)
                    thread.Start();
                foreach (var thread in threads)
                    thread.Join();
                totalCount = Math.Max(totalCount, allocators.Sum(a => a.AllocationCount));
            } 
            
            var totalSize = totalCount * UnitAllocator.UnitSize;
            var totalSizeWithOverhead = totalCount * (UnitAllocator.UnitSize + GarbageAllocator.ObjectSize);
            using (Writer.Section($"Allocation speed:")) {
                Writer.AppendMetric("Operations per second", totalCount / Sizes.Mega / duration, "M/s");
                Writer.AppendMetric("Bytes per second", totalSize  / duration / Sizes.GB, "GB/s");
                Writer.AppendMetric("Bytes per second (incl. overhead)", totalSizeWithOverhead  / duration / Sizes.GB, "GB/s");
            }
            Writer.AppendLine();
        }
    }
}
