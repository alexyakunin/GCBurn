using System;
using System.Diagnostics;
using Benchmarking.Common;

namespace GCBurn.SpeedTest
{
    public class UnitAllocator : IActivity
    {
#pragma warning disable 169
        public class Unit { long Field1, Field2; }
#pragma warning restore 169

        public const long UnitSize = 2 * sizeof(long);
        public const int StepSize = 50;
        public TimeSpan RunDuration = TimeSpan.Zero;
        public long AllocationCount = 0;
        private object _sink;

        public UnitAllocator(TimeSpan runDuration) => RunDuration = runDuration;

        public void Run()
        {
            var duration = RunDuration;
            var lastTimestamp = Stopwatch.GetTimestamp();
            var endTimestamp = lastTimestamp + (long) (duration.TotalSeconds * Stopwatch.Frequency);
            var allocationCount = 0;
            object last = null;
            while (lastTimestamp < endTimestamp) {
                for (var i = 0; i < StepSize; i++)
                    last = new Unit();
                allocationCount += StepSize;
                lastTimestamp = Stopwatch.GetTimestamp();
            }            
            _sink = last; // Just to make sure JIT / compiler somehow doesn't eliminate the allocation at all
            AllocationCount += allocationCount;
        }
    }
}
