using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace JitBench
{
    class Program
    {
        public static void Main(string[] args)
        {
            CommandLineOptions options = CommandLineOptions.Parse(args);
            TestRun testRun = ConfigureTestRun(options);

            ConsoleTestOutputHelper console = new ConsoleTestOutputHelper();
            string logPath = Path.Combine(testRun.OutputDir, "JitBench_log.txt");
            FileTestOutputHelper logOutput = new FileTestOutputHelper(logPath);

            testRun.WriteConfiguration(console);
            testRun.WriteConfiguration(logOutput);
            console.WriteLine("");
            console.WriteLine("");
            console.WriteLine("Benchmark run in progress...");
            console.WriteLine("Verbose log: " + logPath);
            console.WriteLine("");

            testRun.Run(logOutput);
            testRun.WriteBenchmarkResults(console);
        }

        static TestRun ConfigureTestRun(CommandLineOptions options)
        {
            TestRun run = new TestRun()
            {
                OutputDir = GetInitialWorkingDir(),
                DotnetFrameworkVersion = JitBench.VersioningConstants.MicrosoftNETCoreAppVersion,
                Iterations = 11
            };

            if(options.OutputDirectory != null)
            {
                run.OutputDir = options.OutputDirectory;
            }

            if(options.CoreCLRBinaryDir != null)
            {
                if(!Directory.Exists(options.CoreCLRBinaryDir))
                {
                    throw new Exception("coreclr-bin-dir directory " + options.CoreCLRBinaryDir + " does not exist");
                }
                run.PrivateCoreCLRBinDir = options.CoreCLRBinaryDir;
            }
            else
            {
                string coreRootEnv = Environment.GetEnvironmentVariable("CORE_ROOT");
                if (coreRootEnv != null)
                {
                    if (!Directory.Exists(coreRootEnv))
                    {
                        throw new Exception("CORE_ROOT directory " + coreRootEnv + " does not exist");
                    }
                    run.PrivateCoreCLRBinDir = coreRootEnv;
                }
                else
                {
                    //maybe we've got private coreclr binaries in our current directory? Use those if so.
                    string currentDirectory = Directory.GetCurrentDirectory();
                    if(File.Exists(Path.Combine(currentDirectory, "System.Private.CoreLib.dll")))
                    {
                        run.PrivateCoreCLRBinDir = currentDirectory;
                    }
                    else
                    {
                        // don't use private CoreCLR binaries
                    }
                }
            }

            if(options.DotnetFrameworkVersion != null)
            {
                run.DotnetFrameworkVersion = options.DotnetFrameworkVersion;
            }

            if(options.DotnetSdkVersion != null)
            {
                run.DotnetSdkVersion = options.DotnetSdkVersion;
            }
            else
            {
                run.DotnetSdkVersion = DotNetSetup.GetCompatibleDefaultSDKVersionForRuntimeVersion(run.DotnetFrameworkVersion);
            }
            

            if(options.TargetArchitecture != null)
            {
                if(options.TargetArchitecture.Equals("x64", StringComparison.OrdinalIgnoreCase))
                {
                    run.Architecture = Architecture.X64;
                }
                else if(options.TargetArchitecture.Equals("x86", StringComparison.OrdinalIgnoreCase))
                {
                    run.Architecture = Architecture.X86;
                }
                else
                {
                    throw new Exception("Unrecognized architecture " + options.TargetArchitecture);
                }
            }
            else
            {
                run.Architecture = RuntimeInformation.ProcessArchitecture;
            }

            if(options.Iterations > 0)
            {
                run.Iterations = (int)options.Iterations;
            }

            run.UseExistingSetup = options.UseExistingSetup;
            run.BenchviewRunId = options.RunId ?? "Unofficial";
            run.MetricNames.AddRange(options.MetricNames);
            run.Benchmarks.AddRange(GetBenchmarkSelection(options));
            run.Configurations.AddRange(GetBenchmarkConfigurations(options));

            return run;
        }

        static string GetInitialWorkingDir()
        {
            string timestamp = DateTime.Now.ToString("yyyy\\_MM\\_dd\\_hh\\_mm\\_ss\\_ffff");
            return Path.Combine(Path.GetTempPath(), "JitBench_" + timestamp);
        }

        static IEnumerable<Benchmark> GetBenchmarkSelection(CommandLineOptions options)
        {
            if(options.BenchmarkName == null)
            {
                return GetAllBenchmarks();
            }
            else
            {
                string[] names = options.BenchmarkName.Split(';');
                return GetAllBenchmarks().Where(b => names.Any(n => n.Equals(b.Name, StringComparison.OrdinalIgnoreCase)));
            }
        }

        static IEnumerable<Benchmark> GetAllBenchmarks()
        {
            IEnumerable<Type> benchmarkTypes = typeof(Program).GetTypeInfo().Assembly.GetTypes().Where(t => typeof(Benchmark).IsAssignableFrom(t));
            foreach (Type bt in benchmarkTypes)
            {
                ConstructorInfo c = bt.GetConstructor(Type.EmptyTypes);
                if (c != null)
                {
                    yield return (Benchmark)c.Invoke(null);
                }
            }
        }

        static IEnumerable<BenchmarkConfiguration> GetBenchmarkConfigurations(CommandLineOptions options)
        {
            string tieredEnv = Environment.GetEnvironmentVariable("COMPlus_TieredCompilation");
            string minoptsEnv = Environment.GetEnvironmentVariable("COMPlus_JitMinopts");
            string r2rEnv = Environment.GetEnvironmentVariable("COMPlus_ReadyToRun");
            string noNgenEnv = Environment.GetEnvironmentVariable("COMPlus_ZapDisable");
            BenchmarkConfiguration envConfig = new BenchmarkConfiguration();
            if(tieredEnv != null && tieredEnv == "0")
            {
                envConfig.WithoutTiering();
            }
            if (minoptsEnv != null && minoptsEnv != "0")
            {
                envConfig.WithMinOpts();
            }
            if(r2rEnv != null && r2rEnv == "0")
            {
                envConfig.WithNoR2R();
            }
            if(noNgenEnv != null && noNgenEnv != "0")
            {
                envConfig.WithNoNgen();
            }

            string[] configNames = options.Configs.Distinct().ToArray();
            if (!envConfig.IsDefault && configNames.Length != 0)
            {
                throw new Exception("ERROR: Benchmarks cannot be configured via both environment variables and the --configs command line option at the same time. Use one or the other.");
            }
            if (configNames.Length == 0)
            {
                yield return envConfig;
                yield break;
            }

            // The minopts config name by itself implies without tiering
            var minOptsConfig = new BenchmarkConfiguration().WithMinOpts();
            string minOptsConfigName = minOptsConfig.Name;
            minOptsConfig = minOptsConfig.WithoutTiering();
            minOptsConfig.Name = minOptsConfigName;

            BenchmarkConfiguration[] possibleConfigs = new BenchmarkConfiguration[]
            {
                new BenchmarkConfiguration(),
                new BenchmarkConfiguration().WithoutTiering(),
                minOptsConfig,
                new BenchmarkConfiguration().WithMinOpts().WithoutTiering(),
                new BenchmarkConfiguration().WithoutTiering().WithMinOpts(),
                new BenchmarkConfiguration().WithNoR2R(),
                new BenchmarkConfiguration().WithNoR2R().WithoutTiering(),
                new BenchmarkConfiguration().WithoutTiering().WithNoR2R(),
                new BenchmarkConfiguration().WithNoNgen(),
                new BenchmarkConfiguration().WithNoNgen().WithoutTiering(),
                new BenchmarkConfiguration().WithoutTiering().WithNoNgen()
            };
            foreach(string configName in configNames)
            {
                BenchmarkConfiguration config = possibleConfigs.Where(c => c.Name.Equals(configName, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
                if(config == null)
                {
                    throw new ArgumentException("Unrecognized config value: " + configName);
                }
                else
                {
                    yield return config;
                }
            }
        }
    }
}
