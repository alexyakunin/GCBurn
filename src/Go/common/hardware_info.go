package common

import (
	"errors"
	"fmt"
	"log"
	"math"
	"os/exec"
	"runtime"
	"strconv"
	"strings"
)

func GetOSVersion() string {
	switch runtime.GOOS {
	case "windows":
		return runAndGetLastLine("ver")
	case "darwin": // Mac OS X, iOS
		return runAndGetLastLine("system_profiler SPSoftwareDataType | awk '( $1 == \"System Version:\" ) { print $2 }'")
	default:
		return runAndGetLastLine("cat /proc/version")
	}
}

func GetCpuModelName() string {
	switch runtime.GOOS {
	case "windows":
		return runAndGetLastLine("wmic cpu get name")
	case "darwin": // Mac OS X, iOS
		return runAndGetLastLine("sysctl -n machdep.cpu.brand_string")
	default:
		return runAndGetLastLine("lscpu | grep 'Model name:' | sed --expression='s/.*:\\s*//g'")
	}
}

func GetRamSize() int {
	switch runtime.GOOS {
	case "windows":
		sizeB := runAndParseFloat("wmic computersystem get TotalPhysicalMemory")
		return int(math.Round(sizeB / GB))
	case "darwin": // Mac OS X, iOS
		sizeGb := runAndParseFloat("system_profiler SPHardwareDataType | awk '( $1 == \"Memory:\" ) { print $2 }'")
		return int(math.Round(sizeGb))
	default:
		sizeKb := runAndParseFloat("cat /proc/meminfo | awk '( $1 == \"MemTotal:\" ) { print $2 }'")
		return int(math.Round(sizeKb * KB / GB))
	}
}

func runAndParseFloat(cmd string) float64 {
	output := runAndGetLastLine(cmd)
	result, err := strconv.ParseFloat(output, 0)
	if err != nil {
		log.Fatal(err)
	}
	return result
}

func runAndGetLastLine(cmd string) string {
	output := run(cmd)
	output = strings.Replace(output, "\r\n", "\n", -1) // Windows output handling
	lines := strings.Split(output, "\n")
	for i := len(lines) - 1; i >= 0; i-- {
		r := strings.Trim(lines[i], " \t\r\n")
		if r != "" {
			return r
		}
	}
	return ""
}

func run(cmd string) string {
	command := "bash"
	arg := "-c"
	if runtime.GOOS == "windows" {
		command = "cmd"
		arg = "/C"
	}
	out, err := exec.Command(command, arg, cmd).Output()
	if err != nil {
		log.Fatal(err)
	}
	return string(out)
}

func failUnsupportedOS() {
	goos := runtime.GOOS
	err := errors.New(fmt.Sprintf("Unsupported OS: %v", goos))
	log.Fatal(err)
}
