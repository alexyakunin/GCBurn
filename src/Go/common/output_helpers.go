package common

import (
	"fmt"
	"sort"
)

func DumpArrayStats(data []float64, unit, indent string, includePercentiles bool) {
	if len(data) == 0 {
		fmt.Println(indent, "No data.")
		return
	}

	sort.Float64s(data)

	var min, max, sum, avg float64
	min = data[0]
	max = min
	for _, v := range data {
		if v < min {
			min = v
		}
		if v > max {
			max = v
		}
		sum += v
	}
	avg = sum / float64(len(data))

	fmt.Printf("%vMin .. Max:\n", indent)
	fmt.Printf("%v  Min:    %.3f %v\n", indent, min, unit)
	fmt.Printf("%v  Avg:    %.3f %v\n", indent, avg, unit)
	fmt.Printf("%v  Max:    %.3f %v\n", indent, max, unit)
	if !includePercentiles {
		return
	}

	maxIndex := len(data) - 1
	fmt.Printf("%vPercentiles:\n", indent)
	fmt.Printf("%v  50%%:    %.3f %v\n", indent, data[maxIndex*50/100], unit)
	fmt.Printf("%v  90%%:    %.3f %v\n", indent, data[maxIndex*90/100], unit)
	fmt.Printf("%v  95%%:    %.3f %v\n", indent, data[maxIndex*95/100], unit)
	fmt.Printf("%v  99%%:    %.3f %v\n", indent, data[maxIndex*99/100], unit)
	fmt.Printf("%v  99.9%%:  %.3f %v\n", indent, data[maxIndex*999/1000], unit)
	fmt.Printf("%v  99.99%%: %.3f %v\n", indent, data[maxIndex*9999/10000], unit)
}
