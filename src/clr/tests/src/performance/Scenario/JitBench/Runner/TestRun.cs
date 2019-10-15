using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing.Session;

namespace JitBench
{
    public class TestRun
    {
        public TestRun()
        {
            Benchmarks = new List<Benchmark>();
            Configurations = new List<BenchmarkConfiguration>();
            BenchmarkRunResults = new List<BenchmarkRunResult>();
            MetricNames = new List<string>();
        }

        public bool UseExistingSetup { get; set; }
        public string DotnetFrameworkVersion { get; set; }
        public string DotnetSdkVersion { get; set; }
        public string PrivateCoreCLRBinDir { get; set; }
        public Architecture Architecture { get; set; }
        public string OutputDir { get; set; }
        public int Iterations { get; set; }
        public List<Benchmark> Benchmarks { get; }
        public List<BenchmarkConfiguration> Configurations { get; private set; }
        public List<string> MetricNames { get; private set; }
        public string BenchviewRunId { get; set; }
        public DotNetInstallation DotNetInstallation { get; private set; }
        public List<BenchmarkRunResult> BenchmarkRunResults { get; private set; }


        public void Run(ITestOutputHelper output)
        {
            CheckConfiguration();
            SetupBenchmarks(output).Wait();
            RunBenchmarks(output);
            WriteBenchmarkResults(output);
        }

        public void CheckConfiguration()
        {
            ValidateOutputDir();
            ValidateMetrics();
        }

        private void ValidateMetrics()
        {
            var validCollectionOptions = new[] {
                    "default",
                    "gcapi",
                    "stopwatch",
                    "BranchMispredictions",
                    "CacheMisses",
                    "InstructionRetired",
                };
            var reducedList = MetricNames.Distinct(StringComparer.OrdinalIgnoreCase);
            var isSubset = !reducedList.Except(validCollectionOptions, StringComparer.OrdinalIgnoreCase).Any();

            if (!isSubset)
            {
                var errorMessage = $"Valid collection metrics are: {string.Join("|", validCollectionOptions)}";
                throw new InvalidOperationException(errorMessage);
            }

            MetricNames = reducedList.Count() > 0 ? new List<string>(reducedList) : new List<string> { "stopwatch" };

            if (MetricNames.Any(n => !n.Equals("stopwatch")))
            {
                if (TraceEventSession.IsElevated() != true)
                {
                    throw new UnauthorizedAccessException("The application is required to run as Administrator in order to capture kernel data");
                }
            }
        }

        private void ValidateOutputDir()
        {
            if (string.IsNullOrWhiteSpace(OutputDir))
                throw new InvalidOperationException("The output directory name cannot be null, empty or white space.");

            if (OutputDir.Any(c => Path.GetInvalidPathChars().Contains(c)))
                throw new InvalidOperationException($"Specified output directory {OutputDir} contains invalid path characters.");

            OutputDir = Path.IsPathRooted(OutputDir) ? OutputDir : Path.GetFullPath(OutputDir);
            if (OutputDir.Length > 80)
            {
                throw new InvalidOperationException($"The output directory path {OutputDir} is too long (>80 characters). Tests writing here may trigger errors because of path length limits");
            }
            try
            {
                Directory.CreateDirectory(OutputDir);
            }
            catch (IOException e)
            {
                throw new Exception($"Unable to create output directory {OutputDir}: {e.Message}", e);
            }
        }

        public void WriteConfiguration(ITestOutputHelper output)
        {
            output.WriteLine("");
            output.WriteLine("  === CONFIGURATION ===");
            output.WriteLine("");
            output.WriteLine("DotnetFrameworkVersion: " + DotnetFrameworkVersion);
            output.WriteLine("DotnetSdkVersion:       " + DotnetSdkVersion);
            output.WriteLine("PrivateCoreCLRBinDir:   " + PrivateCoreCLRBinDir);
            output.WriteLine("Architecture:           " + Architecture);
            output.WriteLine("OutputDir:              " + OutputDir);
            output.WriteLine("Iterations:             " + Iterations);
            output.WriteLine("UseExistingSetup:       " + UseExistingSetup);
            output.WriteLine("Configurations:         " + string.Join(",", Configurations.Select(c => c.Name)));
        }

        async Task SetupBenchmarks(ITestOutputHelper output)
        {
            output.WriteLine("");
            output.WriteLine("  === SETUP ===");
            output.WriteLine("");

            if(UseExistingSetup)
            {
                output.WriteLine("UseExistingSetup is TRUE. Setup will be skipped.");
            }
            await PrepareDotNet(output);
            foreach (Benchmark benchmark in Benchmarks)
            {
                if(!benchmark.IsArchitectureSupported(Architecture))
                {
                    output.WriteLine("Benchmark " + benchmark.Name + " does not support architecture " + Architecture + ". Skipping setup.");
                    continue;
                }
                await benchmark.Setup(DotNetInstallation, OutputDir, UseExistingSetup, output);
            }
        }

        async Task PrepareDotNet(ITestOutputHelper output)
        {
            if (!UseExistingSetup)
            {
                DotNetSetup setup = new DotNetSetup(Path.Combine(OutputDir, ".dotnet"))
                                .WithSdkVersion(DotnetSdkVersion)
                                .WithArchitecture(Architecture);
                if(DotnetFrameworkVersion != "use-sdk")
                {
                    setup.WithFrameworkVersion(DotnetFrameworkVersion);
                }
                if (PrivateCoreCLRBinDir != null)
                {
                    setup.WithPrivateRuntimeBinaryOverlay(PrivateCoreCLRBinDir);
                }
                DotNetInstallation = await setup.Run(output);
            }
            else
            {
                DotNetInstallation = new DotNetInstallation(Path.Combine(OutputDir, ".dotnet"), DotnetFrameworkVersion, DotnetSdkVersion, Architecture);
            }
        }

        void RunBenchmarks(ITestOutputHelper output)
        {
            output.WriteLine("");
            output.WriteLine("  === EXECUTION ===");
            output.WriteLine("");
            foreach (Benchmark benchmark in Benchmarks)
            {
                if (!benchmark.IsArchitectureSupported(Architecture))
                {
                    output.WriteLine("Benchmark " + benchmark.Name + " does not support architecture " + Architecture + ". Skipping run.");
                    continue;
                }
                BenchmarkRunResults.AddRange(benchmark.Run(this, output));
            }
        }

        public void WriteBenchmarkResults(ITestOutputHelper output)
        {
            output.WriteLine("");
            output.WriteLine("  === RESULTS ===");
            output.WriteLine("");
            WriteBenchmarkResultsTable((b, m) => b.GetDefaultDisplayMetrics().Any(metric => metric.Equals(m)), output);
        }

        void WriteBenchmarkResultsTable(Func<Benchmark,Metric, bool> primaryMetricSelector, ITestOutputHelper output)
        {
            List<ResultTableRowModel> rows = BuildRowModels(primaryMetricSelector);
            List<ResultTableColumn> columns = BuildColumns();
            List<List<string>> formattedCells = new List<List<string>>();
            List<string> headerCells = new List<string>();
            foreach(var column in columns)
            {
                headerCells.Add(column.Heading);
            }
            formattedCells.Add(headerCells);
            foreach(var row in rows)
            {
                List<string> rowFormattedCells = new List<string>();
                foreach(var column in columns)
                {
                    rowFormattedCells.Add(column.CellFormatter(row));
                }
                formattedCells.Add(rowFormattedCells);
            }
            StringBuilder headerRow = new StringBuilder();
            StringBuilder headerRowUnderline = new StringBuilder();
            StringBuilder rowFormat = new StringBuilder();
            for (int j = 0; j < columns.Count; j++)
            {
                int columnWidth = Enumerable.Range(0, formattedCells.Count).Select(i => formattedCells[i][j].Length).Max();
                int hw = headerCells[j].Length;
                headerRow.Append(headerCells[j].PadLeft(hw + (columnWidth - hw) / 2).PadRight(columnWidth + 2));
                headerRowUnderline.Append(new string('-', columnWidth) + "  ");
                rowFormat.Append("{" + j + "," + columnWidth + "}  ");
            }
            output.WriteLine(headerRow.ToString());
            output.WriteLine(headerRowUnderline.ToString());
            for(int i = 1; i < formattedCells.Count; i++)
            {
                output.WriteLine(string.Format(rowFormat.ToString(), formattedCells[i].ToArray()));
            }
        }

        List<ResultTableRowModel> BuildRowModels(Func<Benchmark, Metric, bool> primaryMetricSelector)
        {
            List<ResultTableRowModel> rows = new List<ResultTableRowModel>();
            foreach (Benchmark benchmark in Benchmarks)
            {
                BenchmarkRunResult canonResult = BenchmarkRunResults.Where(r => r.Benchmark == benchmark).FirstOrDefault();
                if (canonResult == null || canonResult.IterationResults == null || canonResult.IterationResults.Count == 0)
                {
                    continue;
                }
                IterationResult canonIteration = canonResult.IterationResults[0];
                foreach (Metric metric in canonIteration.Measurements.Keys)
                {
                    if (primaryMetricSelector(benchmark, metric))
                    {
                        rows.Add(new ResultTableRowModel() { Benchmark = benchmark, Metric = metric });
                    }
                }
            }
            return rows;
        }

        List<ResultTableColumn> BuildColumns()
        {
            List<ResultTableColumn> columns = new List<ResultTableColumn>();
            ResultTableColumn benchmarkColumn = new ResultTableColumn();
            benchmarkColumn.Heading = "Benchmark";
            benchmarkColumn.CellFormatter = row => row.Benchmark.Name;
            columns.Add(benchmarkColumn);
            ResultTableColumn metricNameColumn = new ResultTableColumn();
            metricNameColumn.Heading = "Metric";
            metricNameColumn.CellFormatter = row => $"{row.Metric.Name} ({row.Metric.Unit})";
            columns.Add(metricNameColumn);
            foreach(BenchmarkConfiguration config in Configurations)
            {
                ResultTableColumn column = new ResultTableColumn();
                column.Heading = config.Name;
                column.CellFormatter = row =>
                {
                    var runResult = BenchmarkRunResults.Where(r => r.Benchmark == row.Benchmark && r.Configuration == config).Single();
                    var measurements = runResult.IterationResults.Skip(1).Select(r => r.Measurements.Where(kv => kv.Key.Equals(row.Metric)).Single()).Select(kv => kv.Value);
                    double median = measurements.Median();
                    double q1 = measurements.Quartile1();
                    double q3 = measurements.Quartile3();
                    int digits = Math.Min(Math.Max(0, (int)Math.Ceiling(-Math.Log10(q3-q1) + 1)), 15);
                    return $"{Math.Round(median, digits)} ({Math.Round(q1, digits)}-{Math.Round(q3, digits)})";
                };
                columns.Add(column);
            }
            return columns;
        }

        class ResultTableRowModel
        {
            public Benchmark Benchmark;
            public Metric Metric;
        }

        class ResultTableColumn
        {
            public string Heading;
            public Func<ResultTableRowModel, string> CellFormatter;
        }
    }
}
