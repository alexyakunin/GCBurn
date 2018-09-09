package burn_test

type SetAllocator struct {
	MaxSetSize  int64
	Allocations []AllocationInfo
	StartIndex  int32
	Set         [][]int64
}

func NewSetAllocator(maxSize int64, allocations []AllocationInfo, startIndex int32) *SetAllocator {
	a := &SetAllocator{
		MaxSetSize:  maxSize,
		Allocations: allocations,
		StartIndex:  startIndex,
	}
	return a
}

func (a *SetAllocator) Run() {
	maxSetSize := a.MaxSetSize
	result := make([][]int64, 0)
	index := a.StartIndex
	currentSize := int64(0)
	for currentSize < maxSetSize {
		arraySize := a.Allocations[index].ArraySize
		result = append(result, CreateGarbage(arraySize))
		currentSize += ArraySizeToByteSize(arraySize)
		index = (index + 1) % AllocationSequenceLength
	}
	a.Set = result
}
