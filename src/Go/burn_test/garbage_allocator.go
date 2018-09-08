package burn_test

import (
	. "../common"
	. "../common/nanotime"
	"time"
)

type GarbageAllocator struct {
	Allocations   []AllocationInfo
	StartIndex    int32
	GarbageHolder *GarbageHolder

	// Statistics
	AllocationCount int64
	ByteCount       int64
	GCPauses        []Interval
}

type AllocationInfo struct {
	ArraySize       int32 // in 8 byte words (ints)
	BucketIndex     int8
	GenerationIndex int8
}

func NewGarbageAllocator(ai []AllocationInfo, startIndex int32) *GarbageAllocator {
	a := &GarbageAllocator{}
	a.Allocations = ai
	a.StartIndex = startIndex
	a.GarbageHolder = NewGarbageHolder()
	return a
}

func ByteSizeToArraySize(byteSize int32) int32 {
	s := (byteSize - MinAllocationSize + ArrayItemSize - 1) / ArrayItemSize
	if s < 0 {
		return 0
	}
	return s
}

func ArraySizeToByteSize(arraySize int32) int64 {
	return MinAllocationSize + (int64(arraySize) << ArrayItemSizeLog2)
}

func CreateGarbage(arraySize int32) []int64 {
	return make([]int64, arraySize)
}

func (a *GarbageAllocator) Run(duration time.Duration, done chan bool) {
	allocations := a.Allocations
	gh := a.GarbageHolder
	gcPauses := a.GCPauses

	allocationIndex := a.StartIndex
	allocationIndexMask := int32(len(allocations) - 1)

	a.GarbageHolder.Start()
	lastTimestamp := Nanotime().Nanoseconds()
	endTimestamp := lastTimestamp + duration.Nanoseconds()

	for lastTimestamp < endTimestamp {
		ai := allocations[allocationIndex]
		allocationIndex = (allocationIndex + 1) & allocationIndexMask

		// Allocation
		var garbage = CreateGarbage(ai.ArraySize)
		gh.AddGarbage(garbage, ai.BucketIndex, ai.GenerationIndex)

		a.AllocationCount++
		a.ByteCount += ArraySizeToByteSize(ai.ArraySize)

		// Releasing what should be released by now
		if (allocationIndex & 15) == 0 {
			gh.TryReleaseGarbage()
		}

		// GC check
		elapsed := Nanotime().Nanoseconds()
		diff := elapsed - lastTimestamp
		if diff >= MinGCPause {
			gcPauses = append(gcPauses, Interval{float64(lastTimestamp), float64(elapsed)})
		}
		lastTimestamp = elapsed
	}
	a.GCPauses = gcPauses
	done <- true
}
