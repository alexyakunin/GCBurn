package main

import (
	bt "./burn_test"
	. "./common"
	st "./speed_test"
	"flag"
	"fmt"
	"os"
	"time"
)

func main() {
	var ramSizeGbFlag = flag.Int64("m", 4, "RAM size, GB")
	var durationSecFlag = flag.Int64("d", 10, "Test pass duration, seconds")
	var _ = flag.String("l", "", "Latency mode (ignored for Go)")
	flag.Parse()
	ramSizeGb := *ramSizeGbFlag
	bt.DefaultDuration = time.Duration(*durationSecFlag * int64(time.Second))

	fmt.Printf("Test:\n")
	fmt.Printf("  Runtime:   Go\n")
	fmt.Printf("  Arguments: %+v\n", os.Args[1:])
	fmt.Printf("Environment:\n")
	fmt.Printf("  RAM size: %v GB\n", ramSizeGb)

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
