// +build !windows

package nanotime

import (
	"time"
)

func Nanotime() time.Duration {
	return time.Since(StartTime)
}
