using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Reflection;
using Microsoft.Xunit.Performance.Api;

namespace JitBench
{
    class Word2VecBenchmark : MLBenchmark
    {
        public Word2VecBenchmark() : base("Word2Vec") { }

        protected override string ExecutableName => "Word2VecScenario.dll";

        protected override string GetWord2VecNetSrcDirectory(string outputDir)
        {
            return Path.Combine(GetWord2VecNetRepoRootDir(outputDir), "Word2VecScenario");
        }
    }

    abstract class MLBenchmark : Benchmark
    {
        private static readonly HashSet<int> DefaultExitCodes = new HashSet<int>(new[] { 0 });

        public MLBenchmark(string name) : base(name)
        {
            ExePath = ExecutableName;
        }

        protected abstract string ExecutableName { get; }

        public override async Task Setup(DotNetInstallation dotNetInstall, string outputDir, bool useExistingSetup, ITestOutputHelper output)
        {
            if(!useExistingSetup)
            {
                using (var setupSection = new IndentedTestOutputHelper("Setup " + Name, output))
                {
                    await CloneWord2VecNetRepo(outputDir, setupSection);
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

            string word2VecPatchFullPath = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), Word2VecNetPatch);

            await ExecuteGitCommand($"clone {Word2VecNetRepoUrl} {word2VecNetRepoRootDir}", output);
            await ExecuteGitCommand($"checkout {Word2VecNetCommitSha1Id}", output, workingDirectory: word2VecNetRepoRootDir);
            await ExecuteGitCommand($"apply {word2VecPatchFullPath}", output, workingDirectory: word2VecNetRepoRootDir);
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
            var url = "http://mattmahoney.net/dc/text8.zip";
            await FileTasks.DownloadAndUnzip(url, word2VecNetRepoRootDir + "_temp", output);

            FileTasks.MoveFile(Path.Combine(word2VecNetRepoRootDir + "_temp", "text8"), 
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
                throw new DirectoryNotFoundException("Could not find 'publish' directory");
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

                    match = Regex.Match(line, @"^Steadystate median search time: \s*(\d+\.\d+)ms$");
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

        protected abstract string GetWord2VecNetSrcDirectory(string outputDir);

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

        private const string Word2VecNetRepoUrl = "https://github.com/eabdullin/Word2Vec.Net";
        private const string Word2VecNetCommitSha1Id = "6012a2b5b886926918d51b1b56387d785115f448";
        private const string Word2VecNetPatch = "word2vecnet.patch";
        private const string EnvironmentFileName = "Word2VecNetEnvironment.txt";
        private const string StoreDirName = ".store";
        private readonly Metric TrainingMetric = new Metric("Training", "ms");
        private readonly Metric FirstSearchMetric = new Metric("First Search", "ms");
        private readonly Metric MedianSearchMetric = new Metric("Median Search", "ms");
        private readonly Metric MeanSearchMetric = new Metric("Mean Search", "ms");
    }
}

