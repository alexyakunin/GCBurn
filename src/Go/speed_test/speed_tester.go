package speed_test

import (
	. "../common"
	"fmt"
	"time"
)

const PassCount = 30

var DefaultDuration = time.Duration(time.Millisecond)

type SpeedTester struct {
	NoOutput        bool
	Duration        time.Duration
	AllocationCount int64
}

func NewSpeedTester() *SpeedTester {
	t := &SpeedTester{
		NoOutput: false,
		Duration: DefaultDuration,
	}
	return t
}

func NewWarmupSpeedTester() *SpeedTester {
	t := NewSpeedTester()
	t.NoOutput = true
	t.Duration = time.Duration(5 * time.Millisecond)
	return t
}

func (t *SpeedTester) Run() {
	duration := t.Duration.Seconds()
	if !t.NoOutput {
		fmt.Printf("Test settings:\n")
		fmt.Printf("  Duration:     %v ms\n", int64(t.Duration.Seconds()/Milli))
		fmt.Printf("  Thread count: %v\n", ThreadCount)
		fmt.Printf("  Unit size:    %v B\n", UnitSize)
		fmt.Println()
	}

	totalCount := int64(0)
	for pass := 0; pass < PassCount; pass++ {
		runner := NewParallelRunner(func(i int) IActivity { return NewUnitAllocator(t.Duration) })
		activities := runner.Run()

		// Slice item casting
		allocators := make([]*UnitAllocator, 0)
		for _, a := range activities {
			allocators = append(allocators, a.(*UnitAllocator))
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

	fmt.Printf("Allocation speed:\n")
	fmt.Printf("  Operations per second: %.3f M/s\n", float64(totalCount)/duration/Mega)
	//fmt.Printf("  Bytes per second:      %.3f GB/s\n", float64(totalCount * UnitSize)/duration/GB)
	fmt.Println()
}
