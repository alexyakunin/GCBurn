using System;
using System.Diagnostics;

namespace GCBurn.AllocationSpeedTest
{
    public class UnitAllocator
    {
        public class Unit { long Field1, Field2, Field3; }

        public const long UnitSize = 3 * sizeof(long);
        public const int StepSize = 10;
        public long AllocationCount = 0;

        private object _sink;
        
        public void Run(TimeSpan duration)
        {
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
