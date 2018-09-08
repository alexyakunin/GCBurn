package main

import (
	bt "./burn_test"
	. "./common"
	st "./speed_test"
	"flag"
	"fmt"
	"time"
)

func main() {
	var ramSizeGbFlag = flag.Int64("m", 4, "RAM size, GB")
	var durationSecFlag = flag.Int64("d", 10, "Test pass duration, seconds")
	flag.Parse()
	ramSizeGb := *ramSizeGbFlag
	bt.DefaultDuration = time.Duration(*durationSecFlag * int64(time.Second))

	fmt.Printf("Environment:\n")
	fmt.Printf("  RAM size: %v GB\n", ramSizeGb)

	fmt.Printf("Warming up...\n")
	speedTester := st.NewWarmupSpeedTester()
	speedTester.Run()
	speedTester = nil
	warmupStaticSetSize := int64(ramSizeGb * GB * 2 / 3)
	warmupStaticSetSize = int64(1 * GB) // If this isn'burnTester done, it causes OOM
	burnTester := bt.NewWarmupBurnTester(warmupStaticSetSize)
	burnTester.Run()
	burnTester = nil
	fmt.Printf("  Done.\n")

	fmt.Println()
	fmt.Println("--- Raw allocation (w/o holding what's allocated) ---")
	fmt.Println()
	speedTester = st.NewSpeedTester()
	speedTester.Run()

	fmt.Println()
	fmt.Println("--- Caching / compute server (static set = 50% RAM) ---")
	fmt.Println()
	burnTester = bt.NewBurnTester(ramSizeGb * GB / 2) // 50%
	burnTester.Run()

	fmt.Println()
	fmt.Println("--- Worker / typical server (static set = 20% RAM) ---")
	fmt.Println()
	burnTester = bt.NewBurnTester(ramSizeGb * GB / 5) // 20%
	burnTester.Run()

	fmt.Println()
	fmt.Println("--- Stateless server (no static set) ---")
	fmt.Println()
	burnTester = bt.NewBurnTester(0)
	burnTester.Run()
}
