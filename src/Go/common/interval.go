package common

import (
	"math"
)

var Empty = Interval{}

type Interval struct {
	Start, End float64
}

func (i Interval) IsEmpty() bool {
	return i.End-i.Start == 0
}

func (i Interval) UnionsWith(j Interval) bool {
	if i.Start <= j.Start && j.Start <= i.End {
		return true
	}
	if j.Start <= i.Start && i.Start <= j.End {
		return true
	}
	return false
}

func (i Interval) UnionExtend(j Interval) Interval {
	return Interval{math.Min(i.Start, j.Start), math.Max(i.End, j.End)}
}

func (i Interval) Intersect(j Interval) Interval {
	s := math.Max(i.Start, j.Start)
	e := math.Min(i.End, j.End)
	if e <= s {
		return Empty
	}
	return Interval{s, e}
}

func ToCanonicalSorted(in []Interval) (out []Interval) {
	last := in[0]
	for _, i := range in {
		if last.Start > i.Start && !last.IsEmpty() && !i.IsEmpty() {
			panic("The sequence is expected to be sorted.")
		}
		if last.UnionsWith(i) {
			last = last.UnionExtend(i)
		} else if !last.IsEmpty() {
			out = append(out, last)
			last = i
		}
	}
	if !last.IsEmpty() {
		out = append(out, last)
	}

	return out
}

func IntersectSortedPairs(a, b []Interval) (o []Interval) {
	var i, j int
	for {
		if i >= len(a) || j >= len(b) {
			break
		}
		intersection := a[i].Intersect(b[j])
		if !intersection.IsEmpty() {
			o = append(o, intersection)
		}
		if a[i].End < b[j].End {
			i++
		} else {
			j++
		}
	}
	return o
}
