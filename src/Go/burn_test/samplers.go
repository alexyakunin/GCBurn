package burn_test

import (
	. "../common"
	"log"
	"math"
)

func CreateStandardSizeSampler(rnd *StdRandom) Distribution {
	// Unit of size = 1 byte

	// Small objects: mean = 32B
	dTypical := NewNormalDistribution(rnd, 32, 64)
	// Large objects, the actual size is 2^n, i.e. mean = 2KB
	dLarge := Transform(NewNormalDistribution(rnd, 11, 1), func(x float64) float64 { return math.Pow(2, x) })
	// Extra large objects, the actual size is 2^n, i.e. mean = 64KB
	dxLarge := Transform(NewNormalDistribution(rnd, 16, 1), func(x float64) float64 { return math.Pow(2, x) })

	combined := &CombinedDistribution{
		[]float64{0.99, 0.0099, 0.0001},
		[]Distribution{dTypical, dLarge, dxLarge},
		rnd,
	}
	if err := combined.Check(); err != nil {
		log.Fatal(err)
	}
	return TruncateMin(combined, 0)
}

func CreateStandardTimeSampler(rnd *StdRandom) Distribution {
	// Unit of time = 1 microsecond
	dMethod := ReflectNegatives(NewNormalDistribution(rnd, 0, 0.1))  // ~ Method or a few
	dRequest := ReflectNegatives(NewNormalDistribution(rnd, 0, 1e5)) // ~ Request or a few
	dLongLiving := NewNormalDistribution(rnd, 1e7, 1e7)
	combined := &CombinedDistribution{
		[]float64{0.950, 0.049, 0.001},
		[]Distribution{dMethod, dRequest, dLongLiving},
		rnd,
	}
	if err := combined.Check(); err != nil {
		log.Fatal(err)
	}
	return TruncateMin(combined, 0)
}
