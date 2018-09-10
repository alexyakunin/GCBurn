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

            [Option('m', "ram", Required = false, HelpText = "RAM size, GB",
                Default = null)] // null = same as physical RAM amount
            public string RamSize { get; set; }

            [Option('t', "threads", Required = false, HelpText = "Number of threads to use",
                Default = null)]
            public string ThreadCount { get; set; }

            [Option('l', "gcLatencyMode", Required = false, HelpText = "Latency mode", 
                Default = null)] // null = don't change what's on start
            public string GCLatencyMode { get; set; }

            [Option('s', "maxSize", Required = false, HelpText = "Max. object size", 
                Default = null)] // null = don't change what's on start
            public string MaxSize { get; set; }
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
            var hardwareRamSize = HardwareInfo.GetRamSize();
            var testedRamSize = (int) ArgumentHelper.ParseRelativeValue(options.RamSize, hardwareRamSize ?? 4, true);

            // Dumping environment info
            Writer.AppendValue("Launch parameters", string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
            using (Writer.Section("Software:")) {
                Writer.AppendValue("Runtime", ".NET Core");
                using (Writer.Indent()) {
                    Writer.AppendValue("Version", RuntimeInformation.FrameworkDescription);
                    Writer.AppendValue("GC mode", GCInfo.GetGCMode());
                }
                Writer.AppendValue("OS", $"{RuntimeInformation.OSDescription.Trim()} ({RuntimeInformation.OSArchitecture})");
            }
            using (Writer.Section("Hardware:")) {
                var coreCountAddon = ParallelRunner.ThreadCount != Environment.ProcessorCount 
                    ? $" (assuming {ParallelRunner.ThreadCount} during the test)" 
                    : "";  
                Writer.AppendValue("CPU", HardwareInfo.GetCpuModelName());
                Writer.AppendValue("CPU core count", $"{Environment.ProcessorCount}{coreCountAddon}");
                var ramSizeAddon = testedRamSize != hardwareRamSize 
                        ? $" (assuming {testedRamSize} GB during the test)" 
                        : "";  
                Writer.AppendValue("RAM size", $"{hardwareRamSize.ToString("N0", "GB")}{ramSizeAddon}");
            }

            // Checking whether we actually know the RAM size to test
            var ramSize = testedRamSize * Sizes.GB;

            // Finally, running the test
            SpeedTester speedTester;
            BurnTester burnTester;
            using (Writer.Section("Warming up...")) {
                speedTester = SpeedTester.NewWarmup();
                speedTester.Run();
                speedTester = null;
                burnTester = BurnTester.NewWarmup(1 * (long) Sizes.GB); 
                burnTester.Run();
                burnTester = null;
                Writer.AppendLine("Done.");
            }
            Writer.AppendLine();

            Writer.AppendLine("--- Raw allocation (w/o holding what's allocated) ---");
            Writer.AppendLine();
            speedTester = SpeedTester.New();
            speedTester.Run();

            Writer.AppendLine("--- Stateless server (no static set) ---");
            Writer.AppendLine();
            burnTester = BurnTester.New(0);
            burnTester.Run();

            Writer.AppendLine("--- Worker / typical server (static set = 20% RAM) ---");
            Writer.AppendLine();
            burnTester = BurnTester.New((long) (ramSize * 0.2));
            burnTester.Run();
            
            Writer.AppendLine("--- Caching / compute server (static set = 50% RAM) ---");
            Writer.AppendLine();
            burnTester = BurnTester.New((long) (ramSize * 0.5));
            burnTester.Run();
        }
    }
}
