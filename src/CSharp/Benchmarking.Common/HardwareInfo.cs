using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Medallion.Shell;

namespace Benchmarking.Common
{
    public static class HardwareInfo
    {
        public static string GetCpuModelName()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                var cmd = Command.Run("wmic", "cpu", "get", "name");
                return cmd.StandardOutput.GetLines().SkipEmpty().Last().Trim();
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                var cmd = Command.Run("cat", "/proc/cpuinfo");
                return cmd.StandardOutput.GetLines().SkipEmpty()
                    .ToPairs().First(p => p.Name == "model name").Value;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                var cmd = Command.Run("sysctl", "-n", "machdep.cpu.brand_string");
                return cmd.StandardOutput.GetLines().SkipEmpty().Last().Trim();
            }
            else 
                throw new NotSupportedException($"Unsupported OS: {RuntimeInformation.OSDescription}");
        }

        // In GB
        public static int? GetRamSize()
        {
            try {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                    var cmd = Command.Run("wmic", "computersystem", "get", "TotalPhysicalMemory");
                    var stringValue = cmd.StandardOutput.GetLines().SkipEmpty().Last().Trim();
                    return (int) Math.Round(long.Parse(stringValue) / Sizes.GB);
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) {
                    var cmd = Command.Run("cat", "/proc/meminfo");
                    var stringValue = cmd.StandardOutput.GetLines().SkipEmpty()
                        .ToPairs().Single(p => p.Name == "MemTotal")
                        .Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).First();
                    return (int) Math.Round(long.Parse(stringValue) * Sizes.KB / Sizes.GB);
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                    var cmd = Command.Run("system_profiler", "SPHardwareDataType");
                    var stringValue = cmd.StandardOutput.GetLines().SkipEmpty()
                        .ToPairs().Single(p => p.Name == "Memory")
                        .Value.Split(' ', StringSplitOptions.RemoveEmptyEntries).First();
                    return (int) Math.Round(double.Parse(stringValue));
                }
                return null;
            }
            catch {
                return null;
            }
        }
        
        private static IEnumerable<string> SkipEmpty(this IEnumerable<string> lines) =>
            lines.Select(l => l.TrimEnd()).Where(l => !string.IsNullOrEmpty(l));

        private static IEnumerable<(string Name, string Value)> ToPairs(
            this IEnumerable<string> lines, string separator = ":")
        {
            foreach (var line in lines) {
                var i = line.IndexOf(separator, StringComparison.Ordinal);
                if (i > 0)
                    yield return (line.Substring(0, i).Trim(), line.Substring(i + 1).Trim()); 
            }
        }
    }
}
