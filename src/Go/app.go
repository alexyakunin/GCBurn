package main

import (
	bt "./burn_test"
	. "./common"
	st "./speed_test"
	"flag"
	"fmt"
	"os"
	"runtime"
	"strings"
	"time"
)

func main() {
	ramSizeGb := GetRamSize()
	var durationSecFlag = flag.Int64("d", 10, "Test pass duration, seconds")
	var staticSetSizeGbFlag = flag.String("m", "", "Static set size, GB")
	var threadCountFlag = flag.String("t", "", "Number of threads to use")
	var maxSizeFlag = flag.String("o", "", "Max. object size")
	var testsFlag = flag.String("r", "", "Tests to run (a = raw allocation, b = burn)")
	var outputModeFlag = flag.String("p", "", "Output mode (f = full, m = minimal)")
	var _ = flag.String("l", "", "Latency mode (ignored for Go)")
	flag.Parse()
	bt.DefaultDuration = time.Duration(*durationSecFlag * int64(time.Second))
	bt.DefaultMaxSize = ParseRelative(*maxSizeFlag, bt.DefaultMaxSize, true)
	ThreadCount = int(ParseRelative(*threadCountFlag, float64(ThreadCount), true))
	tests := strings.ToLower(*testsFlag)
	staticSetSizeGb := 0
	if *staticSetSizeGbFlag != "" {
		tests += "b"
		staticSetSizeGb = int(ParseRelative(*staticSetSizeGbFlag, float64(ramSizeGb), true))
	}
	outputMode := strings.ToLower(*outputModeFlag)
	if outputMode == "" {
		outputMode = "f"
	}

	if strings.Contains(outputMode, "f") {
		args := fmt.Sprintf("%+v", os.Args[1:])
		fmt.Printf("Launch parameters: %v\n", args[1:len(args)-1])
		fmt.Printf("Software:\n")
		fmt.Printf("  Runtime:         Go\n")
		fmt.Printf("    Version:       %v\n", runtime.Version())
		fmt.Printf("  OS:              %v %v\n", GetOSVersion(), runtime.GOARCH)
		fmt.Printf("Hardware:\n")
		fmt.Printf("  CPU:             %v\n", GetCpuModelName())
		coreCountAddon := ""
		if runtime.NumCPU() != ThreadCount {
			coreCountAddon = fmt.Sprintf(", %v used by test", ThreadCount)
		}
		fmt.Printf("  CPU core count:  %v%v\n", runtime.NumCPU(), coreCountAddon)
		fmt.Printf("  RAM size:        %v GB\n", ramSizeGb)
		fmt.Println()
	}

	runWarmup()

	if tests == "" {
		runSpeedTest()
		runBurnTest("--- Stateless server (no static set) ---", 0)
		runBurnTest("--- Worker / typical server (static set = 20% RAM) ---", int64(ramSizeGb)*GB/5)
		runBurnTest("--- Caching / compute server (static set = 50% RAM) ---", int64(ramSizeGb)*GB/2)
		return
	}

	if strings.Contains(tests, "a") {
		runSpeedTest()
	}
	if strings.Contains(tests, "b") {
		title := fmt.Sprintf("--- Static set = %v GB (%.2f %% RAM) ---",
			staticSetSizeGb,
			float64(staticSetSizeGb)*100/float64(ramSizeGb))
		runBurnTest(title, int64(staticSetSizeGb)*GB)
	}
}

func runWarmup() {
	speedTester := st.NewWarmupSpeedTester()
	speedTester.Run()
	burnTester := bt.NewWarmupBurnTester(10 * int64(MB))
	burnTester.Run()
}

func runSpeedTest() {
	fmt.Println("--- Raw allocation (w/o holding what's allocated) ---")
	fmt.Println()
	speedTester := st.NewSpeedTester()
	speedTester.Run()
}

func runBurnTest(title string, staticSetSize int64) {
	fmt.Println(title)
	fmt.Println()
	burnTester := bt.NewBurnTester(staticSetSize)
	burnTester.Run()
}
