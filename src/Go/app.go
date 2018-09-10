package main

import (
	bt "./burn_test"
	. "./common"
	st "./speed_test"
	"flag"
	"fmt"
	"os"
	"runtime"
	"time"
)

func main() {
	var durationSecFlag = flag.Int64("d", 10, "Test pass duration, seconds")
	var ramSizeGbFlag = flag.String("m", "", "RAM size, GB")
	var threadCountFlag = flag.String("t", "", "Number of threads to use")
	var maxSizeFlag = flag.String("s", "", "Max. object size")
	var _ = flag.String("l", "", "Latency mode (ignored for Go)")
	flag.Parse()
	bt.DefaultDuration = time.Duration(*durationSecFlag * int64(time.Second))
	bt.DefaultMaxSize = ParseRelative(*maxSizeFlag, bt.DefaultMaxSize, true)
	ramSizeGb := int64(ParseRelative(*ramSizeGbFlag, 4, true))
	ThreadCount = int(ParseRelative(*threadCountFlag, float64(ThreadCount), true))

	args := fmt.Sprintf("%+v", os.Args[1:])
	fmt.Printf("Launch parameters: %v\n", args[1:len(args)-1])
	fmt.Printf("Software:\n")
	fmt.Printf("  Runtime:         Go\n")
	fmt.Printf("    Version:       %v\n", runtime.Version())
	fmt.Printf("  OS:              %v (%v)\n", runtime.GOOS, runtime.GOARCH)
	fmt.Printf("Hardware:\n")
	coreCountAddon := ""
	if runtime.NumCPU() != ThreadCount {
		coreCountAddon = fmt.Sprintf(" (assuming %v during the test)", ThreadCount)
	}
	fmt.Printf("  CPU core count:  %v%v\n", runtime.NumCPU(), coreCountAddon)
	fmt.Printf("  RAM size:        n/a (assuming %v GB during the test)\n", ramSizeGb)
	runtime.NumCPU()

	fmt.Printf("Warming up...\n")
	speedTester := st.NewWarmupSpeedTester()
	speedTester.Run()
	speedTester = nil
	burnTester := bt.NewWarmupBurnTester(1 * int64(GB))
	burnTester.Run()
	burnTester = nil
	fmt.Printf("  Done.\n")
	fmt.Println()

	fmt.Println("--- Raw allocation (w/o holding what's allocated) ---")
	fmt.Println()
	speedTester = st.NewSpeedTester()
	speedTester.Run()

	fmt.Println("--- Stateless server (no static set) ---")
	fmt.Println()
	burnTester = bt.NewBurnTester(0)
	burnTester.Run()

	fmt.Println("--- Worker / typical server (static set = 20% RAM) ---")
	fmt.Println()
	burnTester = bt.NewBurnTester(ramSizeGb * GB / 5) // 20%
	burnTester.Run()

	fmt.Println("--- Caching / compute server (static set = 50% RAM) ---")
	fmt.Println()
	burnTester = bt.NewBurnTester(ramSizeGb * GB / 2) // 50%
	burnTester.Run()
}
