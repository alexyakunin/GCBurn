package burn_test

import (
	. "../common"
	. "../common/nanotime"
)

const NanosecondsPerReleaseCycle = Giga / 1e6 // 1M / second
const BucketCount = 10
const BucketSize = 10

type GarbageHolder struct {
	buckets   [BucketCount][BucketSize][][]int64
	collected []int
	startTime int64
}

func NewGarbageHolder() *GarbageHolder {
	g := &GarbageHolder{}
	g.collected = make([]int, BucketCount, BucketCount)
	return g
}

func (g *GarbageHolder) Reset() {
	for i := range g.buckets {
		for j := range g.buckets[i] {
			g.buckets[i][j] = nil
		}
	}
}

func (g *GarbageHolder) Start() {
	g.startTime = Nanotime().Nanoseconds()
	for i := range g.collected {
		g.collected[i] = 0
	}
}

func (g *GarbageHolder) AddGarbage(garbage []int64, bucket, generation int8) {
	if bucket == 0 && generation == 0 {
		// Zero hold time = don't hold it
		return
	}
	g.buckets[bucket][generation] = append(g.buckets[bucket][generation], garbage)
}

func (g *GarbageHolder) TryReleaseGarbage() {
	collectionIdx := int((Nanotime().Nanoseconds() - g.startTime) / NanosecondsPerReleaseCycle)
	g.release(0, collectionIdx)
}

func (g *GarbageHolder) release(bucket, generationCount int) {
	for generationCount != 0 && bucket < len(g.buckets) {
		remaining := generationCount - g.collected[bucket]
		g.collected[bucket] = generationCount
		if remaining >= BucketSize {
			remaining = BucketSize
		}
		for i := remaining; i < BucketSize; i++ {
			g.buckets[i-remaining] = g.buckets[i] // copy array
			for j := range g.buckets[i] {
				g.buckets[i][j] = nil
			}
		}
		bucket++
		generationCount /= BucketSize
	}
}
