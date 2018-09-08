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
        public static TimeSpan DefaultDuration = TimeSpan.FromSeconds(1);
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
            GC.Collect();
            var duration = Duration.TotalSeconds;

            using (Writer.Section($"Test settings:")) {
                Writer.AppendMetric("Duration", duration, "s");
                Writer.AppendMetric("Thread count", ThreadCount, "");
                Writer.AppendMetric("Unit size", UnitAllocator.UnitSize, "B");
            }
            
            var allocators = Enumerable.Range(0, ThreadCount)
                .Select(i => new UnitAllocator())
                .ToArray();
            var threads = allocators
                .Select(a => new Thread(() => a.Run(Duration)))
                .ToArray();

            foreach (var thread in threads)
                thread.Start();
            foreach (var thread in threads)
                thread.Join();

            Writer.AppendLine();

            // Computing thread & globalpauses
            var count = allocators.Sum(a => a.AllocationCount);
            var size = count * UnitAllocator.UnitSize;
            var fullSize = count * (UnitAllocator.UnitSize + GarbageAllocator.ObjectSize);

            using (Writer.Section($"Allocation speed:")) {
                Writer.AppendMetric("Operations per second", count / Sizes.Mega / duration, "M/s");
                Writer.AppendMetric("Bytes per second", size  / duration / Sizes.GB, "GB/s");
                Writer.AppendMetric("Bytes per second (incl. overhead)", fullSize  / duration / Sizes.GB, "GB/s");
            }
        }
    }
}
