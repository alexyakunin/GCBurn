package speed_test

import (
	. "../common"
	"fmt"
	"runtime"
	"time"
)

const PassCount = 10
var DefaultDuration = time.Duration(time.Millisecond)

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
	duration := t.Duration.Seconds()
	if !t.NoOutput {
		fmt.Printf("Test settings:\n")
		fmt.Printf("  Duration:     %v s\n", int(duration))
		fmt.Printf("  Thread count: %v\n", t.ThreadCount)
		fmt.Println()
	}

	totalCount := int64(0)
	for pass := 0; pass < PassCount; pass++  {
		var done = make(chan bool)
		var allocators []*UnitAllocator
		for i := 0; i < t.ThreadCount; i++ {
			allocators = append(allocators, NewUnitAllocator())
		}

		runtime.GC()
		// Doing this step separately to make sure we Run this at almost the same time
		for _, a := range allocators {
			go a.Run(t.Duration, done)
		}
		// Wait for goroutines to complete
		for range allocators {
			<-done
		}

		tc := int64(0)
		for _, a := range allocators {
			tc += a.AllocationCount
		}
		if totalCount < tc {
			totalCount = tc
		}
	}

	if t.NoOutput {
		return
	}

	totalSize := totalCount * UnitSize

	fmt.Printf("Allocation speed:\n")
	fmt.Printf("  Operations per second: %.2f M/s\n", float64(totalCount)/duration/Mega)
	fmt.Printf("  Bytes per second:      %.2f GB/s\n", float64(totalSize)/duration/GB)
	fmt.Println()
}
