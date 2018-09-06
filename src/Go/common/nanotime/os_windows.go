package nanotime

import (
	"syscall"
	"time"
	"unsafe"
)

var _queryPerformanceCounter *syscall.Proc = nil
var _startPerformanceCounter int64 = 0
var _performanceCounterToNanosecondsMultiplier int64 = 1

func init() {
	dll, err := syscall.LoadDLL("kernel32.dll")
	if err == nil {
		_qpc, err1 := dll.FindProc("QueryPerformanceCounter")
		_qpf, err2 := dll.FindProc("QueryPerformanceFrequency")
		if err1 == nil && err2 == nil {
			var counter int64
			var frequency int64
			ret1, _, _ := _qpf.Call(uintptr(unsafe.Pointer(&frequency)))
			ret2, _, _ := _qpc.Call(uintptr(unsafe.Pointer(&counter)))
			if ret1 != 0 && ret2 != 0 {
				PerformanceCounterFrequency = frequency
				_queryPerformanceCounter = _qpc
				_startPerformanceCounter = counter
				_performanceCounterToNanosecondsMultiplier = 1000000000 / PerformanceCounterFrequency
			}
		}
	}
}

func Nanotime() time.Duration {
	if PerformanceCounterFrequency == 0 {
		return defaultNanotime()
	}
	var ctr int64
	ret, _, _ := _queryPerformanceCounter.Call(uintptr(unsafe.Pointer(&ctr)))
	if ret == 0 {
		return defaultNanotime()
	}

	return time.Duration((ctr - _startPerformanceCounter) * _performanceCounterToNanosecondsMultiplier)
}

func defaultNanotime() time.Duration {
	return time.Since(StartTime)
}
