using System;
using System.CodeDom.Compiler;
using System.Linq;
using System.Runtime;
using System.Runtime.InteropServices;
using CommandLine;
using Benchmarking.Common;
using GCBurn.BurnTest;
using GCBurn.SpeedTest;

namespace GCBurn 
{
    public class App
    {
        public class Options 
        {
            [Option('d', "duration", Required = false, HelpText = "Test pass duration, seconds",
                Default = 10)]
            public int? Duration { get; set; }

            [Option('m', "staticSetSize", Required = false, HelpText = "Static set size, GB",
                Default = null)] // null = same as physical RAM amount
            public string StaticSetSize { get; set; }

            [Option('t', "threads", Required = false, HelpText = "Number of threads to use",
                Default = null)]
            public string ThreadCount { get; set; }

            [Option('o', "maxSize", Required = false, HelpText = "Max. object size", 
                Default = null)] // null = don't change what's on start
            public string MaxSize { get; set; }
            
            [Option('r', "runTests", Required = false, HelpText = "Tests to run (a = raw allocation, b = burn)", 
                Default = null)] // null = don't change what's on start
            public string Tests { get; set; }
            
            [Option('l', "gcLatencyMode", Required = false, HelpText = "Latency mode", 
                Default = null)] // null = don't change what's on start
            public string GCLatencyMode { get; set; }
            
            [Option('p', "outputMode", Required = false, HelpText = "Output mode (f = full, m = minimal)", 
                Default = null)] // null = don't change what's on start
            public string OutputMode { get; set; }
        }
        
        public IndentedTextWriter Writer = new IndentedTextWriter(Console.Out, "  ");
        
        static int Main(string[] args)
        {
            try {
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(options => new App().Run(options))
                    .WithNotParsed(errors => {}); // Parse errors are printed by default
                return 0;
            }                                  
            catch (Exception e) {
                Console.WriteLine();
                Console.WriteLine($"Error: {e.Message}");
                return 1;
            }
        }
        
        public void Run(Options options)
        {
            // Applying options
            if (!string.IsNullOrEmpty(options.GCLatencyMode))
                GCSettings.LatencyMode = Enum.Parse<GCLatencyMode>(options.GCLatencyMode);
            if (options.Duration.HasValue)
                BurnTester.DefaultDuration = TimeSpan.FromSeconds(options.Duration.Value);
            BurnTester.DefaultMaxSize = ArgumentHelper.ParseRelativeValue(
                options.MaxSize, BurnTester.DefaultMaxSize, true);
            ParallelRunner.ThreadCount = (int) ArgumentHelper.ParseRelativeValue(
                options.ThreadCount, ParallelRunner.ThreadCount, true);
            var tests = options.Tests?.ToLowerInvariant() ?? "";
            var ramSizeGb = HardwareInfo.GetRamSize() ?? 4;
            var staticSetSizeGb = 0;
            if (!string.IsNullOrEmpty(options.StaticSetSize)) {
                tests += "b";
                staticSetSizeGb = (int) ArgumentHelper.ParseRelativeValue(options.StaticSetSize, ramSizeGb, true);
            }
            var outputMode = options.OutputMode ?? "f";

            if (outputMode == "f") {
                // Dumping environment info
                Writer.AppendValue("Launch parameters", string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
                using (Writer.Section("Software:")) {
                    Writer.AppendValue("Runtime", ".NET Core");
                    using (Writer.Indent()) {
                        Writer.AppendValue("Version", RuntimeInformation.FrameworkDescription);
                        Writer.AppendValue("GC mode", GCInfo.GetGCMode());
                    }

                    Writer.AppendValue("OS",
                        $"{RuntimeInformation.OSDescription.Trim()} ({RuntimeInformation.OSArchitecture})");
                }

                using (Writer.Section("Hardware:")) {
                    var coreCountAddon = ParallelRunner.ThreadCount != Environment.ProcessorCount
                        ? $", {ParallelRunner.ThreadCount} used by test"
                        : "";
                    Writer.AppendValue("CPU", HardwareInfo.GetCpuModelName());
                    Writer.AppendValue("CPU core count", $"{Environment.ProcessorCount}{coreCountAddon}");
                    Writer.AppendValue("RAM size", $"{ramSizeGb:N0} GB");
                }
                Writer.AppendLine();
            }
            
            RunWarmup();

            if (tests == "") {
                RunSpeedTest();
                RunBurnTest("--- Stateless server (no static set) ---", 0);
                RunBurnTest("--- Worker / typical server (static set = 20% RAM) ---", (long) (ramSizeGb * Sizes.GB / 5));
                RunBurnTest("--- Caching / compute server (static set = 50% RAM) ---", (long) (ramSizeGb * Sizes.GB / 2));
                return;
            }

            if (tests.Contains("a")) {
                RunSpeedTest();
            }
            if (tests.Contains("b")) {
                var title = $"--- Static set = {staticSetSizeGb} GB ({staticSetSizeGb * 100.0 / ramSizeGb:0.##} % RAM) ---";
                RunBurnTest(title, (long) (staticSetSizeGb * Sizes.GB));
            }
        }

        public void RunWarmup() 
        {
            var speedTester = SpeedTester.NewWarmup();
            speedTester.Run();
            var burnTester = BurnTester.NewWarmup(10 * (long) Sizes.MB);
            burnTester.Run();
        }

        public void RunSpeedTest()
        {
            Writer.AppendLine("--- Raw allocation (w/o holding what's allocated) ---");
            Writer.AppendLine();
            var speedTester = SpeedTester.New();
            speedTester.Run();
        }

        public void RunBurnTest(string title, long staticSetSize)
        {
            Writer.AppendLine(title);
            Writer.AppendLine();
            var burnTester = BurnTester.New(staticSetSize);
            burnTester.Run();
        }
    }
}
