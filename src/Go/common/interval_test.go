package common

import (
	"testing"
)

func TestBasic(t *testing.T) {
	var s1 = []Interval{Interval{0, 0}, Interval{1, 1}}
	if !s1[0].IsEmpty() || !s1[1].IsEmpty() {
		t.Errorf("expected empty intervals, got %v", s1)
	}
	var s2 = ToCanonicalSorted(s1)
	if !eq([]Interval{}, s2) {
		t.Errorf("ToCanonicalSorted failed, expected empty, got %v", s2)
	}

	s1 = []Interval{Interval{0, 1}, Interval{1, 10}}
	s2 = []Interval{Interval{0, 10}}
	if s1[0].UnionExtend(s1[1]) != s2[0] {
		t.Errorf("doesnot union")
	}
	if !eq(ToCanonicalSorted(s1), s2) {
		t.Errorf("ToCanonicalSorted failed, expected %v, got %v", s2, ToCanonicalSorted(s1))
	}

	s2 = []Interval{Interval{0.0, 1.0}, Interval{1, 2}, Interval{3, 4}, Interval{5, 6}}
	s2 = ToCanonicalSorted(s2)
	if len(s2) != 3 {
		t.Errorf("ToCanonicalSorted failed, expected len %v", 3)
	}
	if !eq(ToCanonicalSorted(s2), s2) {
		t.Errorf("ToCanonicalSorted failed, expected it would match to itself")
	}
	s1 = ToCanonicalSorted(s1)
	if !eq(IntersectSortedPairs(s1, s2), s2) {
		t.Errorf("IntersectSortedPairs %v and %v failed, expected %v, got %v", s1, s2, s2, IntersectSortedPairs(s1, s2))
	}
}

func eq(a, b []Interval) bool {
	if len(a) != len(b) {
		return false
	}
	for i, _ := range a {
		if a[i] != b[i] {
			return false
		}
	}
	return true
}

/*
        public void CombinedBasicTest()
        {
            var s1 = new [] {(0.0, 0.0), (1, 1)}.ToLongIntervals();
            Assert.Equal(Enumerable.Empty<Interval>(), s1);
            Assert.Equal(s1, s1.ToCanonicalSorted());

            s1 = new [] {(0.0, 1.0), (1, 10)}.ToLongIntervals();
            Assert.Equal(new [] {(0.0, 10.0)}.ToLongIntervals(), s1);
            Assert.Equal(s1, s1.ToCanonicalSorted());

            var s2 = new [] {(0.0, 1.0), (1, 2), (3, 4), (5, 6)}.ToLongIntervals();
            Assert.Equal(3, s2.Count());
            Assert.Equal(s2, s2.ToCanonicalSorted());

            Assert.Equal(s2, s1.IntersectSortedPairs(s2));
        }
    }
}
*/
