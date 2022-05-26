// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// RunBenchmark - .NET Benchmark Performance Harness
//
// Note: This harness is currently built as a CoreCLR test case for ease of running
// against test CORE_ROOT assemblies.   As such, when run with out any parameters,
// it will do nothing and simply return 100.   Use "-run" to actually run tests cases.
//
// Usage: RunBenchmarks [options]
//
//  options:
//
//     -f <xmlFile>               specify benchmark xml control file (default benchmarks.xml)
//     -n <number>                specify number of runs for each benchmark (default is 1)
//     -w                         specify that warmup run should be done first
//     -v                         run in verbose mode
//     -r <rootDir>               specify root directory to run from
//     -s <suite>                 specify a single benchmark suite to run (by name)
//     -i <benchmark>             specify benchmark to include by name (multiple -i's allowed)
//     -e <benchmark>             specify benchmark to exclude by name (multiple -e's allowed)
//     -list                      prints a list of the benchmark names and does nothing else
//     -listsuites                prints a list of the suite names and does nothing else
//     -listtags                  prints a list of the tag names and does nothing else
//     -listexes                  prints a list of full path names to executables in benchmarks
//     -runner                    run benchmarks on using runner(e.g. corerun, default is DesktopCLR)
//     -complus_version <version> run benchmarks on particular DesktopCLR version
//     -run                       run benchmarks
//     -testcase                  run as CoreCLR test case (default)
//     -norun                     prints what would be run, but don't run benchmarks
//     -tags <tags>               specify benchmarks with tags to include
//     -notags <tags>             specify benchmarks with tags to exclude
//     -csvfile                   specify name of Comma Separated Value output file (default console)
//
// Benchmark .XML Control File format:
//
// <?xml version="1.0" encoding="UTF-8"?>
// <benchmark-system>
//     <benchmark-root-directory>ROOT_DIRECTORY</benchmark-root-directory> // optional, can be on command line
//     <benchmark-suite>
//         <name>SUITE_NAME</name>
//             <benchmark>
//                 <name>BENCHMARK_NAME</name>
//                 <directory>BENCHMARK_DIRECTORY</directory>
//                 <executable>EXECUTABLE_PATH</executable>
//                 <args>EXECUTABLE_ARGUMENTS</args>           // optional args, can redirect output: &gt; > out
//                 <run-in-shell>true</run-in-shell>           // optional
//                 <useSSE>true</useSSE>                       // optional use SSE
//                 <useAVX>true</useAVX>                       // optional use AVX
//                 <tags>SIMD,SSE</tags>                       // optional tags for inclusion/exclusion
//             </benchmark>
//             ...
//             LIST_OF_BENCHMARKS
//             ...
//     </benchmark-suite>
//     ...
//     LIST_OF_BENCHMARK_SUITES
//     ...
// </benchmark-system>
//
// Where:
//
//  ROOT_DIRECTORY = root directory for all benchmarks described in this file
//  SUITE_NAME = suite name that benchmark belongs to
//  BENCHMARK_NAME = benchmark name
//  BENCHMARK_DIRECTORY = benchmark directory, relative to root (becomes working directory for benchmark)
//  EXECUTABLE_PATH = relative path (to working directory) for benchmark (simplest form foo.exe)
//  EXECUTABLE_ARGUMENTS = argument for benchmark if any
//

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.IO;

namespace BenchmarkConsoleApplication
{
    // Benchmark Suite - includes suite name an list of benchmarks included in suite.

    internal class BenchmarkSuite
    {
        public string SuiteName;
        public List<Benchmark> BenchmarkList;

        public BenchmarkSuite
        (
            string suiteName
        )
        {
            SuiteName = suiteName;
            BenchmarkList = new List<Benchmark>();
        }
    }

    // Benchmark Tag Set - includes tag name an list of benchmarks that have been tagged with this tag.

    internal class BenchmarkTagSet
    {
        public string TagName;
        public List<Benchmark> BenchmarkList;

        public BenchmarkTagSet
        (
            string tagName
        )
        {
            TagName = tagName;
            BenchmarkList = new List<Benchmark>();
        }
    }

    // Benchmark - includes benchmark name, suite name, tags, working directory, executable name,
    //             executable argurments, whether to run in a shell, whether to use SSE, whether to use
    //             AVX and expected results (exit code).

    internal class Benchmark
    {
        public string Name;
        public string SuiteName;
        public string Tags;
        public string WorkingDirectory;
        public string ExeName;
        public string ExeArgs;
        public bool DoRunInShell;
        public bool UseSSE;
        public bool UseAVX;
        public int ExpectedResults;

        public Benchmark
        (
            string name,
            string suiteName,
            string tags,
            string workingDirectory,
            string exeName,
            string exeArgs,
            bool doRunInShell,
            bool useSSE,
            bool useAVX,
            int expectedResults
        )
        {
            Name = name;
            SuiteName = suiteName;
            Tags = tags;
            WorkingDirectory = workingDirectory;
            ExeName = exeName;
            ExeArgs = exeArgs;
            DoRunInShell = doRunInShell;
            UseSSE = useSSE;
            UseAVX = useAVX;
            ExpectedResults = expectedResults;
        }
    }

    // Benchmark Results - includes benchmark, array of times for each iteration of the benchmark
    //                     minimum time, maximum time, average time, standard deviation and number
    //                     of failures.

    internal class Results
    {
        public Benchmark Benchmark;
        public long[] Times;
        public long Minimum;
        public long Maximum;
        public long Average;
        public double StandardDeviation;
        public int Failures;

        public Results
        (
            Benchmark benchmark,
            int numberOfRuns
        )
        {
            Benchmark = benchmark;
            Times = new long[numberOfRuns + 1]; // leave empty slot at index 0, not used.
            Minimum = 0;
            Maximum = 0;
            Average = 0;
            StandardDeviation = 0.0;
            Failures = 0;
        }
    }

    // Controls - command line controls used to

    internal class Controls
    {
        public bool DoRun; // Actually execute the benchmarks
        public bool DoWarmUpRun; // Do a warmup run first.
        public bool DoVerbose; // Run in verbose mode.
        public bool DoPrintResults; // Print results of runs.
        public bool DoRunAsTestCase; // Run as test case (default)
        public string Runner; // Use the runner to execute benchmarks (e.g. corerun.exe)
        public bool DoDebugBenchmark; // Execute benchmark under debugger (Windows).
        public bool DoListBenchmarks; // List out the benchmarks from .XML file
        public bool DoListBenchmarkSuites; // List out the benchmark suites from .XML file
        public bool DoListBenchmarkTagSets; // List out the benchmark tag sets from the .XML file
        public bool DoListBenchmarkExecutables; // List out the benchmark exectubles from the .XML file
        public int NumberOfRunsPerBenchmark; // Number of runs/iterations each benchmark should be run
        public string ComplusVersion; // COMPlus_VERSION for .NET Framework hosted runs (optional).
        public string BenchmarksRootDirectory; // Root directory for benchmark tree specified in .XML file.
        public string BenchmarkXmlFileName; // Benchmark .XML filename (default coreclr_benchmarks.xml)
        public string BenchmarkCsvFileName; // Benchmark output .CSV filename (default coreclr_benchmarks.csv)
        public string SuiteName; // Specific benchmark suite name to be executed (optional).
        public List<string> IncludeBenchmarkList; // List of specific benchmarks to be included (optional)
        public List<string> ExcludeBenchmarkList; // List of specific benchmarks to be excluded (optional)
        public List<string> IncludeTagList; //List of specific benchmark tags to be included (optional)
        public List<string> ExcludeTagList; //List of specific benchmark tags to be excluded (optional)
    }

    // Benchmark System - actual benchmark system.  Includes the controls, the command processer, main
    //                    execution engine, benchmarks lists, selected benchmark lists,
    //                    benchmark suite dictionary, benchmark tag dictionary, and results table.

    internal class BenchmarkSystem
    {
        public const bool OptionalField = true;

        public Controls Controls = new Controls()
        {
            NumberOfRunsPerBenchmark = 1,
            DoWarmUpRun = false,
            DoVerbose = false,
            Runner = "",
            DoDebugBenchmark = false,
            DoListBenchmarks = false,
            DoListBenchmarkSuites = false,
            DoListBenchmarkTagSets = false,
            DoListBenchmarkExecutables = false,
            DoRun = false,
            DoRunAsTestCase = true,
            DoPrintResults = false,
            ComplusVersion = "",
            SuiteName = "",
            BenchmarksRootDirectory = "",
            BenchmarkXmlFileName = "coreclr_benchmarks.xml",
            BenchmarkCsvFileName = "coreclr_benchmarks.csv",
            IncludeBenchmarkList = new List<string>(),
            ExcludeBenchmarkList = new List<string>(),
            IncludeTagList = new List<string>(),
            ExcludeTagList = new List<string>()
        };

        public Dictionary<string, BenchmarkSuite>
            BenchmarkSuiteTable = new Dictionary<string, BenchmarkSuite>();
        public Dictionary<string, BenchmarkTagSet>
            BenchmarkTagSetTable = new Dictionary<string, BenchmarkTagSet>();
        public List<Benchmark>
            BenchmarkList = new List<Benchmark>();
        public List<Benchmark>
            SelectedBenchmarkList = new List<Benchmark>();
        public List<Results>
            ResultsList = new List<Results>();

        public int NumberOfBenchmarksRun;
        public int Failures = 0;

        public char[] ListSeparatorCharSet = new char[] { ',', ' ' };
        public char[] DirectorySeparatorCharSet = new char[] { '/', '\\' };

        // Main driver for benchmark system.

        public static int Main(string[] args)
        {
            int exitCode = 0;

            Console.WriteLine("RyuJIT Benchmark System");
            try
            {
                BenchmarkSystem benchmarkSystem = new BenchmarkSystem();
                benchmarkSystem.ProcessCommandLine(args);
                benchmarkSystem.BuildBenchmarksList();
                benchmarkSystem.SelectBenchmarks();
                benchmarkSystem.RunBenchmarks();
                benchmarkSystem.ReportResults();

                bool doRunAsTestCase = benchmarkSystem.Controls.DoRunAsTestCase;
                if (doRunAsTestCase)
                {
                    exitCode = 100;
                }
            }
            catch (Exception exception)
            {
                //  Need to find portable Environment.Exit()
                if (exception.Message == "Exit")
                {
                    exitCode = 0;
                }
                else
                {
                    Console.WriteLine("{0}", exception.ToString());
                    exitCode = -1;
                }
            }

            return exitCode;
        }

        // Command line processor.

        public void ProcessCommandLine(string[] args)
        {
            Controls controls = Controls;
            try
            {
                for (int i = 0; i < args.Length;)
                {
                    string arg = args[i++];
                    string benchmark;
                    string[] tags;
                    string runner;

                    switch (arg)
                    {
                        case "-n":
                            arg = args[i++];
                            controls.NumberOfRunsPerBenchmark = Int32.Parse(arg);
                            break;
                        case "-testcase":
                            controls.DoRun = false;
                            controls.DoPrintResults = false;
                            controls.DoRunAsTestCase = true;
                            break;
                        case "-run":
                            controls.DoRun = true;
                            controls.DoPrintResults = true;
                            controls.DoRunAsTestCase = false;
                            break;
                        case "-norun":
                            controls.DoRun = false;
                            controls.DoPrintResults = true;
                            controls.DoRunAsTestCase = false;
                            break;
                        case "-w":
                            controls.DoWarmUpRun = true;
                            break;
                        case "-v":
                            controls.DoVerbose = true;
                            break;
                        case "-r":
                            arg = args[i++];
                            controls.BenchmarksRootDirectory = PlatformSpecificDirectoryName(arg);
                            break;
                        case "-f":
                            arg = args[i++];
                            controls.BenchmarkXmlFileName = arg;
                            break;
                        case "-csvfile":
                            arg = args[i++];
                            controls.BenchmarkCsvFileName = arg;
                            break;
                        case "-s":
                            arg = args[i++];
                            controls.SuiteName = arg;
                            break;
                        case "-i":
                            arg = args[i++];
                            benchmark = arg;
                            controls.IncludeBenchmarkList.Add(benchmark);
                            break;
                        case "-e":
                            arg = args[i++];
                            benchmark = arg;
                            controls.ExcludeBenchmarkList.Add(benchmark);
                            break;
                        case "-list":
                            controls.DoListBenchmarks = true;
                            controls.DoRunAsTestCase = false;
                            break;
                        case "-listsuites":
                            controls.DoListBenchmarkSuites = true;
                            controls.DoRunAsTestCase = false;
                            break;
                        case "-listtags":
                            controls.DoListBenchmarkTagSets = true;
                            controls.DoRunAsTestCase = false;
                            break;
                        case "-listexes":
                            controls.DoListBenchmarkExecutables = true;
                            controls.DoRunAsTestCase = false;
                            break;
                        case "-tags":
                            arg = args[i++];
                            tags = arg.Split(ListSeparatorCharSet, StringSplitOptions.RemoveEmptyEntries);
                            controls.IncludeTagList.AddRange(tags);
                            break;
                        case "-notags":
                            arg = args[i++];
                            tags = arg.Split(ListSeparatorCharSet, StringSplitOptions.RemoveEmptyEntries);
                            controls.ExcludeTagList.AddRange(tags);
                            break;
                        case "-runner":
                            arg = args[i++];
                            runner = arg;
                            controls.Runner = runner;
                            break;
                        case "-debug":
                            controls.DoDebugBenchmark = true;
                            break;
                        case "-complus_version":
                            arg = args[i++];
                            controls.ComplusVersion = arg;
                            break;
                        default:
                            throw new Exception("invalid argument: " + arg);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception: {0}", exception);
                Usage();
            }
        }

        // Print out usage and exit.

        public void Usage()
        {
            Console.WriteLine("");
            Console.WriteLine("Usage: RunBenchmarks [options]");
            Console.WriteLine("");
            Console.WriteLine("   options: ");
            Console.WriteLine("");
            Console.WriteLine("   -f <xmlFile>   specify benchmark xml file (default coreclr_benchmarks.xml)");
            Console.WriteLine("   -n <number>    specify number of runs for each benchmark (default is 1)");
            Console.WriteLine("   -w             specify that warmup run should be done first");
            Console.WriteLine("   -v             run in verbose mode");
            Console.WriteLine("   -r <rootDir>   specify root directory to run from");
            Console.WriteLine("   -s <suite>     specify a single benchmark suite to run (by name)");
            Console.WriteLine("   -i <benchmark> specify benchmark to include by name (multiple -i's allowed)");
            Console.WriteLine("   -e <benchmark> specify benchmark to exclude by name (multiple -e's allowed)");
            Console.WriteLine("   -list          prints a list of the benchmark names and does nothing else");
            Console.WriteLine("   -listsuites    prints a list of the suite names and does nothing else");
            Console.WriteLine("   -listtags      prints a list of the tag names and does nothing else");
            Console.WriteLine("   -listexes      prints a list of the benchmark executables and does nothing else");
            Console.WriteLine("   -runner        runner to be used to run benchmarks (e.g. corerun, default is DesktopCLR)");
            Console.WriteLine("   -complus_version <version> run benchmarks on particular DesktopCLR version");
            Console.WriteLine("   -run           run benchmarks");
            Console.WriteLine("   -norun         prints what would be run, but nothing is executed");
            Console.WriteLine("   -testcase      run as CoreCLR test case (default)");
            Console.WriteLine("   -norun         prints what would be run, but don't run benchmarks");
            Console.WriteLine("   -tags <tags>   specify benchmarks with tags to include");
            Console.WriteLine("   -notags <tags> specify benchmarks with tags to exclude");
            Console.WriteLine("   -csvfile       specify name of Comma Separated Value output file (default coreclr_benchmarks.csv)");

            Exit(-1);
        }

        // Add a benchmark to the list of benchmarks read in from the .XML file.

        public void AddBenchmark
        (
            string name,
            string suiteName,
            string tags,
            string workingDirectory,
            string exeName,
            string exeArgs,
            bool doRunInShell,
            bool useSSE,
            bool useAVX,
            int expectedResults
        )
        {
            BenchmarkSuite benchmarkSuite;
            BenchmarkTagSet benchmarkTagSet;
            Benchmark benchmark;

            benchmark = new Benchmark(name, suiteName, tags,
                workingDirectory, exeName, exeArgs, doRunInShell, useSSE, useAVX, expectedResults);
            BenchmarkList.Add(benchmark);

            if (!BenchmarkSuiteTable.TryGetValue(suiteName, out benchmarkSuite))
            {
                benchmarkSuite = new BenchmarkSuite(suiteName);
                BenchmarkSuiteTable.Add(suiteName, benchmarkSuite);
            }
            benchmarkSuite.BenchmarkList.Add(benchmark);

            string[] tagList = tags.Split(ListSeparatorCharSet, StringSplitOptions.RemoveEmptyEntries);
            foreach (string tag in tagList)
            {
                if (!BenchmarkTagSetTable.TryGetValue(tag, out benchmarkTagSet))
                {
                    benchmarkTagSet = new BenchmarkTagSet(tag);
                    BenchmarkTagSetTable.Add(tag, benchmarkTagSet);
                }
                benchmarkTagSet.BenchmarkList.Add(benchmark);
            }
        }

        // Exit benchmark system with specified exit code.

        public int Exit(int exitCode)
        {
            //  Need to find portable Environment.Exit()
            switch (exitCode)
            {
                case 0:
                case -1:
                case -2:
                    throw new Exception("Exit");
                default:
                    throw new Exception("BadExit");
            }
        }

        // Constructed platform specific field name given either Unix style or Windows style
        // directory name.

        public string PlatformSpecificDirectoryName
        (
            string directoryName
        )
        {
            if (directoryName == "")
                return "";

            string[] path = directoryName.Split(DirectorySeparatorCharSet,
                    StringSplitOptions.RemoveEmptyEntries);
            string platformSpecificDirectoryName = System.IO.Path.Combine(path);

            bool absolutePath = false;
            char firstChar = directoryName[0];

            for (int i = 0; i < DirectorySeparatorCharSet.Length; i++)
            {
                if (firstChar == DirectorySeparatorCharSet[i])
                {
                    absolutePath = true;
                    break;
                }
            }

            if (absolutePath)
            {
                platformSpecificDirectoryName = (Path.DirectorySeparatorChar + platformSpecificDirectoryName);
            }

            return platformSpecificDirectoryName;
        }

        public static bool GetBool
        (
            XElement node,
            string name,
            bool optional = true
        )
        {
            string value = node.Element(name)?.Value;

            if (value == "true")
                return true;
            if (value == "false")
                return false;
            if (optional)
                return false;

            throw new Exception("bad boolean value: " + value);
        }

        public static int GetInteger
        (
            XElement node,
            string name,
            bool optional = true
        )
        {
            string value = node.Element(name)?.Value;

            if (value != "")
            {
                int number = Int32.Parse(value);
                return number;
            }
            if (optional)
                return 0;

            throw new Exception("bad integer value: " + value);
        }

        // Build list of benchmarks by reading in and processing .XML file.

        public void BuildBenchmarksList()
        {
            bool doRunAsTestCase = Controls.DoRunAsTestCase;
            string benchmarksRootDirectory = Controls.BenchmarksRootDirectory;
            string benchmarkXmlFileName = Controls.BenchmarkXmlFileName;
            string benchmarkXmlFullFileName;

            string benchmarkRootDirectoryName;
            string benchmarkSuiteName;
            string benchmarkDirectoryName;
            string benchmarkName;
            string benchmarkExecutableName;
            string benchmarkArgs;
            bool doRunInShell;
            bool useSSE;
            bool useAVX;
            int expectedResults;
            string tags;

            // If we aren't being asked to run benchmarks or print results
            // then we must be being executed as a simple CoreCLR test case.
            // Don't bother reading XML file.

            if (doRunAsTestCase)
            {
                return;
            }

            benchmarkXmlFullFileName = benchmarkXmlFileName;

            // Load XML description of benchmarks.

            XElement benchmarkXml = XElement.Load(benchmarkXmlFullFileName);

            // Get root directory for benchmark system.  Command line argument overrides
            // specification in benchmark control file.

            benchmarkRootDirectoryName = Controls.BenchmarksRootDirectory;
            if (benchmarkRootDirectoryName == "")
            {
                benchmarkRootDirectoryName =
                Controls.BenchmarksRootDirectory = benchmarkRootDirectoryName;
            }
            benchmarkRootDirectoryName = PlatformSpecificDirectoryName(benchmarkRootDirectoryName);
            Controls.BenchmarksRootDirectory = benchmarkRootDirectoryName;

            // Process each benchmark suite in the list of benchmark suites.

            var benchmarkSuiteList = from e in benchmarkXml.Descendants("benchmark-suite") select e;
            foreach (XElement benchmarkSuite in benchmarkSuiteList)
            {
                benchmarkSuiteName = benchmarkSuite.Element("name").Value;

                //Process each benchmark in benchmark suite.

                var benchmarkList = from e in benchmarkSuite.Descendants("benchmark") select e;
                foreach (XElement benchmark in benchmarkList)
                {
                    benchmarkName = benchmark.Element("name").Value;
                    benchmarkDirectoryName = benchmark.Element("directory")?.Value ?? "";
                    benchmarkDirectoryName = PlatformSpecificDirectoryName(benchmarkDirectoryName);
                    benchmarkExecutableName = benchmark.Element("executable").Value;
                    benchmarkArgs = benchmark.Element("args")?.Value ?? "";
                    useSSE = GetBool(benchmark, "useSSE");
                    useAVX = GetBool(benchmark, "useAVX");
                    doRunInShell = GetBool(benchmark, "run-in-shell");
                    expectedResults = GetInteger(benchmark, "expected-results");
                    tags = benchmark.Element("tags")?.Value ?? "";
                    AddBenchmark(benchmarkName, benchmarkSuiteName, tags, benchmarkDirectoryName,
                        benchmarkExecutableName, benchmarkArgs, doRunInShell, useSSE, useAVX, expectedResults);
                }
            }

            // Process early out controls that just do listing.

            if (Controls.DoListBenchmarks)
            {
                ListBenchmarks();
                Exit(-2);
            }
            if (Controls.DoListBenchmarkSuites)
            {
                ListBenchmarkSuites();
                Exit(-2);
            }
            if (Controls.DoListBenchmarkTagSets)
            {
                ListBenchmarkTagSets();
                Exit(-2);
            }
            if (Controls.DoListBenchmarkExecutables)
            {
                ListBenchmarkExecutables();
                Exit(-2);
            }
        }

        // Print out list of benchmarks read in.

        public void ListBenchmarks()
        {
            Console.WriteLine("Benchmark List");
            foreach (Benchmark benchmark in BenchmarkList)
            {
                Console.WriteLine("{0}", benchmark.Name);
                Console.WriteLine("    Suite: {0}", benchmark.SuiteName);
                Console.WriteLine("    WorkingDirectory: {0}", benchmark.WorkingDirectory);
                Console.WriteLine("    ExeName: {0}", benchmark.ExeName);
                Console.WriteLine("    ExeArgs: {0}", benchmark.ExeArgs);
                Console.WriteLine("    RunInShell: {0}", benchmark.DoRunInShell);
                Console.WriteLine("    UseSSE: {0}", benchmark.UseSSE);
                Console.WriteLine("    UseAVX: {0}", benchmark.UseAVX);
                Console.WriteLine("    Tags: {0}", benchmark.Tags);
            }
        }

        // Print out list of benchmark suites read in.

        public void ListBenchmarkSuites()
        {
            Console.WriteLine("Benchmark Suite List");
            var benchmarkSuiteList = BenchmarkSuiteTable.Keys;
            foreach (var suiteName in benchmarkSuiteList)
            {
                Console.WriteLine("{0}", suiteName);
            }
        }

        // Print out list of benchmark tags read in.

        public void ListBenchmarkTagSets()
        {
            Console.WriteLine("Benchmark TagSet List");
            var benchmarkTagSetList = BenchmarkTagSetTable.Keys;
            foreach (var tagName in benchmarkTagSetList)
            {
                Console.WriteLine("{0}", tagName);
            }
        }

        // Print out list of benchmark executables read in.

        public void ListBenchmarkExecutables()
        {
            Console.WriteLine("Benchmark Executable List");
            var benchmarkList = BenchmarkList;
            foreach (var benchmark in benchmarkList)
            {
                string benchmarksRootDirectory = Controls.BenchmarksRootDirectory;
                string benchmarkDirectory = System.IO.Path.Combine(benchmarksRootDirectory, benchmark.WorkingDirectory);
                string workingDirectory = benchmarkDirectory;
                string executableName = System.IO.Path.Combine(workingDirectory, benchmark.ExeName);

                Console.WriteLine("{0}", executableName);
            }
        }

        // Select benchmarks to run based on controls for suite, tag, or specfic
        // benchmark inclusion/exclusion.

        public void SelectBenchmarks()
        {
            List<Benchmark> benchmarkList = BenchmarkList;
            List<string> includeBenchmarkList = Controls.IncludeBenchmarkList;
            List<string> excludeBenchmarkList = Controls.ExcludeBenchmarkList;
            List<string> includeTagList = Controls.IncludeTagList;
            List<string> excludeTagList = Controls.ExcludeTagList;
            string suiteName = Controls.SuiteName;

            if (suiteName != "")
            {
                BenchmarkSuite benchmarkSuite = null;

                if (!BenchmarkSuiteTable.TryGetValue(suiteName, out benchmarkSuite))
                {
                    throw new Exception("bad suite name: " + suiteName);
                }
                benchmarkList = benchmarkSuite.BenchmarkList;
            }

            foreach (Benchmark benchmark in benchmarkList)
            {
                string benchmarkName = benchmark.Name;
                bool include = true;

                if (includeBenchmarkList.Count > 0)
                {
                    include = false;
                    if (includeBenchmarkList.Contains(benchmarkName))
                    {
                        include = true;
                    }
                }

                if (include && (excludeBenchmarkList.Count > 0))
                {
                    if (excludeBenchmarkList.Contains(benchmarkName))
                    {
                        include = false;
                    }
                }

                if (include && (excludeTagList.Count > 0))
                {
                    foreach (string tag in excludeTagList)
                    {
                        BenchmarkTagSet benchmarkTagSet = null;

                        if (!BenchmarkTagSetTable.TryGetValue(tag, out benchmarkTagSet))
                        {
                            throw new Exception("bad tag: " + tag);
                        }

                        List<Benchmark> excludeTagBenchmarkList = benchmarkTagSet.BenchmarkList;

                        if (excludeTagBenchmarkList.Contains(benchmark))
                        {
                            include = false;
                        }
                    }
                }

                if (include)
                {
                    SelectedBenchmarkList.Add(benchmark);
                }
            }
        }

        // Run benchmark - actually run benchmark the specified number of times and using the specified
        // controls and return results.

        public Results RunBenchmark(Benchmark benchmark)
        {
            bool doRun = Controls.DoRun;
            int numberOfRuns = Controls.NumberOfRunsPerBenchmark;
            bool doWarmUpRun = Controls.DoWarmUpRun;
            string runner = Controls.Runner;
            bool doDebugBenchmark = Controls.DoDebugBenchmark;
            bool doVerbose = Controls.DoVerbose;
            string complusVersion = Controls.ComplusVersion;
            string benchmarksRootDirectory = Controls.BenchmarksRootDirectory;
            string benchmarkDirectory = System.IO.Path.Combine(benchmarksRootDirectory, benchmark.WorkingDirectory);
            bool doRunInShell = benchmark.DoRunInShell;
            bool useSSE = benchmark.UseSSE;
            bool useAVX = benchmark.UseAVX;
            int expectedResults = benchmark.ExpectedResults;
            int failureResults = ~expectedResults;
            int actualResults;
            int failures = 0;

            Results results = new Results(benchmark, numberOfRuns);

            if (!doRun)
            {
                return results;
            }

            string workingDirectory = benchmarkDirectory;
            string fileName = System.IO.Path.Combine(workingDirectory, benchmark.ExeName);
            string args = benchmark.ExeArgs;

            if (runner != "")
            {
                args = fileName + " " + args;
                fileName = runner;
            }

            if (doDebugBenchmark)
            {
                args = "/debugexe " + fileName + " " + args;
                fileName = "devenv.exe";
            }

            if (doRunInShell)
            {
                args = "/C " + fileName + " " + args;
                fileName = "cmd.exe";
            }

            if (doVerbose)
            {
                Console.WriteLine("Running benchmark {0} ...", benchmark.Name);
                Console.WriteLine("Invoking: {0} {1}", fileName, args);
            }

            for (int run = (doWarmUpRun) ? 0 : 1; run <= numberOfRuns; run++)
            {
                ProcessStartInfo startInfo = new ProcessStartInfo()
                {
                    FileName = fileName,
                    Arguments = args,
                    WorkingDirectory = workingDirectory,
                    UseShellExecute = false
                };

                if (complusVersion != "")
                {
                    startInfo.Environment["COMPlus_Version"] = complusVersion;
                    startInfo.Environment["COMPlus_DefaultVersion"] = complusVersion;
                }
                if (useSSE)
                {
                    startInfo.Environment["COMPlus_FeatureSIMD"] = "1";
                    startInfo.Environment["COMPlus_EnableAVX"] = "0";
                }
                if (useAVX)
                {
                    startInfo.Environment["COMPlus_FeatureSIMD"] = "1";
                    startInfo.Environment["COMPlus_EnableAVX"] = "1";
                }
                startInfo.Environment["COMPlus_gcConcurrent"] = "0";
                startInfo.Environment["COMPlus_gcServer"] = "0";
                startInfo.Environment["COMPlus_NoGuiOnAssert"] = "1";
                startInfo.Environment["COMPlus_BreakOnUncaughtException"] = "0";

                var clockTime = Stopwatch.StartNew();
                int exitCode = 0;
                try
                {
                    using (var proc = Process.Start(startInfo))
                    {
                        proc.EnableRaisingEvents = true;
                        proc.WaitForExit();
                        exitCode = proc.ExitCode;
                    }
                    this.NumberOfBenchmarksRun++;
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Could not launch test {0} exception: {1}",
                        startInfo.FileName, exception);
                    exitCode = failureResults;
                }
                clockTime.Stop();
                actualResults = exitCode;

                long time = clockTime.ElapsedMilliseconds;
                if (actualResults != expectedResults)
                {
                    failures++;
                    time = 0;
                }
                results.Times[run] = time;

                if (doVerbose)
                {
                    Console.Write("Iteration benchmark {0} ", benchmark.Name);
                    if (actualResults == expectedResults)
                    {
                        Console.Write("elapsed time {0}ms", time);
                    }
                    else
                    {
                        Console.Write("FAILED(expected={0}, actual={1})", expectedResults, actualResults);
                    }
                    if (run == 0)
                    {
                        Console.Write(" (warmup)");
                    }
                    Console.WriteLine("");
                }
            }

            // Calculate min, max, avg, and std devation.

            long sum = 0;
            long minimum = results.Times[1];
            long maximum = minimum;

            for (int run = 1; run <= numberOfRuns; run++)
            {
                long time = results.Times[run];

                sum += time;
                minimum = Math.Min(minimum, time);
                maximum = Math.Max(maximum, time);
            }

            long average = sum / (long)numberOfRuns;
            double standardDeviation = 0.0;

            if (numberOfRuns > 1)
            {
                double s = 0.0;
                double a = (double)average;
                double n = (double)numberOfRuns;
                for (int run = 1; run <= numberOfRuns; run++)
                {
                    double time = (double)results.Times[run];
                    double t = (time - a);
                    s += (t * t);
                }
                double variance = s / n;

                if (a == 0.0)
                {
                    standardDeviation = 0.0;
                }
                else
                {
                    standardDeviation = 100.0 * (Math.Sqrt(variance) / a); // stddev as a percentage
                    standardDeviation = Math.Round(standardDeviation, 2, MidpointRounding.AwayFromZero);
                }
            }

            // Record results and return.

            results.Average = average;
            results.Minimum = minimum;
            results.Maximum = maximum;
            results.StandardDeviation = standardDeviation;
            results.Failures = failures;

            return results;
        }

        // Run the list of selected benchmarks.

        public void RunBenchmarks()
        {
            bool doVerbose = Controls.DoVerbose;

            if (doVerbose)
            {
                Console.WriteLine("Run benchmarks ...");
            }

            foreach (Benchmark benchmark in SelectedBenchmarkList)
            {
                Results results = RunBenchmark(benchmark);
                Failures += results.Failures;
                ResultsList.Add(results);
            }
        }

        // Report the results of the benchmark run.

        public void ReportResults()
        {
            bool doVerbose = Controls.DoVerbose;
            bool doPrintResults = Controls.DoPrintResults;
            string benchmarkCsvFileName = Controls.BenchmarkCsvFileName;
            int numberOfBenchmarksRun = this.NumberOfBenchmarksRun;
            int numberOfFailures = this.Failures;
            int numberOfPasses = numberOfBenchmarksRun - numberOfFailures;
            int numberOfRunsPerBenchmark = Controls.NumberOfRunsPerBenchmark;
            int numberOfFailuresPerBenchmark = 0;

            if (!doPrintResults)
            {
                return;
            }

            if (doVerbose)
            {
                Console.WriteLine("Generating benchmark report on {0}", benchmarkCsvFileName);
            }

            using (TextWriter outputFile = File.CreateText(benchmarkCsvFileName))
            {
                outputFile.WriteLine("Benchmark,Minimum(ms),Maximum(ms),Average(ms),StdDev(%),Passed/Failed(#)");
                foreach (Results results in ResultsList)
                {
                    string name = results.Benchmark.Name;
                    outputFile.Write("{0},", name);

                    long minimum = results.Minimum;
                    long maximum = results.Maximum;
                    long average = results.Average;
                    double standardDeviation = results.StandardDeviation;
                    outputFile.Write("{0},{1},{2},{3}", minimum, maximum, average, standardDeviation);

                    numberOfFailuresPerBenchmark = results.Failures;
                    numberOfPasses = (numberOfPasses < 0) ? 0 : numberOfPasses;
                    if (numberOfFailuresPerBenchmark > 0)
                    {
                        outputFile.Write(",FAILED({0})", numberOfFailuresPerBenchmark);
                    }
                    else
                    {
                        outputFile.Write(",PASSED({0})", numberOfRunsPerBenchmark);
                    }
                    outputFile.WriteLine("");
                }

                outputFile.WriteLine("TOTAL BENCHMARKS({0}), PASSED({1}), FAILED({2})",
                        numberOfBenchmarksRun, numberOfPasses, numberOfFailures);
            }
        }
    }
}
