using System;
using System.Linq;
using System.Runtime;
using System.Text;

namespace Benchmarking.Common
{
    public static class GCInfo
    {
        public static int[] GetGCCounts() => 
            Enumerable.Range(0, GC.MaxGeneration + 1).Select(GC.CollectionCount).ToArray();

        public static int[] GetGCCountsSince(int[] now, int[] since) => 
            now.Select((c, i) => c - since[i]).ToArray();

        public static int[] GetGCCountsSince(int[] since) => 
            GetGCCountsSince(GetGCCounts(), since);

        public static string GetGCMode()
        {
            var isGCLargePagesOn = Environment.GetEnvironmentVariable("COMPlus_GCLargePages") == "1";
            var sb = new StringBuilder();
            sb.Append(GCSettings.IsServerGC ? "Server" : "Workstation").Append(" GC, ");
            sb.Append($"Latency mode: {GCSettings.LatencyMode}, ");
            sb.Append($"LOH compaction mode: {GCSettings.LargeObjectHeapCompactionMode}, ");
            sb.Append($"Large pages: {(isGCLargePagesOn ? "enabled" : "disabled")}, ");
            sb.Append($"Generations: 0..{GC.MaxGeneration}");
            return sb.ToString();
        }
    }
}
