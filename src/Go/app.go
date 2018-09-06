package main

import (
	. "./burn_test"
	. "./common"
	"flag"
	"fmt"
	"time"
)

func main() {
	var ramSizeGbFlag = flag.Int64("m", 4, "RAM size, GB")
	var durationSecFlag = flag.Int64("d", 10, "Test pass duration, seconds")
	flag.Parse()
	ramSizeGb := *ramSizeGbFlag
	DefaultDuration = time.Duration(*durationSecFlag * int64(time.Second))

	fmt.Printf("Environment:\n")
	fmt.Printf("  RAM size: %v GB\n", ramSizeGb)

	fmt.Printf("Warming up...\n")
	warmupStaticSetSize := int64(ramSizeGb * GB * 2 / 3)
	warmupStaticSetSize = int64(1 * GB) // If this isn't done, it causes OOM
	t := NewWarmupBurnTester(warmupStaticSetSize)
	t.Run()
	t = nil
	fmt.Printf("  Done.\n")

	fmt.Println()
	fmt.Println("--- Caching / compute server (static set = 50% RAM) ---")
	fmt.Println()
	t = NewBurnTester(ramSizeGb * GB / 2) // 50%
	t.Run()

	fmt.Println()
	fmt.Println("--- Worker / typical server (static set = 20% RAM) ---")
	fmt.Println()
	t = NewBurnTester(ramSizeGb * GB / 5) // 20%
	t.Run()

	fmt.Println()
	fmt.Println("--- Stateless server (no static set) ---")
	fmt.Println()
	t = NewBurnTester(0)
	t.Run()
}
