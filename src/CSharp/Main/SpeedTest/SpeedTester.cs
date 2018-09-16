using System;
using System.CodeDom.Compiler;
using System.IO;
using System.Linq;
using Benchmarking.Common;
using GCBurn.BurnTest;

namespace GCBurn.SpeedTest 
{
    public class SpeedTester
    {
        public const int PassCount = 30;
        public static TimeSpan DefaultDuration = TimeSpan.FromMilliseconds(1);
        public TimeSpan Duration = DefaultDuration;
        public IndentedTextWriter Writer = new IndentedTextWriter(Console.Out, "  ");
        
        public static SpeedTester New() => new SpeedTester();
        public static SpeedTester NewWarmup() => new SpeedTester() {
            Duration = TimeSpan.FromMilliseconds(5), 
            Writer = new IndentedTextWriter(TextWriter.Null),
        };
        
        public void Run()
        {
            var duration = Duration.TotalSeconds;
            var threadCount = ParallelRunner.ThreadCount;
            using (Writer.Section($"Test settings:")) {
                Writer.AppendMetric("Duration", duration * 1000, "ms");
                Writer.AppendMetric("Thread count", threadCount, "");
                Writer.AppendMetric("Unit size", UnitAllocator.UnitSize, "B");
            }
            Writer.AppendLine();

            var totalCount = 0L;
            for (var pass = 0; pass < PassCount; pass++) {
                var runner = ParallelRunner.New(i => new UnitAllocator(Duration));
                var allocators = runner.Run();
                totalCount = Math.Max(totalCount, allocators.Sum(a => a.AllocationCount));
            } 
            
            var totalSize = totalCount * UnitAllocator.UnitSize;
            var totalSizeWithOverhead = totalCount * (UnitAllocator.UnitSize + GarbageAllocator.ObjectSize);
            using (Writer.Section($"Allocation speed:")) {
                Writer.AppendMetric("Operations per second", totalCount / Sizes.Mega / duration, "M/s");
//                Writer.AppendMetric("Bytes per second", totalSize  / duration / Sizes.GB, "GB/s");
//                Writer.AppendMetric("Bytes per second (incl. overhead)", totalSizeWithOverhead  / duration / Sizes.GB, "GB/s");
            }
            Writer.AppendLine();
        }
    }
}
