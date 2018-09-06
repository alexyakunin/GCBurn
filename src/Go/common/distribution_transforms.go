package common

import (
	"math"
)

type TransformedDistribution struct {
	d         Distribution
	transform func(float64) float64
}

func (t *TransformedDistribution) Sample() float64 {
	return t.transform(t.d.Sample())
}

func Transform(d Distribution, f func(float64) float64) Distribution {
	return &TransformedDistribution{
		d:         d,
		transform: f,
	}
}

func ReflectNegatives(d Distribution) Distribution {
	return &TransformedDistribution{
		d: d,
		transform: func(x float64) float64 {
			if x < 0 {
				return -x
			}
			return x
		},
	}
}

func Truncate(d Distribution, min, max float64) Distribution {
	return &TransformedDistribution{
		d:         d,
		transform: func(x float64) float64 { return math.Max(min, math.Min(max, x)) },
	}
}

func TruncateMin(d Distribution, min float64) Distribution {
	return &TransformedDistribution{
		d:         d,
		transform: func(x float64) float64 { return math.Max(x, min) },
	}
}

func TruncateMax(d Distribution, max float64) Distribution {
	return &TransformedDistribution{
		d:         d,
		transform: func(x float64) float64 { return math.Min(max, x) },
	}
}

func Quantize(d Distribution, quanta float64) Distribution {
	return &TransformedDistribution{
		d:         d,
		transform: func(x float64) float64 { return math.Floor(x / quanta) },
	}
}
