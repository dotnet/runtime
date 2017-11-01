// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance.Api;
using Microsoft.Xunit.Performance.Api.Profilers.Etw;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;

namespace JitBench
{
    class JitBenchHarness
    {
        static void Main(string[] args)
        {
            var options = JitBenchHarnessOptions.Parse(args);

            SetupStatics(options);

            using (var h = new XunitPerformanceHarness(args))
            {
                ProcessStartInfo startInfo = options.UseExistingSetup ? UseExistingSetup() : CreateNewSetup();

                string scenarioName = "MusicStore";
                if (!startInfo.Environment.ContainsKey("DOTNET_MULTILEVEL_LOOKUP"))
                    throw new InvalidOperationException("DOTNET_MULTILEVEL_LOOKUP was not defined.");
                if (startInfo.Environment["DOTNET_MULTILEVEL_LOOKUP"] != "0")
                    throw new InvalidOperationException("DOTNET_MULTILEVEL_LOOKUP was not set to 0.");

                if (options.EnableTiering)
                {
                    startInfo.Environment.Add("COMPlus_EXPERIMENTAL_TieredCompilation", "1");
                    scenarioName += " Tiering";
                }

                if (options.Minopts)
                {
                    startInfo.Environment.Add("COMPlus_JITMinOpts", "1");
                    scenarioName += " Minopts";
                }

                if (options.DisableR2R)
                {
                    startInfo.Environment.Add("COMPlus_ReadyToRun", "0");
                    scenarioName += " NoR2R";
                }

                if (options.DisableNgen)
                {
                    startInfo.Environment.Add("COMPlus_ZapDisable", "1");
                    scenarioName += " NoNgen";
                }

                var program = new JitBenchHarness("JitBench");
                try
                {
                    var scenarioConfiguration = new ScenarioConfiguration(TimeSpan.FromMilliseconds(60000), startInfo) {
                        Iterations = (int)options.Iterations,
                        PreIterationDelegate = program.PreIteration,
                        PostIterationDelegate = program.PostIteration,
                    };
                    var processesOfInterest = new string[] {
                        "dotnet.exe",
                    };
                    var modulesOfInterest = new string[] {
                        "Anonymously Hosted DynamicMethods Assembly",
                        "clrjit.dll",
                        "coreclr.dll",
                        "dotnet.exe",
                        "MusicStore.dll",
                        "ntoskrnl.exe",
                        "System.Private.CoreLib.dll",
                        "Unknown",
                    };

                    if (!File.Exists(startInfo.FileName))
                        throw new FileNotFoundException(startInfo.FileName);
                    if (!Directory.Exists(startInfo.WorkingDirectory))
                        throw new DirectoryNotFoundException(startInfo.WorkingDirectory);

                    h.RunScenario(scenarioConfiguration, teardownDelegate: () => {
                        return program.PostRun("MusicStore", processesOfInterest, modulesOfInterest);
                    });
                }
                catch
                {
                    Console.WriteLine(program.StandardOutput);
                    Console.WriteLine(program.StandardError);
                    throw;
                }
            }
        }

        public JitBenchHarness(string scenarioBenchmarkName)
        {
            _scenarioBenchmarkName = scenarioBenchmarkName;
            _stdout = new StringBuilder();
            _stderr = new StringBuilder();
            IterationsData = new List<IterationData>();
        }

        public string StandardOutput => _stdout.ToString();

        public string StandardError => _stderr.ToString();

        private static void SetupStatics(JitBenchHarnessOptions options)
        {
            s_temporaryDirectory = options.IntermediateOutputDirectory;
            s_targetArchitecture = options.TargetArchitecture;
            if (string.IsNullOrWhiteSpace(s_targetArchitecture))
                throw new ArgumentNullException("Unspecified target architecture.");

            // J == JitBench folder. By reducing the length of the directory
            // name we attempt to reduce the chances of hitting PATH length
            // problems we have been hitting in the lab.
            // The changes we have done have reduced it in this way:
            // C:\Jenkins\workspace\perf_scenario---5b001a46\bin\sandbox\JitBench\JitBench-dev
            // C:\j\workspace\perf_scenario---5b001a46\bin\sandbox\JitBench\JitBench-dev
            // C:\j\w\perf_scenario---5b001a46\bin\sandbox\JitBench\JitBench-dev
            // C:\j\w\perf_scenario---5b001a46\bin\sandbox\J
            s_jitBenchDevDirectory = Path.Combine(s_temporaryDirectory, "J");
            s_dotnetProcessFileName = Path.Combine(s_jitBenchDevDirectory, ".dotnet", "dotnet.exe");
            s_musicStoreDirectory = Path.Combine(s_jitBenchDevDirectory, "src", "MusicStore");
        }

        private static void DownloadAndExtractJitBenchRepo()
        {
            using (var client = new HttpClient())
            {
                var archiveName = $"{JitBenchCommitSha1Id}.zip";
                var url = $"{JitBenchRepoUrl}/archive/{archiveName}";
                var zipFile = Path.Combine(s_temporaryDirectory, archiveName);

                using (FileStream tmpzip = File.Create(zipFile))
                {
                    using (Stream stream = client.GetStreamAsync(url).Result)
                        stream.CopyTo(tmpzip);
                    tmpzip.Flush();
                }

                // If the repo already exists, we delete it and extract it again.
                if (Directory.Exists(s_jitBenchDevDirectory))
                    Directory.Delete(s_jitBenchDevDirectory, true);

                // This step will create s_JitBenchDevDirectory.
                ZipFile.ExtractToDirectory(zipFile, s_temporaryDirectory);
                Directory.Move(Path.Combine(s_temporaryDirectory, $"JitBench-{JitBenchCommitSha1Id}"), s_jitBenchDevDirectory);
            }
        }

        private static void InstallSharedRuntime()
        {
            var psi = new ProcessStartInfo {
                WorkingDirectory = s_jitBenchDevDirectory,
                FileName = @"powershell.exe",
                Arguments = $".\\Dotnet-Install.ps1 -SharedRuntime -InstallDir .dotnet -Channel master -Architecture {s_targetArchitecture}"
            };
            LaunchProcess(psi, 180000);
        }

        private static IDictionary<string, string> InstallDotnet()
        {
            var psi = new ProcessStartInfo {
                WorkingDirectory = s_jitBenchDevDirectory,
                FileName = @"powershell.exe",
                Arguments = $".\\Dotnet-Install.ps1 -InstallDir .dotnet -Channel master -Architecture {s_targetArchitecture}"
            };
            LaunchProcess(psi, 180000);

            return GetInitialEnvironment();
        }

        private static void ModifySharedFramework()
        {
            // Current working directory is the <coreclr repo root>/sandbox directory.
            Console.WriteLine($"Modifying the shared framework.");

            var sourcedi = new DirectoryInfo(Directory.GetCurrentDirectory());
            var targetdi = new DirectoryInfo(
                new DirectoryInfo(Path.Combine(s_jitBenchDevDirectory, ".dotnet", "shared", "Microsoft.NETCore.App"))
                .GetDirectories("*")
                .OrderBy(s => s.Name)
                .Last()
                .FullName);

            Console.WriteLine($"  Source : {sourcedi.FullName}");
            Console.WriteLine($"  Target : {targetdi.FullName}");

            var compiledBinariesOfInterest = new string[] {
                "clretwrc.dll",
                "clrjit.dll",
                "coreclr.dll",
                "mscordaccore.dll",
                "mscordbi.dll",
                "mscorrc.debug.dll",
                "mscorrc.dll",
                "sos.dll",
                "SOS.NETCore.dll",
                "System.Private.CoreLib.dll"
            };

            foreach (var compiledBinaryOfInterest in compiledBinariesOfInterest)
            {
                foreach (FileInfo fi in targetdi.GetFiles(compiledBinaryOfInterest))
                {
                    var sourceFilePath = Path.Combine(sourcedi.FullName, fi.Name);
                    var targetFilePath = Path.Combine(targetdi.FullName, fi.Name);

                    if (File.Exists(sourceFilePath))
                    {
                        File.Copy(sourceFilePath, targetFilePath, true);
                        Console.WriteLine($"    Copied file - '{targetFilePath}'");
                    }
                }
            }
        }

        private static IDictionary<string, string> GenerateStore(IDictionary<string, string> environment)
        {
            // This step generates some environment variables needed later.
            var psi = new ProcessStartInfo {
                WorkingDirectory = s_jitBenchDevDirectory,
                FileName = "powershell.exe",
                Arguments = $"-Command \".\\AspNet-GenerateStore.ps1 -InstallDir .store -Architecture {s_targetArchitecture} -Runtime win7-{s_targetArchitecture}; gi env:JITBENCH_*, env:DOTNET_SHARED_STORE | %{{ \\\"$($_.Name)=$($_.Value)\\\" }} 1>>{EnvironmentFileName}\""
            };

            LaunchProcess(psi, 1800000, environment);

            // Return the generated environment variables.
            return GetEnvironment(environment, Path.Combine(s_jitBenchDevDirectory, EnvironmentFileName));
        }

        private static IDictionary<string, string> GetEnvironment(IDictionary<string, string> environment, string fileName)
        {
            foreach (var line in File.ReadLines(fileName))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string[] pair = line.Split(new char[] { '=' }, 2);
                if (pair.Length != 2)
                    throw new InvalidOperationException($"AspNet-GenerateStore.ps1 did not generate the expected environment variable {pair}");

                if (!environment.ContainsKey(pair[0]))
                    environment.Add(pair[0], pair[1]);
                else
                    environment[pair[0]] = pair[1];
            }

            return environment;
        }

        private static void DotNetInfo(string workingDirectory, string dotnetFileName, IDictionary<string, string> environment)
        {
            var psi = new ProcessStartInfo {
                WorkingDirectory = workingDirectory,
                FileName = dotnetFileName,
                Arguments = "--info"
            };

            LaunchProcess(psi, 60000, environment);
        }

        private static void RestoreMusicStore(string workingDirectory, string dotnetFileName, IDictionary<string, string> environment)
        {
            var psi = new ProcessStartInfo {
                WorkingDirectory = workingDirectory,
                FileName = dotnetFileName,
                Arguments = "restore"
            };

            LaunchProcess(psi, 300000, environment);
        }

        private static void PublishMusicStore(string workingDirectory, string dotnetFileName, IDictionary<string, string> environment)
        {
            var manifest = environment["JITBENCH_ASPNET_MANIFEST"];
            if (!File.Exists(manifest))
                throw new FileNotFoundException(manifest);

            var psi = new ProcessStartInfo {
                WorkingDirectory = workingDirectory,
                FileName = dotnetFileName,
                Arguments = $"publish -c Release -f {JitBenchTargetFramework} --manifest \"{manifest}\" /p:MvcRazorCompileOnPublish=false -o \"{MusicStorePublishDirectory}\""
            };

            LaunchProcess(psi, 300000, environment);
        }

        // Return an environment with the downloaded dotnet on the path.
        private static IDictionary<string, string> GetInitialEnvironment()
        {
            // TODO: This is currently hardcoded, but we could probably pull it from the powershell cmdlet call.
            var dotnetPath = Path.Combine(s_jitBenchDevDirectory, ".dotnet");
            var dotnetexe = Path.Combine(dotnetPath, "dotnet.exe");
            if (!File.Exists(dotnetexe))
                throw new FileNotFoundException(dotnetexe);

            var environment = new Dictionary<string, string> {
                { "DOTNET_MULTILEVEL_LOOKUP", "0" },
                { "PATH", $"{dotnetPath};{Environment.GetEnvironmentVariable("PATH")}" }
            };

            return environment;
        }

        private static string MusicStorePublishDirectory =>
            Path.Combine(s_musicStoreDirectory, "bin", s_targetArchitecture, "Release", JitBenchTargetFramework, "publish");

        private static ProcessStartInfo CreateJitBenchStartInfo(IDictionary<string, string> environment)
        {
            var psi = new ProcessStartInfo {
                Arguments = "MusicStore.dll",
                FileName = s_dotnetProcessFileName,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                WorkingDirectory = MusicStorePublishDirectory,
            };

            foreach (KeyValuePair<string, string> pair in environment)
                psi.Environment.Add(pair.Key, pair.Value);

            return psi;
        }

        private static ProcessStartInfo UseExistingSetup()
        {
            PrintHeader("Using existing SETUP");

            IDictionary<string, string> environment = GetInitialEnvironment();
            environment = GetEnvironment(environment, Path.Combine(s_jitBenchDevDirectory, EnvironmentFileName));
            ValidateEnvironment(environment);

            return CreateJitBenchStartInfo(environment);
        }

        private static ProcessStartInfo CreateNewSetup()
        {
            PrintHeader("Starting SETUP");

            DownloadAndExtractJitBenchRepo();
            InstallSharedRuntime();
            IDictionary<string, string> environment = InstallDotnet();

            if (new string[] { "PATH" }.Except(environment.Keys, StringComparer.OrdinalIgnoreCase).Any())
                throw new Exception("Missing expected environment variable PATH.");

            environment = GenerateStore(environment);

            ValidateEnvironment(environment);

            ModifySharedFramework();

            DotNetInfo(s_musicStoreDirectory, s_dotnetProcessFileName, environment);
            RestoreMusicStore(s_musicStoreDirectory, s_dotnetProcessFileName, environment);
            PublishMusicStore(s_musicStoreDirectory, s_dotnetProcessFileName, environment);

            return CreateJitBenchStartInfo(environment);
        }

        private static void ValidateEnvironment(IDictionary<string, string> environment)
        {
            var expectedVariables = new string[] {
                "DOTNET_MULTILEVEL_LOOKUP",
                "PATH",
                "JITBENCH_ASPNET_MANIFEST",
                "JITBENCH_FRAMEWORK_VERSION",
                "JITBENCH_ASPNET_VERSION",
                "DOTNET_SHARED_STORE",
            };
            if (expectedVariables.Except(environment.Keys, StringComparer.OrdinalIgnoreCase).Any())
                throw new Exception("Missing expected environment variables.");

            Console.WriteLine("**********************************************************************");
            foreach (var env in expectedVariables)
                Console.WriteLine($"  {env}={environment[env]}");
            Console.WriteLine("**********************************************************************");
        }

        private const string JitBenchRepoUrl = "https://github.com/aspnet/JitBench";
        private const string JitBenchCommitSha1Id = "b7e7b786c60daa255aacaea85006afe4d4ec8306";
        private const string JitBenchTargetFramework = "netcoreapp2.1";
        private const string EnvironmentFileName = "JitBenchEnvironment.txt";

        private void PreIteration(Scenario scenario)
        {
            PrintHeader("Setting up data standard output/error process handlers.");

            _stderr.Clear();
            _stdout.Clear();

            if (scenario.Process.StartInfo.RedirectStandardError)
            {
                scenario.Process.ErrorDataReceived += (object sender, DataReceivedEventArgs errorLine) => {
                    if (!string.IsNullOrEmpty(errorLine.Data))
                        _stderr.AppendLine(errorLine.Data);
                };
            }

            if (scenario.Process.StartInfo.RedirectStandardInput)
                throw new NotImplementedException("RedirectStandardInput has not been implemented yet.");

            if (scenario.Process.StartInfo.RedirectStandardOutput)
            {
                scenario.Process.OutputDataReceived += (object sender, DataReceivedEventArgs outputLine) => {
                    if (!string.IsNullOrEmpty(outputLine.Data))
                        _stdout.AppendLine(outputLine.Data);
                };
            }
        }

        private void PostIteration(ScenarioExecutionResult scenarioExecutionResult)
        {
            PrintHeader("Processing iteration results.");

            double? startupTime = null;
            double? firstRequestTime = null;
            double? steadyStateAverageTime = null;

            using (var reader = new StringReader(_stdout.ToString()))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match match = Regex.Match(line, @"^Server started in (\d+)ms$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        startupTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }

                    match = Regex.Match(line, @"^Request took (\d+)ms$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        firstRequestTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }

                    match = Regex.Match(line, @"^Steadystate average response time: (\d+)ms$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        steadyStateAverageTime = Convert.ToDouble(match.Groups[1].Value);
                        break;
                    }
                }
            }

            if (!startupTime.HasValue)
                throw new Exception("Startup time was not found.");
            if (!firstRequestTime.HasValue)
                throw new Exception("First Request time was not found.");
            if (!steadyStateAverageTime.HasValue)
                throw new Exception("Steady state average response time not found.");

            IterationsData.Add(new IterationData {
                ScenarioExecutionResult = scenarioExecutionResult,
                StandardOutput = _stdout.ToString(),
                StartupTime = startupTime.Value,
                FirstRequestTime = firstRequestTime.Value,
                SteadystateTime = steadyStateAverageTime.Value,
            });

            PrintRunningStepInformation($"({IterationsData.Count}) Server started in {IterationsData.Last().StartupTime}ms");
            PrintRunningStepInformation($"({IterationsData.Count}) Request took {IterationsData.Last().FirstRequestTime}ms");
            PrintRunningStepInformation($"({IterationsData.Count}) Cold start time (server start + first request time): {IterationsData.Last().StartupTime + IterationsData.Last().FirstRequestTime}ms");
            PrintRunningStepInformation($"({IterationsData.Count}) Average steady state response {IterationsData.Last().SteadystateTime}ms");

            _stdout.Clear();
            _stderr.Clear();
        }

        private ScenarioBenchmark PostRun(
            string scenarioTestModelName,
            IReadOnlyCollection<string> processesOfInterest,
            IReadOnlyCollection<string> modulesOfInterest)
        {
            PrintHeader("Post-Processing scenario data.");

            var scenarioBenchmark = new ScenarioBenchmark(_scenarioBenchmarkName);

            foreach (var iter in IterationsData)
            {
                var scenarioExecutionResult = iter.ScenarioExecutionResult;
                var scenarioTestModel = scenarioBenchmark.Tests
                    .SingleOrDefault(t => t.Name == scenarioTestModelName);

                if (scenarioTestModel == null)
                {
                    scenarioTestModel = new ScenarioTestModel(scenarioTestModelName);
                    scenarioBenchmark.Tests.Add(scenarioTestModel);

                    // Add measured metrics to each test.
                    scenarioTestModel.Performance.Metrics.Add(ElapsedTimeMilliseconds);
                }

                scenarioTestModel.Performance.IterationModels.Add(new IterationModel {
                    Iteration = new Dictionary<string, double> {
                        { ElapsedTimeMilliseconds.Name, (scenarioExecutionResult.ProcessExitInfo.ExitTime - scenarioExecutionResult.ProcessExitInfo.StartTime).TotalMilliseconds},
                    }
                });

                // Create (measured) test entries for this scenario.
                var startup = scenarioBenchmark.Tests
                    .SingleOrDefault(t => t.Name == "Startup" && t.Namespace == scenarioTestModel.Name);
                if (startup == null)
                {
                    startup = new ScenarioTestModel("Startup") {
                        Namespace = scenarioTestModel.Name,
                    };
                    scenarioBenchmark.Tests.Add(startup);

                    // Add measured metrics to each test.
                    startup.Performance.Metrics.Add(ElapsedTimeMilliseconds);
                }

                var firstRequest = scenarioBenchmark.Tests
                    .SingleOrDefault(t => t.Name == "First Request" && t.Namespace == scenarioTestModel.Name);
                if (firstRequest == null)
                {
                    firstRequest = new ScenarioTestModel("First Request") {
                        Namespace = scenarioTestModel.Name,
                    };
                    scenarioBenchmark.Tests.Add(firstRequest);

                    // Add measured metrics to each test.
                    firstRequest.Performance.Metrics.Add(ElapsedTimeMilliseconds);
                }

                startup.Performance.IterationModels.Add(new IterationModel {
                    Iteration = new Dictionary<string, double> {
                            { ElapsedTimeMilliseconds.Name, iter.StartupTime },
                        },
                });

                firstRequest.Performance.IterationModels.Add(new IterationModel {
                    Iteration = new Dictionary<string, double> {
                            { ElapsedTimeMilliseconds.Name, iter.FirstRequestTime },
                        },
                });

                if (!string.IsNullOrWhiteSpace(iter.ScenarioExecutionResult.EventLogFileName) &&
                    File.Exists(iter.ScenarioExecutionResult.EventLogFileName))
                {
                    // Adding ETW data.
                    scenarioBenchmark = AddEtwData(
                        scenarioBenchmark, iter.ScenarioExecutionResult, processesOfInterest, modulesOfInterest);
                }
            }

            for (int i = scenarioBenchmark.Tests.Count - 1; i >= 0; i--)
                if (scenarioBenchmark.Tests[i].Performance.IterationModels.All(iter => iter.Iteration.Count == 0))
                    scenarioBenchmark.Tests.RemoveAt(i);

            return scenarioBenchmark;
        }

        private static ScenarioBenchmark AddEtwData(
            ScenarioBenchmark scenarioBenchmark,
            ScenarioExecutionResult scenarioExecutionResult,
            IReadOnlyCollection<string> processesOfInterest,
            IReadOnlyCollection<string> modulesOfInterest)
        {
            var metricModels = scenarioExecutionResult.PerformanceMonitorCounters
                .Select(pmc => new MetricModel {
                    DisplayName = pmc.DisplayName,
                    Name = pmc.Name,
                    Unit = pmc.Unit,
                });

            // Get the list of processes of interest.
            Console.WriteLine($"Parsing: {scenarioExecutionResult.EventLogFileName}");
            var processes = new SimpleTraceEventParser().GetProfileData(scenarioExecutionResult);

            // Extract the Pmc data for each one of the processes.
            foreach (var process in processes)
            {
                if (!processesOfInterest.Any(p => p.Equals(process.Name, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var processTest = scenarioBenchmark.Tests
                    .SingleOrDefault(t => t.Name == process.Name && t.Namespace == "");
                if (processTest == null)
                {
                    processTest = new ScenarioTestModel(process.Name) {
                        Namespace = "",
                    };
                    scenarioBenchmark.Tests.Add(processTest);

                    // Add metrics definitions.
                    processTest.Performance.Metrics.Add(ElapsedTimeMilliseconds);
                    processTest.Performance.Metrics.AddRange(metricModels);
                }

                var processIterationModel = new IterationModel {
                    Iteration = new Dictionary<string, double>()
                };
                processTest.Performance.IterationModels.Add(processIterationModel);

                processIterationModel.Iteration.Add(
                    ElapsedTimeMilliseconds.Name, process.LifeSpan.Duration.TotalMilliseconds);

                // Add process metrics values.
                foreach (var pmcData in process.PerformanceMonitorCounterData)
                    processIterationModel.Iteration.Add(pmcData.Key.Name, pmcData.Value);

                foreach (var module in process.Modules)
                {
                    var moduleName = Path.GetFileName(module.FullName);
                    if (modulesOfInterest.Any(m => m.Equals(moduleName, StringComparison.OrdinalIgnoreCase)))
                    {
                        var moduleTestName = $"{moduleName}";
                        var moduleTest = scenarioBenchmark.Tests
                            .SingleOrDefault(t => t.Name == moduleTestName && t.Namespace == process.Name);

                        if (moduleTest == null)
                        {
                            moduleTest = new ScenarioTestModel(moduleTestName) {
                                Namespace = process.Name,
                                Separator = "!",
                            };
                            scenarioBenchmark.Tests.Add(moduleTest);

                            // Add metrics definitions.
                            moduleTest.Performance.Metrics.AddRange(metricModels);
                        }

                        var moduleIterationModel = new IterationModel {
                            Iteration = new Dictionary<string, double>()
                        };
                        moduleTest.Performance.IterationModels.Add(moduleIterationModel);

                        // 5. Add module metrics values.
                        foreach (var pmcData in module.PerformanceMonitorCounterData)
                            moduleIterationModel.Iteration.Add(pmcData.Key.Name, pmcData.Value);
                    }
                }
            }

            return scenarioBenchmark;
        }

        private static void LaunchProcess(ProcessStartInfo processStartInfo, int timeoutMilliseconds, IDictionary<string, string> environment = null)
        {
            Console.WriteLine();
            Console.WriteLine($"{System.Security.Principal.WindowsIdentity.GetCurrent().Name}@{Environment.MachineName} \"{processStartInfo.WorkingDirectory}\"");
            Console.WriteLine($"[{DateTime.Now}] $ {processStartInfo.FileName} {processStartInfo.Arguments}");

            if (environment != null)
            {
                foreach (KeyValuePair<string, string> pair in environment)
                {
                    if (!processStartInfo.Environment.ContainsKey(pair.Key))
                        processStartInfo.Environment.Add(pair.Key, pair.Value);
                    else
                        processStartInfo.Environment[pair.Key] = pair.Value;
                }
            }

            using (var p = new System.Diagnostics.Process { StartInfo = processStartInfo })
            {
                p.Start();
                if (p.WaitForExit(timeoutMilliseconds) == false)
                {
                    // FIXME: What about clean/kill child processes?
                    p.Kill();
                    throw new TimeoutException($"The process '{processStartInfo.FileName} {processStartInfo.Arguments}' timed out.");
                }

                if (p.ExitCode != 0)
                    throw new Exception($"{processStartInfo.FileName} exited with error code {p.ExitCode}");
            }
        }

        private static void PrintHeader(string message)
        {
            Console.WriteLine();
            Console.WriteLine("**********************************************************************");
            Console.WriteLine($"** [{DateTime.Now}] {message}");
            Console.WriteLine("**********************************************************************");
        }

        private static void PrintRunningStepInformation(string message)
        {
            Console.WriteLine($"-- {message}");
        }

        private List<IterationData> IterationsData { get; }

        private static MetricModel ElapsedTimeMilliseconds { get; } = new MetricModel {
            DisplayName = "Duration",
            Name = "Duration",
            Unit = "ms",
        };

#if DEBUG
        private const int NumberOfIterations = 2;
#else
        private const int NumberOfIterations = 11;
#endif
        private readonly string _scenarioBenchmarkName;
        private readonly StringBuilder _stdout;
        private readonly StringBuilder _stderr;

        private static string s_temporaryDirectory;
        private static string s_jitBenchDevDirectory;
        private static string s_dotnetProcessFileName;
        private static string s_musicStoreDirectory;
        private static string s_targetArchitecture;
    }
}
