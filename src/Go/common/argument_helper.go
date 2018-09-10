package common

import (
	"strconv"
	"strings"
)

func ParseRelative(value string, defaultValue float64, negativeSubtractsFromDefault bool) float64 {
	value = strings.Trim(value, " ")
	if value == "" {
		return defaultValue
	}

	offset := 0.0
	unit := 1.0
	if len(value) >= 1 && value[len(value)-1:] == "%" {
		value = strings.Trim(value[0:len(value)-1], " ")
		unit = defaultValue / 100
	} else if len(value) >= 3 && value[len(value)-3:] == "pct" {
		value = strings.Trim(value[0:len(value)-3], " ")
		unit = defaultValue / 100
	} else if len(value) >= 3 && value[len(value)-3:] == "pts" {
		value = strings.Trim(value[0:len(value)-3], " ")
		unit = defaultValue / 1000
	}
	if len(value) >= 1 && value[0:1] == "-" && negativeSubtractsFromDefault {
		offset = defaultValue
	}
	v, err := strconv.ParseFloat(value, 64)
	if err != nil {
		panic(err)
	}
	return offset + v*unit

}
