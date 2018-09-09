package burn_test

import (
	. "../common"
	. "../common/nanotime"
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
const MaxTime = 1000 * TimeSamplerFrequency // 1000 seconds

var MinGCPause = time.Duration(10 * time.Microsecond).Nanoseconds()
var DefaultDuration = time.Duration(10 * time.Second)

type BurnTester struct {
	NoOutput       bool
	Duration       time.Duration
	StaticSetSize  int64
	Allocations    []AllocationInfo
	StartIndexes   []int32
	StaticSet      [][][]int64
	StaticSetCount int64
	Random         *StdRandom
	isInitialized  bool
}

func NewBurnTester(staticSetSize int64) *BurnTester {
	t := &BurnTester{
		NoOutput:       false,
		Duration:       DefaultDuration,
		StaticSetSize:  staticSetSize,
		Allocations:    make([]AllocationInfo, AllocationSequenceLength),
		StartIndexes:   make([]int32, AllocationSequenceLength),
		StaticSet:      make([][][]int64, 0),
		StaticSetCount: 0,
		Random:         NewStdRandom(123),
	}
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

	sizeSampler := Truncate(CreateStandardSizeSampler(t.Random), MinSize, MaxSize)
	timeSampler := Truncate(CreateStandardTimeSampler(t.Random), 0, MaxTime)
	releaseCycleTimeInSeconds := NanosecondsPerReleaseCycle * Nano
	timeFactor := (1.0 / TimeSamplerFrequency) / releaseCycleTimeInSeconds
	for i := 0; i < AllocationSequenceLength; i++ {
		size := int32(sizeSampler.Sample())
		arraySize := ByteSizeToArraySize(size)

		_time := timeSampler.Sample()
		_timeStr := strconv.Itoa(int(_time * timeFactor))
		bucketIndex := int8(len(_timeStr) - 1)
		generationIndex := int8(_timeStr[0] - '0')

		t.Allocations[i] = AllocationInfo{arraySize, bucketIndex, generationIndex}
	}

	for i := 0; i < ThreadCount; i++ {
		t.StartIndexes[i] = int32(t.Random.Next()) % AllocationSequenceLength
	}

	startOffset := int32(t.Random.Next()) % AllocationSequenceLength
	runner := NewParallelRunner(func(i int) IActivity {
		return NewSetAllocator(
			t.StaticSetSize/int64(ThreadCount),
			t.Allocations,
			(startOffset+t.StartIndexes[i])%AllocationSequenceLength)
	})
	activities := runner.Run()
	for _, a := range activities {
		allocator := a.(*SetAllocator)
		t.StaticSet = append(t.StaticSet, allocator.Set)
		t.StaticSetCount += int64(len(allocator.Set))
	}

	runtime.GC()

	t.isInitialized = true
}

func (t *BurnTester) Run() {
	t.TryInitialize()

	duration := t.Duration.Seconds()
	if !t.NoOutput {
		fmt.Printf("Test settings:\n")
		fmt.Printf("  Duration:          %v s\n", int(duration))
		fmt.Printf("  Thread count:      %v\n", ThreadCount)
		fmt.Printf("  Static set:\n")
		fmt.Printf("    Total size:      %.3f GB\n", float64(t.StaticSetSize)/GB)
		fmt.Printf("    Object count:    %.3f M\n", float64(t.StaticSetCount)/Mega)
		fmt.Println()
	}

	runner := NewParallelRunner(func(i int) IActivity { return NewGarbageAllocator(t.Duration, t.Allocations, t.StartIndexes[i]) })

	msPre := new(runtime.MemStats)
	runtime.ReadMemStats(msPre)
	startTime := float64(Nanotime().Nanoseconds())
	activities := runner.Run()
	msPost := new(runtime.MemStats)
	runtime.ReadMemStats(msPost)

	// Slice item casting
	allocators := make([]*GarbageAllocator, 0)
	for _, a := range activities {
		allocators = append(allocators, a.(*GarbageAllocator))
	}

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
			t := math.Pow(10, float64(ai.BucketIndex)) * float64(ai.GenerationIndex) * NanosecondsPerReleaseCycle * NanoToMilliseconds
			allocationHoldDurations = append(allocationHoldDurations, t)
			allocationSizes = append(allocationSizes, float64(ArraySizeToByteSize(ai.ArraySize)))
		}
	}

	fmt.Printf("Allocation speed:\n")
	var ops, bytes int64
	for _, a := range allocators {
		ops += a.AllocationCount
		bytes += a.ByteCount
	}
	fmt.Printf("  Operations per second: %.2f M/s\n", float64(ops)/duration/Mega)
	fmt.Printf("  Bytes per second:      %.2f GB/s\n", float64(bytes)/duration/GB)
	fmt.Printf("  Allocation stats:\n")
	fmt.Printf("    Size:\n")
	DumpArrayStats(allocationSizes, "B", "      ", true)
	fmt.Printf("    Hold duration:\n")
	DumpArrayStats(allocationHoldDurations, "ms", "      ", true)
	fmt.Println()
	fmt.Printf("GC stats:\n")
	fmt.Printf("  RAM used:              %.3f -> %.3f GB\n", float64(msPre.HeapAlloc)/GB, float64(msPost.HeapAlloc)/GB)
	fmt.Printf("  GC rate:               %.3f /s\n", float64(msPost.NumGC-msPre.NumGC)/duration)
	fmt.Printf("  Allocation rate:\n")
	fmt.Printf("    Objects:             %.3f M/s, %.3f M/s freed\n",
		float64(msPost.Mallocs-msPre.Mallocs)/duration/Mega,
		float64(msPost.Frees-msPre.Frees)/duration/Mega)
	fmt.Printf("    Bytes:               %.3f GB/s\n", float64(msPost.TotalAlloc-msPre.TotalAlloc)/duration/GB)
	fmt.Printf("  Global pauses:\n")
	fmt.Printf("    %% of time frozen:    %.3f %%, %.3f %% as reported by runtime\n",
		globalPausesSum/1000/duration*100,
		float64(msPost.PauseTotalNs-msPre.PauseTotalNs)/Giga/duration*100)
	fmt.Printf("    # per second:        %.3f /s\n", float64(len(globalPauses))/duration)
	DumpArrayStats(globalPauses, "ms", "      ", true)
	fmt.Println()
}
