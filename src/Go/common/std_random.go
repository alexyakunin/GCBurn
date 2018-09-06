package common

const mersenne8 = 2147483647 // 2^31 - 1, 8th Mersenne prime
const multiplier = 48271     // Prime, same as in C++11 minstd_rand

type StdRandom struct {
	r int
}

func NewStdRandom(seed int) *StdRandom {
	return &StdRandom{r: seed}
}

func (r *StdRandom) Next() int {
	r.r = int(int64(r.r)*multiplier) % mersenne8
	return r.r
}

func (r *StdRandom) NextFloat64() float64 {
	return float64(r.Next()) / mersenne8
}
