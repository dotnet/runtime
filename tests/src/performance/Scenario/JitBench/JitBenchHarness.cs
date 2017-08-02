// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using CommandLine.Text;
using Microsoft.Xunit.Performance.Api;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.RegularExpressions;

namespace JitBench
{
    class Program
    {
        static void Main(string[] args)
        {
            var options = JitBenchHarnessOptions.Parse(args);

            s_temporaryDirectory = Path.Combine(options.IntermediateOutputDirectory, "JitBench");
            s_targetArchitecture = options.TargetArchitecture;
            if (string.IsNullOrWhiteSpace(s_targetArchitecture))
                throw new ArgumentNullException("Unspecified target architecture.");

            if (Directory.Exists(s_temporaryDirectory))
                Directory.Delete(s_temporaryDirectory, true);
            Directory.CreateDirectory(s_temporaryDirectory);

            s_jitBenchDevDirectory = Path.Combine(s_temporaryDirectory, "JitBench-dev");

            using (var h = new XunitPerformanceHarness(args))
            {
                ProcessStartInfo startInfo = Setup();
                h.RunScenario(startInfo, () => { PrintHeader("Running Benchmark Scenario"); }, PostIteration, PostProcessing, s_ScenarioConfiguration);
            }
        }

        static Program()
        {
            s_ScenarioConfiguration = new ScenarioConfiguration(TimeSpan.FromMilliseconds(20000)) {
                Iterations = 11
            };

            // Set variables we will need to store results.
            s_iteration = 0;
            s_startupTimes = new double[s_ScenarioConfiguration.Iterations];
            s_requestTimes = new double[s_ScenarioConfiguration.Iterations];
            s_targetArchitecture = "";
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
            var psi = new ProcessStartInfo() {
                WorkingDirectory = s_jitBenchDevDirectory,
                FileName = @"powershell.exe",
                Arguments = $".\\Dotnet-Install.ps1 -SharedRuntime -InstallDir .dotnet -Channel master -Architecture {s_targetArchitecture}"
            };
            LaunchProcess(psi, 180000);
        }

        private static IDictionary<string, string> InstallDotnet()
        {
            var psi = new ProcessStartInfo() {
                WorkingDirectory = s_jitBenchDevDirectory,
                FileName = @"powershell.exe",
                Arguments = $".\\Dotnet-Install.ps1 -InstallDir .dotnet -Channel master -Architecture {s_targetArchitecture}"
            };
            LaunchProcess(psi, 180000);

            // TODO: This is currently hardcoded, but we could probably pull it from the powershell cmdlet call.
            return new Dictionary<string, string> { { "PATH", $"{Path.Combine(s_jitBenchDevDirectory, ".dotnet")};{psi.Environment["PATH"]}" } };
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
            var environmentFileName = "JitBenchEnvironment.txt";
            var psi = new ProcessStartInfo() {
                WorkingDirectory = s_jitBenchDevDirectory,
                FileName = "powershell.exe",
                Arguments = $"-Command \".\\AspNet-GenerateStore.ps1 -InstallDir .store -Architecture {s_targetArchitecture} -Runtime win7-{s_targetArchitecture}; gi env:JITBENCH_*, env:DOTNET_SHARED_STORE | %{{ \\\"$($_.Name)=$($_.Value)\\\" }} 1>>{environmentFileName}\""
            };

            LaunchProcess(psi, 1800000, environment);

            // Return the generated environment variables.
            return GetEnvironment(environment, Path.Combine(s_jitBenchDevDirectory, environmentFileName));
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

        private static void RestoreMusicStore(string workingDirectory, string dotnetFileName, IDictionary<string, string> environment)
        {
            var psi = new ProcessStartInfo() {
                WorkingDirectory = workingDirectory,
                FileName = dotnetFileName,
                Arguments = "restore"
            };

            LaunchProcess(psi, 300000, environment);
        }

        private static void PublishMusicStore(string workingDirectory, string dotnetFileName, IDictionary<string, string> environment)
        {
            var psi = new ProcessStartInfo() {
                WorkingDirectory = workingDirectory,
                FileName = "cmd.exe",
                Arguments = $"/C \"{dotnetFileName} publish -c Release -f {JitBenchTargetFramework} --manifest %JITBENCH_ASPNET_MANIFEST% /p:MvcRazorCompileOnPublish=false\""
            };

            LaunchProcess(psi, 300000, environment);
        }

        private static ProcessStartInfo Setup()
        {
            PrintHeader("Starting SETUP");

            DownloadAndExtractJitBenchRepo();
            InstallSharedRuntime();
            IDictionary<string, string> environment = InstallDotnet();

            if (new string[] { "PATH" }.Except(environment.Keys, StringComparer.OrdinalIgnoreCase).Any())
                throw new Exception("Missing expected environment variable PATH.");

            environment = GenerateStore(environment);

            var expectedVariables = new string[] {
                "PATH",
                "JITBENCH_ASPNET_MANIFEST",
                "JITBENCH_FRAMEWORK_VERSION",
                "JITBENCH_ASPNET_VERSION",
                "DOTNET_SHARED_STORE"
            };
            if (expectedVariables.Except(environment.Keys, StringComparer.OrdinalIgnoreCase).Any())
                throw new Exception("Missing expected environment variables.");

            ModifySharedFramework();

            var dotnetProcessFileName = Path.Combine(s_jitBenchDevDirectory, ".dotnet", "dotnet.exe");
            var musicStoreDirectory = Path.Combine(s_jitBenchDevDirectory, "src", "MusicStore");

            RestoreMusicStore(musicStoreDirectory, dotnetProcessFileName, environment);
            PublishMusicStore(musicStoreDirectory, dotnetProcessFileName, environment);

            var psi = new ProcessStartInfo() {
                FileName = "cmd.exe",
                Arguments = $"/C \"{dotnetProcessFileName} MusicStore.dll 1>{MusicStoreRedirectedStandardOutputFileName}\"",
                WorkingDirectory = Path.Combine(musicStoreDirectory, "bin", "Release", "netcoreapp2.0", "publish")
            };

            foreach (KeyValuePair<string, string> pair in environment)
                psi.Environment.Add(pair.Key, pair.Value);

            return psi;
        }

        private const string MusicStoreRedirectedStandardOutputFileName = "measures.txt";
        private const string JitBenchRepoUrl = "https://github.com/aspnet/JitBench";
        private const string JitBenchCommitSha1Id = "a44cd96e4ff8d7c1b43fe3c96a597919bb1544bd";
        private const string JitBenchTargetFramework = "netcoreapp2.0";

        private static void PostIteration()
        {
            var path = Path.Combine(s_jitBenchDevDirectory, "src", "MusicStore", "bin", "Release", "netcoreapp2.0", "publish");
            path = Path.Combine(path, MusicStoreRedirectedStandardOutputFileName);

            double? startupTime = null;
            double? requestTime = null;
            foreach (string line in File.ReadLines(path))
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
                    requestTime = Convert.ToDouble(match.Groups[1].Value);
                    break;
                }
            }

            if (!startupTime.HasValue)
                throw new Exception("Startup time was not found.");
            if (!requestTime.HasValue)
                throw new Exception("Request time was not found.");

            s_startupTimes[s_iteration] = startupTime.Value;
            s_requestTimes[s_iteration] = requestTime.Value;

            PrintRunningStepInformation($"{s_iteration} Server started in {s_startupTimes[s_iteration]}ms");
            PrintRunningStepInformation($"{s_iteration} Request took {s_requestTimes[s_iteration]}ms");
            PrintRunningStepInformation($"{s_iteration} Cold start time (server start + first request time): {s_startupTimes[s_iteration] + s_requestTimes[s_iteration]}ms");

            ++s_iteration;
        }

        private static ScenarioBenchmark PostProcessing()
        {
            PrintHeader("Starting POST");

            var scenarioBenchmark = new ScenarioBenchmark("MusicStore") {
                Namespace = "JitBench"
            };

            // Create (measured) test entries for this scenario.
            var startup = new ScenarioTestModel("Startup time");
            scenarioBenchmark.Tests.Add(startup);

            var request = new ScenarioTestModel("Request time");
            scenarioBenchmark.Tests.Add(request);

            // Add measured metrics to each test.
            startup.Performance.Metrics.Add(new MetricModel {
                Name = "Duration",
                DisplayName = "Duration",
                Unit = "ms"
            });
            request.Performance.Metrics.Add(new MetricModel {
                Name = "Duration",
                DisplayName = "Duration",
                Unit = "ms"
            });

            for (int i = 0; i < s_ScenarioConfiguration.Iterations; ++i)
            {
                var startupIteration = new IterationModel { Iteration = new Dictionary<string, double>() };
                startupIteration.Iteration.Add("Duration", s_startupTimes[i]);
                startup.Performance.IterationModels.Add(startupIteration);

                var requestIteration = new IterationModel { Iteration = new Dictionary<string, double>() };
                requestIteration.Iteration.Add("Duration", s_requestTimes[i]);
                request.Performance.IterationModels.Add(requestIteration);
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

            using (var p = new Process() { StartInfo = processStartInfo })
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
            Console.WriteLine($"** {message}");
            Console.WriteLine("**********************************************************************");
        }

        private static void PrintRunningStepInformation(string message)
        {
            Console.WriteLine($"-- {message}");
        }

        private static readonly ScenarioConfiguration s_ScenarioConfiguration;

        private static int s_iteration;
        private static double[] s_startupTimes;
        private static double[] s_requestTimes;
        private static string s_temporaryDirectory;
        private static string s_jitBenchDevDirectory;
        private static string s_targetArchitecture;

        /// <summary>
        /// Provides an interface to parse the command line arguments passed to the JitBench harness.
        /// </summary>
        private sealed class JitBenchHarnessOptions
        {
            public JitBenchHarnessOptions()
            {
                _tempDirectory = Directory.GetCurrentDirectory();
            }

            [Option('o', Required = false, HelpText = "Specifies the intermediate output directory name.")]
            public string IntermediateOutputDirectory
            {
                get { return _tempDirectory; }

                set
                {
                    if (string.IsNullOrWhiteSpace(value))
                        throw new InvalidOperationException("The intermediate output directory name cannot be null, empty or white space.");

                    if (value.Any(c => Path.GetInvalidPathChars().Contains(c)))
                        throw new InvalidOperationException("Specified intermediate output directory name contains invalid path characters.");

                    _tempDirectory = Path.IsPathRooted(value) ? value : Path.GetFullPath(value);
                    Directory.CreateDirectory(_tempDirectory);
                }
            }

            [Option("target-architecture", Required = true, HelpText = "JitBench target architecture (It must match the built product that was copied into sandbox).")]
            public string TargetArchitecture { get; set; }

            public static JitBenchHarnessOptions Parse(string[] args)
            {
                using (var parser = new Parser((settings) => {
                    settings.CaseInsensitiveEnumValues = true;
                    settings.CaseSensitive = false;
                    settings.HelpWriter = new StringWriter();
                    settings.IgnoreUnknownArguments = true;
                }))
                {
                    JitBenchHarnessOptions options = null;
                    parser.ParseArguments<JitBenchHarnessOptions>(args)
                        .WithParsed(parsed => options = parsed)
                        .WithNotParsed(errors => {
                            foreach (Error error in errors)
                            {
                                switch (error.Tag)
                                {
                                    case ErrorType.MissingValueOptionError:
                                        throw new ArgumentException(
                                                $"Missing value option for command line argument '{(error as MissingValueOptionError).NameInfo.NameText}'");
                                    case ErrorType.HelpRequestedError:
                                        Console.WriteLine(Usage());
                                        Environment.Exit(0);
                                        break;
                                    case ErrorType.VersionRequestedError:
                                        Console.WriteLine(new AssemblyName(typeof(JitBenchHarnessOptions).GetTypeInfo().Assembly.FullName).Version);
                                        Environment.Exit(0);
                                        break;
                                    case ErrorType.BadFormatTokenError:
                                    case ErrorType.UnknownOptionError:
                                    case ErrorType.MissingRequiredOptionError:
                                    case ErrorType.MutuallyExclusiveSetError:
                                    case ErrorType.BadFormatConversionError:
                                    case ErrorType.SequenceOutOfRangeError:
                                    case ErrorType.RepeatedOptionError:
                                    case ErrorType.NoVerbSelectedError:
                                    case ErrorType.BadVerbSelectedError:
                                    case ErrorType.HelpVerbRequestedError:
                                        break;
                                }
                            }
                        });
                    return options;
                }
            }

            public static string Usage()
            {
                var parser = new Parser((parserSettings) => {
                    parserSettings.CaseInsensitiveEnumValues = true;
                    parserSettings.CaseSensitive = false;
                    parserSettings.EnableDashDash = true;
                    parserSettings.HelpWriter = new StringWriter();
                    parserSettings.IgnoreUnknownArguments = true;
                });

                var helpTextString = new HelpText {
                    AddDashesToOption = true,
                    AddEnumValuesToHelpText = true,
                    AdditionalNewLineAfterOption = false,
                    Heading = "JitBenchHarness",
                    MaximumDisplayWidth = 80,
                }.AddOptions(parser.ParseArguments<JitBenchHarnessOptions>(new string[] { "--help" })).ToString();
                return helpTextString;
            }

            private string _tempDirectory;
        }
    }
}
