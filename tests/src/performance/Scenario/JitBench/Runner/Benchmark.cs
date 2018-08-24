using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Xunit.Performance.Api;
using Microsoft.Xunit.Performance.Api.Profilers.Etw;

namespace JitBench
{
    public abstract class Benchmark
    {
        public Benchmark(string name)
        {
            Name = name;
            EnvironmentVariables = new Dictionary<string, string>();
        }

        public string Name { get; private set; }
        public string ExePath { get; protected set; }
        public string WorkingDirPath { get; protected set; }
        public string CommandLineArguments { get; protected set; }
        public Dictionary<string, string> EnvironmentVariables { get; private set; }

        public BenchmarkRunResult[] Run(TestRun run, ITestOutputHelper output)
        {
            using (var runSectionOutput = new IndentedTestOutputHelper($"Run {Name} iterations", output))
            {
                return MeasureIterations(run, runSectionOutput);
            }   
        }

        public abstract Task Setup(DotNetInstallation dotnetInstall, string intermediateOutputDir, bool useExistingSetup, ITestOutputHelper output);

        protected void RetargetProjects(
            DotNetInstallation dotNetInstall,
            string rootDir,
            IEnumerable<string> projectFileRelativePaths)
        {
            if (string.IsNullOrWhiteSpace(rootDir))
            {
                throw new ArgumentNullException(rootDir);
            }
            if (!Directory.Exists(rootDir))
            {
                throw new DirectoryNotFoundException($"Root directory was not found: {rootDir}");
            }

            foreach (string projectFileRelativePath in projectFileRelativePaths)
            {
                string projectFile = Path.Combine(rootDir, projectFileRelativePath);
                if (!File.Exists(projectFile))
                {
                    throw new FileNotFoundException($"Project file was not found: {projectFile}");
                }

                var doc = new XmlDocument();
                Encoding docEncoding;
                using (var fs = new FileStream(projectFile, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var sr = new StreamReader(fs))
                {
                    docEncoding = sr.CurrentEncoding;
                    doc.Load(sr);
                }
                XmlElement root = doc.DocumentElement;

                // Comment out all existing TargetFramework and RuntimeFrameworkVersion elements
                foreach (XmlElement e in root.SelectNodes("PropertyGroup/TargetFramework").OfType<XmlElement>())
                {
                    e.ParentNode.ReplaceChild(doc.CreateComment(e.OuterXml), e);
                }
                foreach (XmlElement e in root.SelectNodes("PropertyGroup/RuntimeFrameworkVersion").OfType<XmlElement>())
                {
                    e.ParentNode.ReplaceChild(doc.CreateComment(e.OuterXml), e);
                }

                // Add TargetFramework and RuntimeFrameworkVersion elements with the requested values to the top
                {
                    XmlElement propertyGroupElement = doc.CreateElement("PropertyGroup");
                    root.PrependChild(propertyGroupElement);

                    XmlElement targetFrameworkElement = doc.CreateElement("TargetFramework");
                    XmlElement runtimeFrameworkVersionElement = doc.CreateElement("RuntimeFrameworkVersion");
                    propertyGroupElement.AppendChild(targetFrameworkElement);
                    propertyGroupElement.AppendChild(runtimeFrameworkVersionElement);

                    targetFrameworkElement.InnerText =
                        DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
                    runtimeFrameworkVersionElement.InnerText = dotNetInstall.FrameworkVersion;
                }

                using (var fs = new FileStream(projectFile, FileMode.Truncate, FileAccess.Write, FileShare.Read))
                using (var sw = new StreamWriter(fs, docEncoding))
                {
                    doc.Save(sw);
                }
            }
        }

        public virtual Metric[] GetDefaultDisplayMetrics()
        {
            return new Metric[] { Metric.ElapsedTimeMilliseconds };
        }

        /// <summary>
        /// Does this benchmark run properly on a given architecture?
        /// </summary>
        public virtual bool IsArchitectureSupported(Architecture arch)
        {
            return (arch == Architecture.X86 || arch == Architecture.X64);
        }

        BenchmarkRunResult[] MeasureIterations(TestRun run, ITestOutputHelper output)
        {
            List<BenchmarkRunResult> results = new List<BenchmarkRunResult>();
            foreach (BenchmarkConfiguration config in run.Configurations)
            {
                results.Add(MeasureIterations(run, config, output));
            }
            return results.ToArray();
        }

        BenchmarkRunResult MeasureIterations(TestRun run, BenchmarkConfiguration config, ITestOutputHelper output)
        {
            // The XunitPerformanceHarness is hardcoded to log to the console. It would be nice if the output was configurable somehow
            // but in lieue of that we can redirect all console output with light hackery.
            using (var redirector = new ConsoleRedirector(output))
            {
                // XunitPerformanceHarness expects to do the raw commandline parsing itself, but I really don't like that its default collection
                // metric requires the use of ETW. Getting an admin console or admin VS instance isn't where most people start, its
                // a small nuissance, and for these tests its often not needed/adds non-trivial overhead. I set the default to stopwatch if the
                // perf:collect argument hasn't been specified, but that sadly requires that I pre-parse, interpret, and then re-format all the 
                // args to make that change :(
                // 
                // In TestRun.ValidateMetricNames() I pre-check if ETW is going to be needed and give an error there rather than doing all the
                // test setup (~1 minute?) and then giving the error after the user has probably wandered away. That also relies on some of this
                // replicated command line parsing.
                string[] args = new string[] { "--perf:collect", string.Join("+", run.MetricNames), "--perf:outputdir", run.OutputDir, "--perf:runid", run.BenchviewRunId };
                using (var harness = new XunitPerformanceHarness(args))
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(run.DotNetInstallation.DotNetExe, (ExePath + " " + CommandLineArguments).Trim());
                    startInfo.WorkingDirectory = WorkingDirPath;
                    startInfo.RedirectStandardError = true;
                    startInfo.RedirectStandardOutput = true;
                    IEnumerable<KeyValuePair<string, string>> extraEnvVars = config.EnvironmentVariables.Concat(EnvironmentVariables).Append(new KeyValuePair<string, string>("DOTNET_MULTILEVEL_LOOKUP", "0"));
                    foreach (KeyValuePair<string, string> kv in extraEnvVars)
                    {
                        startInfo.Environment[kv.Key] = kv.Value;
                    }
                    output.WriteLine("XUnitPerfHarness doesn't log env vars it uses to run processes. To workaround, logging them here:");
                    output.WriteLine($"Environment variables: {string.Join(", ", extraEnvVars.Select(kv => kv.Key + "=" + kv.Value))}");
                    output.WriteLine($"Working directory: \"{startInfo.WorkingDirectory}\"");
                    output.WriteLine($"Command line: \"{startInfo.FileName}\" {startInfo.Arguments}");

                    BenchmarkRunResult result = new BenchmarkRunResult(this, config);
                    StringBuilder stderr = new StringBuilder();
                    StringBuilder stdout = new StringBuilder();
                    var scenarioConfiguration = new ScenarioTestConfiguration(TimeSpan.FromMinutes(60), startInfo)
                    {
                        //XUnitPerformanceHarness writes files to disk starting with {runid}-{ScenarioBenchmarkName}-{TestName}
                        TestName = (Name + "-" + config.Name).Replace(' ', '_'),
                        Scenario = new ScenarioBenchmark("JitBench"),
                        Iterations = run.Iterations,
                        PreIterationDelegate = scenario =>
                        {
                            stderr.Clear();
                            stdout.Clear();
                            scenario.Process.ErrorDataReceived += (object sender, DataReceivedEventArgs errorLine) =>
                            {
                                if(!string.IsNullOrEmpty(errorLine.Data))
                                {
                                    stderr.AppendLine(errorLine.Data);
                                    redirector.WriteLine("STDERROR: " + errorLine.Data);
                                }
                            };
                            scenario.Process.OutputDataReceived += (object sender, DataReceivedEventArgs outputLine) =>
                            {
                                stdout.AppendLine(outputLine.Data);
                                redirector.WriteLine(outputLine.Data);
                            };
                        },
                        PostIterationDelegate = scenarioResult =>
                        {
                            result.IterationResults.Add(RecordIterationMetrics(scenarioResult, stdout.ToString(), stderr.ToString(), redirector));
                        }
                    };
                    harness.RunScenario(scenarioConfiguration, sb => { BenchviewResultExporter.ConvertRunResult(sb, result); });
                    return result;
                }
            }
        }

        protected virtual IterationResult RecordIterationMetrics(ScenarioExecutionResult scenarioIteration, string stdout, string stderr, ITestOutputHelper output)
        {
            IterationResult iterationResult = new IterationResult();
            int elapsedMs = (int)(scenarioIteration.ProcessExitInfo.ExitTime - scenarioIteration.ProcessExitInfo.StartTime).TotalMilliseconds;
            iterationResult.Measurements.Add(Metric.ElapsedTimeMilliseconds, elapsedMs);
            if (!string.IsNullOrWhiteSpace(scenarioIteration.EventLogFileName) && File.Exists(scenarioIteration.EventLogFileName))
            {
                AddEtwData(iterationResult, scenarioIteration, output);
            }
            return iterationResult;
        }

        protected static void AddEtwData(
            IterationResult iteration,
            ScenarioExecutionResult scenarioExecutionResult,
            ITestOutputHelper output)
        {
            string[] modulesOfInterest = new string[] {
                        "Anonymously Hosted DynamicMethods Assembly",
                        "clrjit.dll",
                        "coreclr.dll",
                        "dotnet.exe",
                        "MusicStore.dll",
                        "AllReady.dll",
                        "Word2VecScenario.dll",
                        "ntoskrnl.exe",
                        "System.Private.CoreLib.dll",
                        "Unknown",
                    };

            // Get the list of processes of interest.
            try
            {
                var processes = new SimpleTraceEventParser().GetProfileData(scenarioExecutionResult);

                // Extract the Pmc data for each one of the processes.
                foreach (var process in processes)
                {
                    if (process.Id != scenarioExecutionResult.ProcessExitInfo.ProcessId)
                        continue;

                    iteration.Measurements.Add(new Metric($"PMC/{process.Name}/Duration", "ms"),
                        process.LifeSpan.Duration.TotalMilliseconds);

                    // Add process metrics values.
                    foreach (var pmcData in process.PerformanceMonitorCounterData)
                        iteration.Measurements.Add(new Metric($"PMC/{process.Name}/{pmcData.Key.Name}", pmcData.Key.Unit), pmcData.Value);

                    foreach (var module in process.Modules)
                    {
                        var moduleName = Path.GetFileName(module.FullName);
                        if (modulesOfInterest.Any(m => m.Equals(moduleName, StringComparison.OrdinalIgnoreCase)))
                        {
                            foreach (var pmcData in module.PerformanceMonitorCounterData)
                            {
                                Metric m = new Metric($"PMC/{process.Name}!{moduleName}/{pmcData.Key.Name}", pmcData.Key.Unit);
                                // Sometimes the etw parser gives duplicate module entries which leads to duplicate keys
                                // but I haven't hunted down the reason. For now it is first one wins.
                                if (!iteration.Measurements.ContainsKey(m))
                                {
                                    iteration.Measurements.Add(m, pmcData.Value);
                                }
                            }
                                
                        }
                    }
                }
            }
            catch (InvalidOperationException e)
            {
                output.WriteLine("Error while processing ETW log: " + scenarioExecutionResult.EventLogFileName);
                output.WriteLine(e.ToString());
            }
        }

        /// <summary>
        /// When serializing the result data to benchview this is called to determine if any of the metrics should be reported differently
        /// than they were collected. We use this to collect several measurements in each iteration, then present those measurements
        /// to benchview as if each was a distinct test model with its own set of iterations of a single measurement.
        /// </summary>
        public virtual bool TryGetBenchviewCustomMetricReporting(Metric originalMetric, out Metric newMetric, out string newScenarioModelName)
        {   
            if (originalMetric.Name.StartsWith("PMC/"))
            {
                int prefixLength = "PMC/".Length;
                int secondSlash = originalMetric.Name.IndexOf('/', prefixLength);
                newScenarioModelName = originalMetric.Name.Substring(prefixLength, secondSlash - prefixLength);
                string newMetricName = originalMetric.Name.Substring(secondSlash+1);
                newMetric = new Metric(newMetricName, originalMetric.Unit);
                return true;
            }
            else
            {
                newMetric = default(Metric);
                newScenarioModelName = null;
                return false;
            }
        }
    }
}
