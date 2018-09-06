package burn_test

import (
	. "../common"
	"fmt"
	"math"
	"runtime"
	"strconv"
	"time"
)

const ArrayItemSize = 8
const ArrayItemSizeLog2 = 3
const MinArraySize = 24
const MinAllocationSize = MinArraySize + 8

const AllocationSequenceLength = 1 << 20 // 2^20, i.e. ~ 1M items; must be a power of 2!
const MinSize = 1
const MaxSize = 1 << 17 // 128KB
const TimeSamplerFrequency = 1000000
const TimeSamplerUnitInSeconds = 1.0 / TimeSamplerFrequency
const MaxTime = 1000 * TimeSamplerFrequency // 1000 seconds

var MinGCPause = time.Duration(10 * time.Microsecond).Nanoseconds()
var DefaultDuration = time.Duration(10 * time.Second)

type BurnTester struct {
	NoOutput      bool
	Duration      time.Duration
	ThreadCount   int
	StaticSetSize int64
	Allocations   []AllocationInfo
	StartIndexes  []int32
	StaticSet     [][]int64
	Random        *StdRandom
	isInitialized bool
}

func NewBurnTester(staticSetSize int64) *BurnTester {
	t := &BurnTester{}
	t.NoOutput = false
	t.Duration = DefaultDuration
	t.ThreadCount = runtime.NumCPU()
	t.StaticSetSize = staticSetSize
	t.Allocations = make([]AllocationInfo, AllocationSequenceLength)
	t.StartIndexes = make([]int32, AllocationSequenceLength)
	t.Random = NewStdRandom(123)
	return t
}

func NewWarmupBurnTester(staticSetSize int64) *BurnTester {
	t := NewBurnTester(staticSetSize)
	t.NoOutput = true
	t.Duration = time.Duration(1 * time.Second)
	return t
}

func (t *BurnTester) TryInitialize() {
	if t.isInitialized {
		return
	}

	runtime.GC() // Go fails with OOM on initialization, so trying this...

	sizeSampler := Truncate(CreateStandardSizeSampler(t.Random), MinSize, MaxSize)
	timeSampler := Truncate(CreateStandardTimeSampler(t.Random), 0, MaxTime)
	releaseCycleTimeInSeconds := Giga / NanosecondsPerReleaseCycle
	timeFactor := TimeSamplerUnitInSeconds / releaseCycleTimeInSeconds
	for i := 0; i < AllocationSequenceLength; i++ {
		size := int32(sizeSampler.Sample())
		arraySize := ByteSizeToArraySize(size)

		_time := timeSampler.Sample()
		_timeStr := strconv.Itoa(int(_time * timeFactor))
		bucketIndex := int8(len(_timeStr) - 1)
		generationIndex := int8(_timeStr[0] - '0')

		t.Allocations[i] = AllocationInfo{arraySize, bucketIndex, generationIndex}
	}

	// Creating staticSet
	var staticSet [][]int64
	var index = t.Random.Next() % AllocationSequenceLength
	var currentSize int64 = 0
	for currentSize < t.StaticSetSize {
		arraySize := t.Allocations[index].ArraySize
		staticSet = append(staticSet, CreateGarbage(arraySize))
		currentSize += ArraySizeToByteSize(arraySize)
		index = (index + 1) % AllocationSequenceLength
	}
	t.StaticSet = staticSet

	for i := 0; i < t.ThreadCount; i++ {
		t.StartIndexes[i] = int32(t.Random.Next()) % AllocationSequenceLength
	}

	t.isInitialized = true
}

func (t *BurnTester) Run() {
	t.TryInitialize()
	runtime.GC()

	if !t.NoOutput {
		fmt.Printf("Test settings:\n")
		fmt.Printf("  Duration:     %v second(memStatsBefore)\n", int(t.Duration.Seconds()))
		fmt.Printf("  Thread count: %v\n", t.ThreadCount)
		fmt.Printf("  Static set:\n")
		fmt.Printf("    Total size:     %.3f GB\n", float64(t.StaticSetSize)/GB)
		fmt.Printf("    Object count:   %.3f M\n", float64(len(t.StaticSet))/Mega)
		fmt.Println()
	}

	var done = make(chan bool)
	var allocators []*Allocator
	var startTime = float64(time.Now().UnixNano())
	for i := 0; i < t.ThreadCount; i++ {
		allocators = append(allocators, NewAllocator(t.Allocations, t.StartIndexes[i]))
	}

	memStatsBefore := new(runtime.MemStats)
	runtime.ReadMemStats(memStatsBefore)

	// Doing this step separately to make sure we Run this at almost the same time
	for _, a := range allocators {
		go a.Run(t.Duration, done)
	}
	// Wait for goroutines to complete
	for range allocators {
		<-done
	}

	memStatsAfter := new(runtime.MemStats)
	runtime.ReadMemStats(memStatsAfter)

	if t.NoOutput {
		return
	}

	const NanoToMilliseconds = 1000 / Giga // ns to ms

	// Normalizing GCPauses; nanoseconds to milliseconds and a bit more
	var pauses [][]Interval
	for _, a := range allocators {
		for j := range a.GCPauses {
			a.GCPauses[j].Start = (a.GCPauses[j].Start - startTime) * NanoToMilliseconds
			a.GCPauses[j].End = (a.GCPauses[j].End - startTime - 0.5) * NanoToMilliseconds

		}
		pauses = append(pauses, ToCanonicalSorted(a.GCPauses))
	}
	var intersections = pauses[0]
	for _, p := range pauses {
		intersections = IntersectSortedPairs(intersections, p)
	}
	var globalPauses []float64
	var globalPausesSum float64
	for _, p := range intersections {
		globalPauses = append(globalPauses, p.End-p.Start)
		globalPausesSum += p.End - p.Start
	}
	var allocationSizes []float64
	var allocationHoldDurations []float64
	for _, a := range allocators {
		for _, ai := range a.Allocations {
			t := math.Pow(10, float64(ai.BucketIndex)) * float64(ai.GenerationIndex) * (Giga / NanosecondsPerReleaseCycle) * NanoToMilliseconds
			allocationHoldDurations = append(allocationHoldDurations, t)
			allocationSizes = append(allocationSizes, float64(ArraySizeToByteSize(ai.ArraySize)))
		}
	}

	sec := t.Duration.Seconds()

	fmt.Printf("Allocation speed:\n")
	var ops, bytes int64
	for _, a := range allocators {
		ops += a.AllocationCount
		bytes += a.ByteCount
	}
	fmt.Printf("  Operations per second: %.2f M/memStatsBefore\n", float64(ops)/sec/Mega)
	fmt.Printf("  Bytes per second:      %.2f GB/memStatsBefore\n", float64(bytes)/sec/GB)
	fmt.Printf("  Allocation stats:\n")
	fmt.Printf("    Size:\n")
	DumpArrayStats(allocationSizes, "B", "      ", true)
	fmt.Printf("    Hold duration:\n")
	DumpArrayStats(allocationHoldDurations, "ms", "      ", true)
	fmt.Println()
	fmt.Printf("GC stats:\n")
	fmt.Printf("  RAM used:              %.3f -> %.3f GB\n", float64(memStatsBefore.Alloc)/GB, float64(memStatsAfter.Alloc)/GB)
	fmt.Printf("  GC rate:               %.3f /s\n", float64(memStatsAfter.NumGC-memStatsBefore.NumGC)/sec)
	fmt.Printf("  Allocation rate:       %.3f GB/s\n", float64(memStatsAfter.TotalAlloc-memStatsBefore.TotalAlloc)/sec/GB)
	fmt.Printf("  Free rate:             %.3f GB/s\n", float64(memStatsAfter.Frees-memStatsBefore.Frees)/sec/GB)
	fmt.Printf("  Global pauses:\n")
	fmt.Printf("    %% of time frozen:   %.3f %%\n", globalPausesSum/1000/sec*100)
	fmt.Printf("    # per second:        %.3f /s\n", float64(len(globalPauses))/sec)
	DumpArrayStats(globalPauses, "ms", "      ", true)
}
