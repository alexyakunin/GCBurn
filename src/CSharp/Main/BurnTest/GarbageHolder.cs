using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace GCBurn.BurnTest
{
    // Non thread safe type!
    public class GarbageHolder
    {
        public static readonly long TicksPerReleaseCycle = Stopwatch.Frequency / 1_000_000; // 1M / second
        public const int BucketCount = 10;
        public const int BucketSize = 10;

        private readonly List<object>[][] _buckets;
        private readonly long[] _collected;
        private long _startTimestamp;

        public GarbageHolder()
        {
            _buckets = new List<object>[BucketCount][];
            _collected = new long[BucketCount];
            Reset();
        }

        public void Reset()
        {
            for (var i = 0; i < _buckets.Length; i++)
                _buckets[i] = Enumerable.Range(0, BucketSize).Select(_ => new List<object>()).ToArray();
        }

        public void Start()
        {
            _startTimestamp = Stopwatch.GetTimestamp();
            for (var i = 0; i < _collected.Length; i++)
                _collected[i] = 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddGarbage(object garbage, sbyte bucketIndex, sbyte generationIndex)
        {
            if (bucketIndex == 0 && generationIndex == 0)
                // Zero hold time = don't hold it
                return;
            _buckets[bucketIndex][generationIndex].Add(garbage);
        }


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void TryReleaseGarbage()
        {
            var collectionIndex = (Stopwatch.GetTimestamp() - _startTimestamp) / TicksPerReleaseCycle;
            TryReleaseGarbage(0, collectionIndex);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void TryReleaseGarbage(int bucketIndex, long generationCount)
        {
            while (generationCount != 0 && bucketIndex < _buckets.Length) {
                ref var collected = ref _collected[bucketIndex]; // ref is just to speed this up
                var remaining = generationCount - collected;
                collected = generationCount;
                if (remaining >= BucketSize)
                    remaining = BucketSize;
                var bucket = _buckets[bucketIndex];
                Array.Copy(bucket, remaining, bucket, 0, BucketSize - remaining);
                for (var i = remaining; i < BucketSize; i++)
                    bucket[i] = new List<object>();

                // Kind of a tail call
                bucketIndex++;
                generationCount /= BucketSize;
            }
        }
    }
}
