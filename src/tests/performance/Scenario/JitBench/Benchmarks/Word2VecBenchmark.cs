using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Xunit.Performance.Api;
using System.Runtime.InteropServices;
using System.Globalization;

namespace JitBench
{
    class Word2VecBenchmark : Benchmark
    {
        private static readonly HashSet<int> DefaultExitCodes = new HashSet<int>(new[] { 0 });

        public Word2VecBenchmark() : base("Word2Vec")
        {
            ExePath = "Word2VecScenario.dll";
        }

        public override bool IsArchitectureSupported(Architecture arch)
        {
            //Word2Vec uses large amounts of virtual address space which it may or may not exhaust 
            //when running on x86. In the case I investigated there was still a few 100MB free,
            //but it was sufficiently fragmented that the largest block was less than 32MB which
            //is what the GC wants for a new LOH segment. The GC behavior here is by-design.
            //Tiered jitting increases the memory usage (more code) and may cause different
            //fragmentation patterns - arguably either 'by design' or 'known issue that won't change
            //unless customers tell us its a problem'. I'm OK telling people not to use tiered jitting 
            //if their app already uses most of the address space on x86, and having an intermitently 
            //failing test in a perf suite won't give us useful info hence x64 only for this one.

            return arch == Architecture.X64;
        }

        public override async Task Setup(DotNetInstallation dotNetInstall, string outputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            if(!useExistingSetup)
            {
                using (var setupSection = new IndentedTestOutputHelper("Setup " + Name, output))
                {
                    await CloneWord2VecNetRepo(outputDir, setupSection);
                    RetargetProjects(
                        dotNetInstall,
                        GetWord2VecNetRepoRootDir(outputDir),
                        new string[]
                        {
                            Path.Combine("Word2Vec.Net", "Word2Vec.Net.csproj"),
                            Path.Combine("Word2VecScenario", "Word2VecScenario.csproj")
                        });
                    await Publish(dotNetInstall, outputDir, setupSection);
                    await DownloadAndExtractTextCorpus(dotNetInstall, outputDir, setupSection);
                }
            }
            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            WorkingDirPath = GetWord2VecNetPublishDirectory(dotNetInstall, outputDir, tfm);
        }

        async Task CloneWord2VecNetRepo(string outputDir, ITestOutputHelper output)
        {
            // If the repo already exists, we delete it and extract it again.
            string word2VecNetRepoRootDir = GetWord2VecNetRepoRootDir(outputDir);
            FileTasks.DeleteDirectory(word2VecNetRepoRootDir, output);

            await ExecuteGitCommand($"clone {Word2VecNetRepoUrl} {word2VecNetRepoRootDir}", output);
            await ExecuteGitCommand($"checkout {Word2VecNetCommitSha1Id}", output, workingDirectory: word2VecNetRepoRootDir);
        }

        async Task ExecuteGitCommand(string arguments, ITestOutputHelper output, string workingDirectory = null)
        {
            int exitCode = await new ProcessRunner("git", arguments).WithLog(output).WithWorkingDirectory(workingDirectory).Run();

            if (!DefaultExitCodes.Contains(exitCode))
                throw new Exception($"git {arguments} has failed, the exit code was {exitCode}");
        }

        async Task DownloadAndExtractTextCorpus(DotNetInstallation dotNetInstall, string outputDir, ITestOutputHelper output)
        {
            // If the file already exists, exit
            string word2VecNetRepoRootDir = GetWord2VecNetRepoRootDir(outputDir);
            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            string word2VecNetPublishDir = GetWord2VecNetPublishDirectory(dotNetInstall, outputDir, tfm);

            // Download the corpus of text. This is a zip file that contains a text file of 100M of text from Wikipedia
            var url = "https://perfbenchmarkstorage.blob.core.windows.net/corpus/Corpus10.zip";
            await FileTasks.DownloadAndUnzip(url, word2VecNetRepoRootDir + "_temp", output);

            FileTasks.MoveFile(Path.Combine(word2VecNetRepoRootDir + "_temp", "Corpus.txt"), 
                    Path.Combine(word2VecNetPublishDir, "Corpus.txt"), output);
        }

        private async Task<string> Publish(DotNetInstallation dotNetInstall, string outputDir, ITestOutputHelper output)
        {
            string tfm = DotNetSetup.GetTargetFrameworkMonikerForFrameworkVersion(dotNetInstall.FrameworkVersion);
            string publishDir = GetWord2VecNetPublishDirectory(dotNetInstall, outputDir, tfm);
            if (publishDir != null)
            {
                FileTasks.DeleteDirectory(publishDir, output);
            }
            string dotNetExePath = dotNetInstall.DotNetExe;
            await new ProcessRunner(dotNetExePath, $"publish -c Release -f {tfm}")
                .WithWorkingDirectory(GetWord2VecNetSrcDirectory(outputDir))
                .WithEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0")
                .WithEnvironmentVariable("WORD2VEC_FRAMEWORK_VERSION", dotNetInstall.FrameworkVersion)
                .WithEnvironmentVariable("UseSharedCompilation", "false")
                .WithLog(output)
                .Run();

            publishDir = GetWord2VecNetPublishDirectory(dotNetInstall, outputDir, tfm);
            if (publishDir == null)
            {
                throw new DirectoryNotFoundException($"Could not find 'publish' directory: {publishDir}");
            }
            return publishDir;
        }

        public override Metric[] GetDefaultDisplayMetrics()
        {
            return new Metric[]
            {
                TrainingMetric,
                FirstSearchMetric,
                MedianSearchMetric
            };
        }

        protected override IterationResult RecordIterationMetrics(ScenarioExecutionResult scenarioIteration, string stdout, string stderr, ITestOutputHelper output)
        {
            IterationResult result = base.RecordIterationMetrics(scenarioIteration, stdout, stderr, output);
            AddConsoleMetrics(result, stdout, output);
            return result;
        }

        void AddConsoleMetrics(IterationResult result, string stdout, ITestOutputHelper output)
        {
            output.WriteLine("Processing iteration results.");

            double? trainingTime = null;
            double? firstSearchTime = null;
            double? steadyStateMedianTime = null;
            var currentDecimalSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            using (var reader = new StringReader(stdout))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Match match = Regex.Match(line, @"^Training took \s*(\d+)ms$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        trainingTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }

                    match = Regex.Match(line, @"^Search took \s*(\d+)ms$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        firstSearchTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }

                    match = Regex.Match(line, $@"^Steadystate median search time: \s*(\d+\{currentDecimalSeparator}\d+)ms$");
                    if (match.Success && match.Groups.Count == 2)
                    {
                        //many lines will match, but the final values of these variables will be from the last batch which is presumably the
                        //best measurement of steady state performance
                        steadyStateMedianTime = Convert.ToDouble(match.Groups[1].Value);
                        continue;
                    }
                }
            }

            if (!trainingTime.HasValue)
                throw new FormatException("Training time was not found.");
            if (!firstSearchTime.HasValue)
                throw new FormatException("First Search time was not found.");
            if (!steadyStateMedianTime.HasValue)
                throw new FormatException("Steady state median response time not found.");
                

            result.Measurements.Add(TrainingMetric, trainingTime.Value);
            result.Measurements.Add(FirstSearchMetric, firstSearchTime.Value);
            result.Measurements.Add(MedianSearchMetric, steadyStateMedianTime.Value);

            output.WriteLine($"Training took {trainingTime}ms");
            output.WriteLine($"Search took {firstSearchTime}ms");
            output.WriteLine($"Median steady state search {steadyStateMedianTime.Value}ms");
        }

        /// <summary>
        /// When serializing the result data to benchview this is called to determine if any of the metrics should be reported differently
        /// than they were collected. Both web apps use this to collect several measurements in each iteration, then present those measurements
        /// to benchview as if each was the Duration metric of a distinct scenario test with its own set of iterations.
        /// </summary>
        public override bool TryGetBenchviewCustomMetricReporting(Metric originalMetric, out Metric newMetric, out string newScenarioModelName)
        {
            if(originalMetric.Equals(TrainingMetric))
            {
                newScenarioModelName = "Training";
            }
            else if (originalMetric.Equals(FirstSearchMetric))
            {
                newScenarioModelName = "First Search";
            }
            else if (originalMetric.Equals(MedianSearchMetric))
            {
                newScenarioModelName = "Median Search";
            }
            else
            {
                return base.TryGetBenchviewCustomMetricReporting(originalMetric, out newMetric, out newScenarioModelName);
            }
            newMetric = Metric.ElapsedTimeMilliseconds;
            return true;
        }

        protected static string GetWord2VecNetRepoRootDir(string outputDir)
        {
            return Path.Combine(outputDir, "W"); 
        }

        protected string GetWord2VecNetSrcDirectory(string outputDir)
        {
            return Path.Combine(GetWord2VecNetRepoRootDir(outputDir), "Word2VecScenario");
        }

        string GetWord2VecNetPublishDirectory(DotNetInstallation dotNetInstall, string outputDir, string tfm)
        {
            string dir = Path.Combine(GetWord2VecNetSrcDirectory(outputDir), "bin", dotNetInstall.Architecture, "Release", tfm, "publish");
            if (Directory.Exists(dir))
            {
                return dir;
            }

            dir = Path.Combine(GetWord2VecNetSrcDirectory(outputDir), "bin", "Release", tfm, "publish");
            if (Directory.Exists(dir))
            {
                return dir;
            }

            return null;
        }

        string GetCoreClrRoot()
        {
            string currentDirectory = Directory.GetCurrentDirectory();
            string workspace = Environment.GetEnvironmentVariable("CORECLR_REPO");
            if (workspace == null)
            {
                workspace = currentDirectory;
            }

            return workspace;
        }

        private const string Word2VecNetRepoUrl = "https://github.com/dotnet-perf-bot/Word2Vec.Net.git";
        private const string Word2VecNetCommitSha1Id = "bbf60216bd735ba2ccc3a54570ce735789968f2d";
        private readonly Metric TrainingMetric = new Metric("Training", "ms");
        private readonly Metric FirstSearchMetric = new Metric("First Search", "ms");
        private readonly Metric MedianSearchMetric = new Metric("Median Search", "ms");
    }
}

