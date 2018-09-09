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
            [Option('m', "ram", Required = false, HelpText = "RAM size, GB",
                Default = null)] // null = same as physical RAM amount
            public int? RamSize { get; set; }

            [Option('d', "duration", Required = false, HelpText = "Test pass duration, seconds",
                Default = 10)]
            public int? Duration { get; set; }

            [Option('l', "gcLatencyMode", Required = false, HelpText = "Latency mode", 
                Default = null)] // null = don't change what's on start
            public string GCLatencyMode { get; set; }
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
            var hardwareRamSize = HardwareInfo.GetRamSize();
#if DEBUG
            hardwareRamSize = 4;
#endif
            var testedRamSize = options.RamSize ?? hardwareRamSize;

            // Dumping environment info
            using (Writer.Section("Test:")) {
                Writer.AppendValue("Runtime", ".NET Core / C#");
                Writer.AppendValue("Arguments", string.Join(" ", Environment.GetCommandLineArgs().Skip(1)));
            }
            using (Writer.Section("Environment:")) {
                using (Writer.Section("Hardware:")) {
                    Writer.AppendValue($"CPU", $"{HardwareInfo.GetCpuModelName()}, {Environment.ProcessorCount} core(s)");
                    var ramSizeString = hardwareRamSize.ToString("N0", "GB") + (
                        options.RamSize != null 
                            ? $" (assuming {options.RamSize.ToString("N0", "GB")} during the test)" 
                            : "");  
                    Writer.AppendValue("RAM size", ramSizeString);
                }
                using (Writer.Section("Operating system:")) {
                    Writer.AppendValue("Version", RuntimeInformation.OSDescription.Trim());
                    Writer.AppendValue("Architecture", RuntimeInformation.OSArchitecture.ToString());
                }
                using (Writer.Section(".NET Framework:")) {
                    Writer.AppendValue("Version", RuntimeInformation.FrameworkDescription);
                    Writer.AppendValue("GC mode", GCInfo.GetGCMode());
                }
            }

            // Checking whether we actually know the RAM size to test
            if (!testedRamSize.HasValue)
                throw new ApplicationException("Can't proceed: neither hardware nor assumed RAM size is available.");
            var ramSize = testedRamSize.Value * Sizes.GB;

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
