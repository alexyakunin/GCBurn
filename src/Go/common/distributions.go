package common

import (
	"errors"
	"math"
)

type Distribution interface {
	Sample() float64
}

type NormalDistribution struct {
	Mean, StdDev float64
	Rnd          *StdRandom
}

func NewNormalDistribution(r *StdRandom, mean, stddev float64) *NormalDistribution {
	return &NormalDistribution{Rnd: r, Mean: mean, StdDev: stddev}
}

func (d *NormalDistribution) Sample() float64 {
	for {
		if x, _, ok := polarTransform(d.Rnd.NextFloat64(), d.Rnd.NextFloat64()); ok {
			return d.Mean + x*d.StdDev
		}
	}
}

type LogNormalDistribution struct {
	Base NormalDistribution
}

func NewLogNormalDistribution(r *StdRandom, mu, sigma float64) *LogNormalDistribution {
	l := &LogNormalDistribution{}
	l.Base = *NewNormalDistribution(r, mu, sigma)
	return l
}

func (l *LogNormalDistribution) Sample() float64 {
	return math.Exp(l.Base.Sample())
}

type CombinedDistribution struct {
	Weights       []float64
	Distributions []Distribution
	Rnd           *StdRandom
}

func (c *CombinedDistribution) Check() error {
	if len(c.Distributions) < 1 {
		return errors.New("At least one distibution expected")
	}
	if len(c.Weights) != len(c.Distributions) {
		return errors.New("Numbers of Distributions and Weights don't match")
	}
	var sum float64
	for _, w := range c.Weights {
		sum += w
	}
	if math.Abs(sum-1) > 1e-8 {
		return errors.New("Weights must sum up to 1")
	}
	return nil
}

func (c *CombinedDistribution) Sample() float64 {
	expectedWeight := c.Rnd.NextFloat64()
	idx := 0
	weight := c.Weights[0]
	for expectedWeight >= weight {
		idx++
		weight += c.Weights[idx]
	}
	if idx >= len(c.Distributions) {
		idx = len(c.Distributions) - 1
	}
	return c.Distributions[idx].Sample()
}

// Private

func polarTransform(x01, y01 float64) (x, y float64, ok bool) {
	xn11 := 2*x01 - 1
	yn11 := 2*y01 - 1
	r2 := xn11*xn11 + yn11*yn11

	if r2 >= 1 || r2 == 0 {
		return 0, 0, false
	}

	factor := math.Sqrt(-2 * math.Log(r2) / r2)
	x = xn11 * factor
	y = yn11 * factor
	ok = true
	return
}
