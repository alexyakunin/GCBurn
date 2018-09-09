using System.Collections.Generic;
using Benchmarking.Common;

namespace GCBurn.BurnTest
{
    public class SetAllocator : IActivity
    {
        public long MaxSetSize;
        public (int ArraySize, sbyte BucketIndex, sbyte GenerationIndex)[] Allocations;
        public int StartIndex;
        public object[][] Set;

        public SetAllocator(long maxSetSize, (int ArraySize, sbyte BucketIndex, sbyte GenerationIndex)[] allocations, int startIndex)
        {
            MaxSetSize = maxSetSize;
            Allocations = allocations;
            StartIndex = startIndex;
        }

        public void Run()
        {
            var maxSetSize = MaxSetSize;
            var allocations = Allocations;
            var index = StartIndex;

            var result = new List<object[]>();
            var currentSet = new List<object>();
            for (var currentSize = 0L; currentSize < maxSetSize;) {
                var (arraySize, _, _) = allocations[index];
                currentSet.Add(GarbageAllocator.CreateGarbage(arraySize));
                currentSize += GarbageAllocator.ArraySizeToByteSize(arraySize);
                index = (index + 1) % BurnTester.AllocationSequenceLength;
                if (currentSet.Count > 1000_000_000) {
                    // Workaround for .NET array size restriction
                    result.Add(currentSet.ToArray());
                    currentSet = new List<object>();
                }
            };
            result.Add(currentSet.ToArray());
            Set = result.ToArray();
        }
    }
}
