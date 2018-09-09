package speed_test

import (
	. "../common"
	"fmt"
	"runtime"
	"time"
)

var DefaultDuration = time.Duration(5 * time.Second)

type SpeedTester struct {
	NoOutput        bool
	Duration        time.Duration
	ThreadCount     int
	AllocationCount int64
}

func NewSpeedTester() *SpeedTester {
	t := &SpeedTester{}
	t.NoOutput = false
	t.Duration = DefaultDuration
	t.ThreadCount = runtime.NumCPU()
	return t
}

func NewWarmupSpeedTester() *SpeedTester {
	t := NewSpeedTester()
	t.NoOutput = true
	t.Duration = time.Duration(1 * time.Second)
	return t
}

func (t *SpeedTester) Run() {
	runtime.GC()
	duration := t.Duration.Seconds()

	if !t.NoOutput {
		fmt.Printf("Test settings:\n")
		fmt.Printf("  Duration:     %v s\n", int(duration))
		fmt.Printf("  Thread count: %v\n", t.ThreadCount)
		fmt.Println()
	}

	var done = make(chan bool)
	var allocators []*UnitAllocator
	for i := 0; i < t.ThreadCount; i++ {
		allocators = append(allocators, NewUnitAllocator())
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

	totalCount := int64(0)
	for _, a := range allocators {
		totalCount += a.AllocationCount
	}
	totalSize := totalCount * UnitSize

	fmt.Printf("Allocation speed:\n")
	fmt.Printf("  Operations per second: %.2f M/s\n", float64(totalCount)/duration/Mega)
	fmt.Printf("  Bytes per second:      %.2f GB/s\n", float64(totalSize)/duration/GB)
	fmt.Println()
	fmt.Printf("Runtime GC stats:\n")
	fmt.Printf("  RAM used:              %.3f -> %.3f GB\n", float64(memStatsBefore.Alloc)/GB, float64(memStatsAfter.Alloc)/GB)
	fmt.Printf("  GC rate:               %.3f /s\n", float64(memStatsAfter.NumGC-memStatsBefore.NumGC)/duration)
	fmt.Printf("  Allocation rate:       %.3f GB/s\n", float64(memStatsAfter.TotalAlloc-memStatsBefore.TotalAlloc)/duration/GB)
	fmt.Printf("  Free rate:             %.3f GB/s\n", float64(memStatsAfter.Frees-memStatsBefore.Frees)/duration/GB)
	fmt.Println()
}
