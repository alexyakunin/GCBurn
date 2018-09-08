package speed_test

import (
	. "../common/nanotime"
	"time"
)

const UnitSize = 24
const StepSize = 50

type UnitAllocator struct {
	Last *AllocationUnit
	// Statistics
	AllocationCount int64
}

type AllocationUnit struct {
	Field1 int64
	Field2 int64
	Field3 int64
}

func NewUnitAllocator() *UnitAllocator {
	a := &UnitAllocator{}
	return a
}

func (a *UnitAllocator) Run(duration time.Duration, done chan bool) {
	lastTimestamp := Nanotime().Nanoseconds()
	endTimestamp := lastTimestamp + duration.Nanoseconds()
	last := new(AllocationUnit)

	for lastTimestamp < endTimestamp {
		for i := 0; i < StepSize; i++ {
			last = new(AllocationUnit)
		}
		a.AllocationCount += StepSize
		lastTimestamp = Nanotime().Nanoseconds()
	}
	a.Last = last
	done <- true
}
