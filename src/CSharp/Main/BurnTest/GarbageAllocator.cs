using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Benchmarking.Common;

namespace GCBurn.BurnTest
{
    public class GarbageAllocator : IActivity
    {
        public const int ArrayItemSize = 8;
        public const int ArrayItemSizeLog2 = 3;
        public const int ObjectSize = 16; // Object header size on x64 platform
        public const int MinArraySize = ObjectSize + 8; // Object header + length
        public const int MinAllocationSize = MinArraySize + 8; // Array size + pointer size
        public const int AvgPauseFrequency = 12000;

        public TimeSpan RunDuration = TimeSpan.Zero;
        public long MinGCPause = Stopwatch.Frequency / 100_000; // 0.01 ms
        public (int ArraySize, sbyte BucketIndex, sbyte GenerationIndex)[] Allocations;
        public GarbageHolder GarbageHolder = new GarbageHolder();
        public int StartIndex;

        // Statistics
        public long AllocationCount;
        public long ByteCount;
        public List<Interval> GCPauses; 

        public GarbageAllocator(TimeSpan runDuration, (int, sbyte, sbyte)[] allocations, int startIndex)
        {
            RunDuration = runDuration;
            Allocations = allocations;
            StartIndex = startIndex;
            GCPauses = new List<Interval>((int) (runDuration.TotalSeconds * AvgPauseFrequency)); // No reallocations during the test
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ArraySizeToByteSize(int arraySize) => MinAllocationSize + (arraySize << ArrayItemSizeLog2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int ByteSizeToArraySize(int byteSize) =>
            Math.Max(0, (byteSize - MinAllocationSize + ArrayItemSize - 1) / ArrayItemSize);
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static object CreateGarbage(int arraySize) => new long[arraySize];

        public void Run()
        {
            var duration = RunDuration;
            var allocations = Allocations;
            var gh = GarbageHolder;
            var gcPauses = GCPauses;

            var allocationIndex = StartIndex;
            int allocationIndexMask = Allocations.Length - 1; 

            GarbageHolder.Start();
            var lastTimestamp = Stopwatch.GetTimestamp();
            var endTimestamp = lastTimestamp + (long) (duration.TotalSeconds * Stopwatch.Frequency);

            while (lastTimestamp < endTimestamp) {
                var (arraySize, bucketIndex, generationIndex) = allocations[allocationIndex];
                allocationIndex = (allocationIndex + 1) & allocationIndexMask;
                
                // Allocation
                var garbage = CreateGarbage(arraySize);
                gh.AddGarbage(garbage, bucketIndex, generationIndex);
                AllocationCount++;
                ByteCount += ArraySizeToByteSize(arraySize);
                
                // Releasing what should be released by now
                if ((allocationIndex & 15) == 0)
                    gh.TryReleaseGarbage();

                // GC check
                var elapsed = Stopwatch.GetTimestamp();
                var diff = elapsed - lastTimestamp;
                if (diff >= MinGCPause)
                    gcPauses.Add(new Interval(lastTimestamp, elapsed));
                lastTimestamp = elapsed;
            }
        }
    }
}
