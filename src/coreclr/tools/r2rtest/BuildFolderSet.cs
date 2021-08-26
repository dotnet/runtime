// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.Clr;

namespace R2RTest
{
    public class BuildFolderSet
    {
        const string FrameworkOutputFileName = "framework-r2r.dll";

        private readonly IEnumerable<BuildFolder> _buildFolders;

        private readonly IEnumerable<CompilerRunner> _compilerRunners;

        private readonly BuildOptions _options;

        private readonly Buckets _frameworkCompilationFailureBuckets;

        private readonly Buckets _compilationFailureBuckets;

        private readonly Buckets _executionFailureBuckets;

        private readonly Dictionary<string, byte> _cpaotManagedSequentialResults;

        private readonly Dictionary<string, byte> _crossgenManagedSequentialResults;

        private readonly Dictionary<string, byte> _cpaotRequiresMarshalingResults;

        private readonly Dictionary<string, byte> _crossgenRequiresMarshalingResults;

        private readonly Dictionary<string, string> _frameworkExclusions;

        private long _frameworkCompilationMilliseconds;

        private long _compilationMilliseconds;

        private long _executionMilliseconds;

        private long _buildMilliseconds;

        public BuildFolderSet(
            IEnumerable<BuildFolder> buildFolders,
            IEnumerable<CompilerRunner> compilerRunners,
            BuildOptions options)
        {
            _buildFolders = buildFolders;
            _compilerRunners = compilerRunners;
            _options = options;

            _frameworkCompilationFailureBuckets = new Buckets();
            _compilationFailureBuckets = new Buckets();
            _executionFailureBuckets = new Buckets();

            _cpaotManagedSequentialResults = new Dictionary<string, byte>();
            _crossgenManagedSequentialResults = new Dictionary<string, byte>();

            _cpaotRequiresMarshalingResults = new Dictionary<string, byte>();
            _crossgenRequiresMarshalingResults = new Dictionary<string, byte>();

            _frameworkExclusions = new Dictionary<string, string>();
        }

        private void WriteJittedMethodSummary(StreamWriter logWriter)
        {
            var allMethodsPerModulePerCompiler = new Dictionary<string, HashSet<string>>[(int)CompilerIndex.Count];

            foreach (CompilerRunner runner in _compilerRunners)
            {
                allMethodsPerModulePerCompiler[(int)runner.Index] = new Dictionary<string, HashSet<string>>();
            }

            foreach (BuildFolder folder in FoldersToBuild)
            {
                for (int exeIndex = 0; exeIndex < folder.Executions.Count; exeIndex++)
                {
                    var appMethodsPerModulePerCompiler = new Dictionary<string, HashSet<string>>[(int)CompilerIndex.Count];
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        appMethodsPerModulePerCompiler[(int)runner.Index] = new Dictionary<string, HashSet<string>>();
                        folder.AddModuleToJittedMethodsMapping(allMethodsPerModulePerCompiler[(int)runner.Index], exeIndex, runner.Index);
                        folder.AddModuleToJittedMethodsMapping(appMethodsPerModulePerCompiler[(int)runner.Index], exeIndex, runner.Index);
                    }
                    folder.WriteJitStatistics(appMethodsPerModulePerCompiler, _compilerRunners);
                }
            }

            BuildFolder.WriteJitStatistics(logWriter, allMethodsPerModulePerCompiler, _compilerRunners);
        }

        public bool Compile()
        {
            if (!CompileFramework())
            {
                return false;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            ResolveTestExclusions();

            var compilationsToRun = new List<ProcessInfo>();

            foreach (BuildFolder folder in FoldersToBuild)
            {
                foreach (ProcessInfo[] compilation in folder.Compilations)
                {
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        ProcessInfo compilationProcess = compilation[(int)runner.Index];
                        if (compilationProcess != null)
                        {
                            compilationsToRun.Add(compilationProcess);
                        }
                    }
                }
            }

            ParallelRunner.Run(compilationsToRun, _options.DegreeOfParallelism, _options.MeasurePerf);

            bool success = true;
            var failedCompilationsPerBuilder = new List<KeyValuePair<string, string>>();
            int successfulCompileCount = 0;

            var r2rDumpExecutionsToRun = new List<ProcessInfo>();

            foreach (BuildFolder folder in FoldersToBuild)
            {
                foreach (ProcessInfo[] compilation in folder.Compilations)
                {
                    HashSet<string> failedFiles = new HashSet<string>();
                    string failedBuilders = null;
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        ProcessInfo runnerProcess = compilation[(int)runner.Index];
                        if (runnerProcess == null || runnerProcess.IsEmpty)
                        {
                            // No runner process
                        }
                        else if (runnerProcess.Succeeded)
                        {
                            AnalyzeCompilationLog(runnerProcess, runner.Index);
                            if (_options.R2RDumpPath != null)
                            {
                                r2rDumpExecutionsToRun.Add(new ProcessInfo(new R2RDumpProcessConstructor(runner, runnerProcess.Parameters.OutputFileName, naked: false)));
                                r2rDumpExecutionsToRun.Add(new ProcessInfo(new R2RDumpProcessConstructor(runner, runnerProcess.Parameters.OutputFileName, naked: true)));
                            }
                        }
                        else // runner process failed
                        {
                            _compilationFailureBuckets.AddCompilation(runnerProcess);
                            failedFiles.UnionWith(runnerProcess.Parameters.InputFileNames);
                            if (failedBuilders == null)
                            {
                                failedBuilders = runner.CompilerName;
                            }
                            else
                            {
                                failedBuilders += "; " + runner.CompilerName;
                            }
                        }
                    }
                    if (failedFiles.Count > 0)
                    {
                        foreach (string file in failedFiles)
                        {
                            failedCompilationsPerBuilder.Add(new KeyValuePair<string, string>(file, failedBuilders));
                        }
                        success = false;
                    }
                    else
                    {
                        successfulCompileCount++;
                    }
                }
            }

            ParallelRunner.Run(r2rDumpExecutionsToRun, _options.DegreeOfParallelism);

            foreach (ProcessInfo r2rDumpExecution in r2rDumpExecutionsToRun)
            {
                if (!r2rDumpExecution.Succeeded)
                {
                    string causeOfFailure;
                    if (r2rDumpExecution.TimedOut)
                    {
                        causeOfFailure = "timed out";
                    }
                    else if (r2rDumpExecution.ExitCode != 0)
                    {
                        causeOfFailure = $"invalid exit code {r2rDumpExecution.ExitCode}";
                    }
                    else
                    {
                        causeOfFailure = "Unknown cause of failure";
                    }

                    Console.Error.WriteLine("Error running R2R dump on {0}: {1}", string.Join(", ", r2rDumpExecution.Parameters.InputFileNames), causeOfFailure);
                    success = false;
                }
            }

            _compilationMilliseconds = stopwatch.ElapsedMilliseconds;

            return success;
        }

        public bool CompileFramework()
        {
            if (!_options.Framework)
            {
                return true;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();

            string coreRoot = _options.CoreRootDirectory.FullName;

            File.Delete(Path.Combine(coreRoot, FrameworkOutputFileName));

            string[] frameworkFolderFiles = Directory.GetFiles(coreRoot);

            IEnumerable<CompilerRunner> frameworkRunners = _options.CompilerRunners(isFramework: true, overrideOutputPath: _options.OutputDirectory.FullName);

            // Pre-populate the output folders with the input files so that we have backdrops
            // for failing compilations.
            foreach (CompilerRunner runner in frameworkRunners)
            {
                string outputPath = runner.GetOutputPath(coreRoot);
                outputPath.RecreateDirectory();
            }

            var compilationsToRun = new List<ProcessInfo>();
            var r2rDumpExecutionsToRun = new List<ProcessInfo>();
            var compilationsPerRunner = new List<KeyValuePair<string, ProcessInfo[]>>();
            var excludedAssemblies = new List<string>();

            if (_options.Composite)
            {
                var processes = new ProcessInfo[(int)CompilerIndex.Count];
                foreach (CompilerRunner runner in frameworkRunners)
                {
                    List<string> inputFrameworkDlls = new List<string>();
                    foreach (string frameworkDll in ComputeManagedAssemblies.GetManagedAssembliesInFolder(_options.CoreRootDirectory.FullName))
                    {
                        string simpleName = Path.GetFileNameWithoutExtension(frameworkDll);
                        if (FrameworkExclusion.Exclude(simpleName, runner.Index, out string reason))
                        {
                            _frameworkExclusions[simpleName] = reason;
                        }
                        else
                        {
                            inputFrameworkDlls.Add(frameworkDll);
                            compilationsPerRunner.Add(new KeyValuePair<string, ProcessInfo[]>(frameworkDll, processes));
                        }
                    }

                    if (inputFrameworkDlls.Count > 0)
                    {
                        string outputFileName = runner.GetOutputFileName(_options.CoreRootDirectory.FullName, FrameworkOutputFileName);
                        ProcessInfo compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, outputFileName, inputFrameworkDlls));
                        compilationsToRun.Add(compilationProcess);
                        processes[(int)runner.Index] = compilationProcess;
                        if (_options.R2RDumpPath != null)
                        {
                            r2rDumpExecutionsToRun.Add(new ProcessInfo(new R2RDumpProcessConstructor(runner, outputFileName, naked: false)));
                            r2rDumpExecutionsToRun.Add(new ProcessInfo(new R2RDumpProcessConstructor(runner, outputFileName, naked: true)));
                        }
                    }
                }
            }
            else
            {
                foreach (string frameworkDll in ComputeManagedAssemblies.GetManagedAssembliesInFolder(_options.CoreRootDirectory.FullName))
                {
                    string simpleName = Path.GetFileNameWithoutExtension(frameworkDll);

                    ProcessInfo[] processes = new ProcessInfo[(int)CompilerIndex.Count];
                    compilationsPerRunner.Add(new KeyValuePair<string, ProcessInfo[]>(frameworkDll, processes));
                    foreach (CompilerRunner runner in frameworkRunners)
                    {
                        if (FrameworkExclusion.Exclude(simpleName, runner.Index, out string reason))
                        {
                            _frameworkExclusions[simpleName] = reason;
                            continue;
                        }
                        string outputFileName = Path.Combine(runner.GetOutputPath(_options.CoreRootDirectory.FullName), Path.GetFileName(frameworkDll));
                        var compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, outputFileName, new string[] { frameworkDll }));
                        compilationsToRun.Add(compilationProcess);
                        processes[(int)runner.Index] = compilationProcess;

                        if (_options.R2RDumpPath != null)
                        {
                            r2rDumpExecutionsToRun.Add(new ProcessInfo(new R2RDumpProcessConstructor(runner, outputFileName, naked: false)));
                            r2rDumpExecutionsToRun.Add(new ProcessInfo(new R2RDumpProcessConstructor(runner, outputFileName, naked: true)));
                        }
                    }
                }
            }

            ParallelRunner.Run(compilationsToRun, _options.DegreeOfParallelism);

            var skipCopying = new HashSet<string>[(int)CompilerIndex.Count];
            foreach (CompilerRunner runner in frameworkRunners)
            {
                skipCopying[(int)runner.Index] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            var failedCompilationsPerBuilder = new int[(int)CompilerIndex.Count];
            int successfulCompileCount = 0;
            int failedCompileCount = 0;
            foreach (KeyValuePair<string, ProcessInfo[]> kvp in compilationsPerRunner)
            {
                bool anyCompilationsFailed = false;
                foreach (CompilerRunner runner in frameworkRunners)
                {
                    ProcessInfo compilationProcess = kvp.Value[(int)runner.Index];
                    if (compilationProcess == null)
                    {
                        // No compilation process (e.g. there's no real compilation phase for the JIT mode)
                    }
                    else if (compilationProcess.Succeeded)
                    {
                        skipCopying[(int)runner.Index].UnionWith(compilationProcess.Parameters.InputFileNames);
                        AnalyzeCompilationLog(compilationProcess, runner.Index);
                    }
                    else
                    {
                        anyCompilationsFailed = true;
                        failedCompilationsPerBuilder[(int)runner.Index]++;
                        _frameworkCompilationFailureBuckets.AddCompilation(compilationProcess);
                    }
                }
                if (anyCompilationsFailed)
                {
                    failedCompileCount++;
                }
                else
                {
                    successfulCompileCount++;
                }
            }

            foreach (CompilerRunner runner in frameworkRunners)
            {
                string outputPath = runner.GetOutputPath(coreRoot);
                foreach (string file in frameworkFolderFiles)
                {
                    if (!skipCopying[(int)runner.Index].Contains(file))
                    {
                        string targetFile = Path.Combine(outputPath, Path.GetFileName(file));
                        File.Copy(file, targetFile, overwrite: true);
                    }
                }
            }

            _frameworkCompilationMilliseconds = stopwatch.ElapsedMilliseconds;

            bool success = (failedCompileCount == 0);

            ParallelRunner.Run(r2rDumpExecutionsToRun, _options.DegreeOfParallelism);

            foreach (ProcessInfo r2rDumpExecution in r2rDumpExecutionsToRun)
            {
                if (!r2rDumpExecution.Succeeded)
                {
                    string causeOfFailure;
                    if (r2rDumpExecution.TimedOut)
                    {
                        causeOfFailure = "timed out";
                    }
                    else if (r2rDumpExecution.ExitCode != 0)
                    {
                        causeOfFailure = $"invalid exit code {r2rDumpExecution.ExitCode}";
                    }
                    else
                    {
                        causeOfFailure = "Unknown cause of failure";
                    }

                    Console.Error.WriteLine("Error running R2R dump on {0}: {1}", string.Join(", ", r2rDumpExecution.Parameters.InputFileNames), causeOfFailure);
                    success = false;
                }
            }

            return success;
        }

        private void AnalyzeCompilationLog(ProcessInfo compilationProcess, CompilerIndex runnerIndex)
        {
            Dictionary<string, byte> managedSequentialTarget;
            Dictionary<string, byte> requiresMarshalingTarget;

            switch (runnerIndex)
            {
                case CompilerIndex.CPAOT:
                    managedSequentialTarget = _cpaotManagedSequentialResults;
                    requiresMarshalingTarget = _cpaotRequiresMarshalingResults;
                    break;

                case CompilerIndex.Crossgen:
                    managedSequentialTarget = _crossgenManagedSequentialResults;
                    requiresMarshalingTarget = _crossgenRequiresMarshalingResults;
                    break;

                default:
                    return;
            }

            try
            {
                const string ManagedSequentialStartMarker = "[[[IsManagedSequential{";
                const string RequiresMarshalingStartMarker = "[[[MethodRequiresMarshaling{";

                foreach (string line in File.ReadAllLines(compilationProcess.Parameters.LogPath))
                {
                    AnalyzeMarker(line, ManagedSequentialStartMarker, managedSequentialTarget);
                    AnalyzeMarker(line, RequiresMarshalingStartMarker, requiresMarshalingTarget);
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error reading log file {0}: {1}", compilationProcess.Parameters.LogPath, ex.Message);
            }
        }

        private void AnalyzeMarker(string line, string marker, Dictionary<string, byte> target)
        {
            const string FalseEndMarker = "}=False]]]";
            const string TrueEndMarker = "}=True]]]";
            const string MultiEndMarker = "}=Multi]]]";

            int startIndex = line.IndexOf(marker);
            if (startIndex >= 0)
            {
                startIndex += marker.Length;
                int falseEndIndex = line.IndexOf(FalseEndMarker, startIndex);
                int trueEndIndex = falseEndIndex >= 0 ? falseEndIndex : line.IndexOf(TrueEndMarker, startIndex);
                int multiEndIndex = trueEndIndex >= 0 ? trueEndIndex : line.IndexOf(MultiEndMarker, startIndex);
                byte result;
                if (falseEndIndex >= 0)
                {
                    result = 0;
                }
                else if (trueEndIndex >= 0)
                {
                    result = 1;
                }
                else if (multiEndIndex >= 0)
                {
                    result = 2;
                }
                else
                {
                    throw new NotImplementedException();
                }
                string typeName = line.Substring(startIndex, multiEndIndex - startIndex);

                byte previousValue;
                if (target.TryGetValue(typeName, out previousValue) && previousValue != result)
                {
                    result = 2;
                }
                target[typeName] = result;
            }
        }

        public bool Execute()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            var executionsToRun = new List<ProcessInfo>();

            foreach (BuildFolder folder in FoldersToBuild)
            {
                AddBuildFolderExecutions(executionsToRun, folder, _options.Iterations);
            }

            ParallelRunner.Run(
                executionsToRun,
                degreeOfParallelism: _options.Sequential || _options.Iterations > 1 ? 1 : 0,
                measurePerf: false);

            int successfulExecuteCount = 0;

            bool success = true;
            foreach (BuildFolder folder in FoldersToBuild)
            {
                foreach (ProcessInfo[][] execution in folder.Executions)
                {
                    HashSet<string> failedFiles = new HashSet<string>();
                    string failedBuilders = null;
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        ProcessInfo[] runnerProcesses = execution[(int)runner.Index];
                        foreach (ProcessInfo runnerProcess in runnerProcesses)
                        {
                            if (runnerProcess != null && !runnerProcess.Succeeded)
                            {
                                _executionFailureBuckets.AddExecution(runnerProcess);

                                failedFiles.UnionWith(runnerProcess.Parameters.InputFileNames);
                                if (failedBuilders == null)
                                {
                                    failedBuilders = runner.CompilerName;
                                }
                                else
                                {
                                    failedBuilders += "; " + runner.CompilerName;
                                }
                            }
                        }
                    }
                    if (failedFiles.Count > 0)
                    {
                        success = false;
                    }
                    else
                    {
                        successfulExecuteCount++;
                    }
                }
            }

            _executionMilliseconds = stopwatch.ElapsedMilliseconds;

            return success;
        }

        public bool Build()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            bool success = _options.Exe || Compile();

            if (!_options.NoExe)
            {
                success = Execute() && success;
            }

            _buildMilliseconds = stopwatch.ElapsedMilliseconds;

            return success;
        }

        private void ResolveTestExclusions()
        {
            TestExclusionMap exclusions = TestExclusionMap.Create(_options);
            foreach (BuildFolder folder in _buildFolders)
            {
                if (exclusions.TryGetIssue(folder.InputFolder, out string issueID))
                {
                    folder.IssueID = issueID;
                    continue;
                }
            }
        }

        private void AddBuildFolderExecutions(List<ProcessInfo> executionsToRun, BuildFolder folder, int iterations)
        {
            foreach (ProcessInfo[][] execution in folder.Executions)
            {
                foreach (CompilerRunner runner in _compilerRunners)
                {
                    ProcessInfo[] executionProcesses = execution[(int)runner.Index];
                    if (executionProcesses != null)
                    {
                        bool compilationsSucceeded = true;
                        if (!_options.Exe)
                        {
                            foreach (ProcessInfo[] compilation in folder.Compilations)
                            {
                                ProcessInfo runnerCompilation = compilation[(int)runner.Index];
                                if (!runnerCompilation.IsEmpty && !runnerCompilation.Succeeded)
                                {
                                    compilationsSucceeded = false;
                                    break;
                                }
                            }
                        }
                        if (compilationsSucceeded)
                        {
                            executionsToRun.AddRange(executionProcesses);
                        }
                        else
                        {
                            // Forget the execution process when compilation failed
                            execution[(int)runner.Index] = null;
                        }
                    }
                }
            }
        }

        private void WriteTopRankingProcesses(StreamWriter logWriter, string metric, IEnumerable<ProcessInfo> processes)
        {
            const int TopAppCount = 10;

            IEnumerable<ProcessInfo> selection = processes.Where(process => !process.IsEmpty).OrderByDescending(process => process.DurationMilliseconds).Take(TopAppCount);
            int count = selection.Count();
            if (count == 0)
            {
                // No entries to log
                return;
            }

            logWriter.WriteLine();

            string headerLine = $"{count} top ranking {metric}";
            logWriter.WriteLine(headerLine);
            logWriter.WriteLine(new string('-', headerLine.Length));

            foreach (ProcessInfo processInfo in selection)
            {
                logWriter.WriteLine($"{processInfo.DurationMilliseconds,10} | {processInfo.Parameters.OutputFileName}");
            }
        }

        enum CompilationOutcome
        {
            PASS = 0,
            FAIL = 1,

            Count
        }

        private enum ExecutionOutcome
        {
            PASS = 0,
            EXIT_CODE = 1,
            CRASHED = 2,
            TIMED_OUT = 3,
            BUILD_FAILED = 4,

            Count
        }

        private CompilationOutcome GetCompilationOutcome(ProcessInfo compilation)
        {
            return compilation.Succeeded ? CompilationOutcome.PASS : CompilationOutcome.FAIL;
        }

        private ExecutionOutcome GetExecutionOutcome(ProcessInfo execution)
        {
            if (execution.TimedOut)
            {
                return ExecutionOutcome.TIMED_OUT;
            }
            if (execution.Crashed)
            {
                return ExecutionOutcome.CRASHED;
            }
            return (execution.Succeeded ? ExecutionOutcome.PASS : ExecutionOutcome.EXIT_CODE);
        }

        private void WriteBuildStatistics(StreamWriter logWriter)
        {
            // The Count'th element corresponds to totals over all compiler runners used in the run
            var compilationOutcomes = new int[(int)CompilationOutcome.Count, (int)CompilerIndex.Count + 1];
            var executionOutcomes = new int[(int)ExecutionOutcome.Count, (int)CompilerIndex.Count + 1];
            int totalCompilations = 0;
            int totalExecutions = 0;

            foreach (BuildFolder folder in FoldersToBuild)
            {
                var compilationFailedPerRunner = new bool[(int)CompilerIndex.Count];
                if (!_options.Exe)
                {
                    foreach (ProcessInfo[] compilation in folder.Compilations)
                    {
                        totalCompilations++;
                        bool anyCompilationFailed = false;
                        foreach (CompilerRunner runner in _compilerRunners)
                        {
                            if (!compilation[(int)runner.Index].IsEmpty)
                            {
                                CompilationOutcome outcome = GetCompilationOutcome(compilation[(int)runner.Index]);
                                compilationOutcomes[(int)outcome, (int)runner.Index]++;
                                if (outcome != CompilationOutcome.PASS)
                                {
                                    anyCompilationFailed = true;
                                    compilationFailedPerRunner[(int)runner.Index] = true;
                                }
                            }
                        }
                        if (anyCompilationFailed)
                        {
                            compilationOutcomes[(int)CompilationOutcome.FAIL, (int)CompilerIndex.Count]++;
                        }
                        else
                        {
                            compilationOutcomes[(int)CompilationOutcome.PASS, (int)CompilerIndex.Count]++;
                        }
                    }
                }

                if (!_options.NoExe)
                {
                    foreach (ProcessInfo[][] executions in folder.Executions)
                    {
                        totalExecutions++;
                        bool anyCompilationFailed = false;
                        int executionFailureOutcomeMask = 0;
                        foreach (CompilerRunner runner in _compilerRunners)
                        {
                            ProcessInfo[] execProcesses = executions[(int)runner.Index];
                            bool compilationFailed = compilationFailedPerRunner[(int)runner.Index];
                            anyCompilationFailed |= compilationFailed;
                            foreach (ProcessInfo execProcess in execProcesses)
                            {
                                bool executionFailed = !compilationFailed && (execProcess != null && !execProcess.Succeeded);
                                if (executionFailed)
                                {
                                    ExecutionOutcome outcome = GetExecutionOutcome(execProcess);
                                    executionOutcomes[(int)outcome, (int)runner.Index]++;
                                    executionFailureOutcomeMask |= 1 << (int)outcome;
                                }
                                else if (compilationFailed)
                                {
                                    executionOutcomes[(int)ExecutionOutcome.BUILD_FAILED, (int)runner.Index]++;
                                }
                                else
                                {
                                    executionOutcomes[(int)ExecutionOutcome.PASS, (int)runner.Index]++;
                                }
                                if (!anyCompilationFailed)
                                {
                                    if (executionFailureOutcomeMask != 0)
                                    {
                                        for (int outcomeIndex = 0; outcomeIndex < (int)ExecutionOutcome.Count; outcomeIndex++)
                                        {
                                            if ((executionFailureOutcomeMask & (1 << outcomeIndex)) != 0)
                                            {
                                                executionOutcomes[outcomeIndex, (int)CompilerIndex.Count]++;
                                            }
                                        }
                                    }
                                    else
                                    {
                                        executionOutcomes[(int)ExecutionOutcome.PASS, (int)CompilerIndex.Count]++;
                                    }
                                }
                                else
                                {
                                    executionOutcomes[(int)ExecutionOutcome.BUILD_FAILED, (int)CompilerIndex.Count]++;
                                }
                            }
                        }
                    }
                }
            }

            logWriter.WriteLine();
            logWriter.WriteLine($"Configuration:    {(_options.Release ? "Release" : "Debug")}");
            logWriter.WriteLine($"Framework:        {(_options.Framework ? "build native" : _options.UseFramework ? "prebuilt native" : "MSIL")}");
            logWriter.WriteLine($"Version bubble:   {(_options.LargeBubble ? "input + all reference assemblies" : "single assembly")}");
            logWriter.WriteLine($"Input folder:     {_options.InputDirectory?.FullName}");
            logWriter.WriteLine($"CORE_ROOT:        {_options.CoreRootDirectory?.FullName}");
            logWriter.WriteLine($"GC stress mode:   {(!string.IsNullOrEmpty(_options.GCStress) ? _options.GCStress : "None")}");
            logWriter.WriteLine($"Total folders:    {_buildFolders.Count()}");
            logWriter.WriteLine($"Blocked w/issues: {_buildFolders.Count(folder => folder.IsBlockedWithIssue)}");
            int foldersToBuild = FoldersToBuild.Count();
            logWriter.WriteLine($"Folders to build: {foldersToBuild}");
            if (!_options.Exe)
            {
                logWriter.WriteLine($"# compilations:   {totalCompilations}");
            }
            logWriter.WriteLine($"# executions:     {totalExecutions}");
            logWriter.WriteLine($"Total build time: {_buildMilliseconds} msecs");
            if (!_options.Exe)
            {
                logWriter.WriteLine($"Framework time:   {_frameworkCompilationMilliseconds} msecs");
                logWriter.WriteLine($"Compilation time: {_compilationMilliseconds} msecs");
            }
            logWriter.WriteLine($"Execution time:   {_executionMilliseconds} msecs");

            if (foldersToBuild != 0)
            {
                int lineSize = 10 * _compilerRunners.Count() + 13 + 8;
                var separator = new string('-', lineSize);

                if (!_options.Exe)
                {
                    logWriter.WriteLine();
                    logWriter.Write($"{totalCompilations,8} ILC |");
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        logWriter.Write($"{runner.CompilerName,8} |");
                    }
                    logWriter.WriteLine(" Overall");
                    logWriter.WriteLine(separator);
                    for (int outcomeIndex = 0; outcomeIndex < (int)CompilationOutcome.Count; outcomeIndex++)
                    {
                        logWriter.Write($"{((CompilationOutcome)outcomeIndex).ToString(),12} |");
                        foreach (CompilerRunner runner in _compilerRunners)
                        {
                            logWriter.Write($"{compilationOutcomes[outcomeIndex, (int)runner.Index],8} |");
                        }
                        logWriter.WriteLine($"{compilationOutcomes[outcomeIndex, (int)CompilerIndex.Count],8}");
                    }
                }

                if (!_options.NoExe)
                {
                    logWriter.WriteLine();
                    logWriter.Write($"{totalExecutions,8} EXE |");
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        logWriter.Write($"{runner.CompilerName,8} |");
                    }
                    logWriter.WriteLine(" Overall");
                    logWriter.WriteLine(separator);
                    for (int outcomeIndex = 0; outcomeIndex < (int)ExecutionOutcome.Count; outcomeIndex++)
                    {
                        logWriter.Write($"{((ExecutionOutcome)outcomeIndex).ToString(),12} |");
                        foreach (CompilerRunner runner in _compilerRunners)
                        {
                            logWriter.Write($"{executionOutcomes[outcomeIndex, (int)runner.Index],8} |");
                        }
                        logWriter.WriteLine($"{executionOutcomes[outcomeIndex, (int)CompilerIndex.Count],8}");
                    }
                }

                WritePerFolderStatistics(logWriter);

                WriteExecutableSizeStatistics(logWriter);

                WriteJittedMethodSummary(logWriter);

                if (!_options.Exe)
                {
                    WriteTopRankingProcesses(logWriter, "compilations by duration", EnumerateCompilations());
                }
                if (!_options.NoExe)
                {
                    WriteTopRankingProcesses(logWriter, "executions by duration", EnumerateExecutions());
                }
            }

            if (_options.Framework && !_options.Exe)
            {
                logWriter.WriteLine();
                logWriter.WriteLine("Framework compilation failures:");
                FrameworkCompilationFailureBuckets.WriteToStream(logWriter, detailed: false);

                logWriter.WriteLine();
                logWriter.WriteLine("Framework exclusions:");
                WriteFrameworkExclusions(logWriter);
            }

            if (foldersToBuild != 0)
            {
                if (!_options.Exe)
                {
                    logWriter.WriteLine();
                    logWriter.WriteLine("Compilation failures:");
                    CompilationFailureBuckets.WriteToStream(logWriter, detailed: false);
                }

                if (!_options.NoExe)
                {
                    logWriter.WriteLine();
                    logWriter.WriteLine("Execution failures:");
                    ExecutionFailureBuckets.WriteToStream(logWriter, detailed: false);
                }
            }

            if (_buildFolders.Count() != 0)
            {
                WriteFoldersBlockedWithIssues(logWriter);
            }
        }

        private void WriteFrameworkExclusions(StreamWriter logWriter)
        {
            int keyLength = _frameworkExclusions.Keys.Max(key => key.Length);
            const string SimpleNameTitle = "SIMPLE_NAME";
            keyLength = Math.Max(keyLength, SimpleNameTitle.Length);
            var title = new StringBuilder();
            title.Append(SimpleNameTitle);
            title.Append(' ', keyLength - SimpleNameTitle.Length);
            title.Append(" | REASON");
            logWriter.WriteLine(title.ToString());
            logWriter.WriteLine(new string('-', title.Length));
            foreach (KeyValuePair<string, string> exclusion in _frameworkExclusions.OrderBy(kvp => kvp.Key, StringComparer.OrdinalIgnoreCase))
            {
                var line = new StringBuilder();
                line.Append(exclusion.Key);
                line.Append(' ', keyLength - exclusion.Key.Length);
                line.Append(" | ");
                line.Append(exclusion.Value);
                logWriter.WriteLine(line.ToString());
            }
        }

        private void WritePerFolderStatistics(StreamWriter logWriter)
        {
            string baseFolder = _options.InputDirectory.FullName;
            int baseOffset = baseFolder.Length + (baseFolder.Length > 0 && baseFolder[baseFolder.Length - 1] == Path.DirectorySeparatorChar ? 0 : 1);
            var folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (BuildFolder folder in FoldersToBuild)
            {
                string relativeFolder = "";
                if (folder.InputFolder.Length > baseFolder.Length)
                {
                    relativeFolder = folder.InputFolder.Substring(baseOffset);
                }
                int endPos = relativeFolder.IndexOf(Path.DirectorySeparatorChar);
                if (endPos < 0)
                {
                    endPos = relativeFolder.Length;
                }
                folders.Add(relativeFolder.Substring(0, endPos));
            }
            if (folders.Count <= 1)
            {
                // Just one folder - no per folder statistics needed
                return;
            }

            var folderList = new List<string>(folders);
            folderList.Sort(StringComparer.OrdinalIgnoreCase);
            logWriter.WriteLine();
            logWriter.WriteLine("Folder statistics:");
            string title = "";
            if (!_options.Exe)
            {
                title += "#ILC | PASS | FAIL | ";
            }
            if (!_options.NoExe)
            {
                title += "#EXE | PASS | FAIL | ";
            }
            title += "PATH";
            logWriter.WriteLine(title);
            logWriter.WriteLine(new string('-', title.Length));

            foreach (string relativeFolder in folderList)
            {
                string folder = Path.Combine(baseFolder, relativeFolder);
                int ilcCount = 0;
                int exeCount = 0;
                int exeFail = 0;
                int ilcFail = 0;
                foreach (BuildFolder buildFolder in FoldersToBuild)
                {
                    string buildFolderPath = buildFolder.InputFolder;
                    if (buildFolderPath.Equals(folder, StringComparison.OrdinalIgnoreCase) ||
                        buildFolderPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase) &&
                            buildFolderPath[folder.Length] == Path.DirectorySeparatorChar)
                    {
                        if (!_options.Exe)
                        {
                            foreach (ProcessInfo[] compilation in buildFolder.Compilations)
                            {
                                bool anyIlcFail = false;
                                foreach (CompilerRunner runner in _compilerRunners)
                                {
                                    if (compilation[(int)runner.Index] != null && !compilation[(int)runner.Index].Succeeded)
                                    {
                                        anyIlcFail = true;
                                        break;
                                    }
                                }
                                ilcCount++;
                                if (anyIlcFail)
                                {
                                    ilcFail++;
                                }
                            }
                        }
                        if (!_options.NoExe)
                        {
                            foreach (ProcessInfo[][] executions in buildFolder.Executions)
                            {
                                bool anyExeFail = false;
                                foreach (CompilerRunner runner in _compilerRunners)
                                {
                                    foreach (ProcessInfo execution in executions[(int)runner.Index])
                                    {
                                        if (execution != null && !execution.Succeeded)
                                        {
                                            anyExeFail = true;
                                            break;
                                        }
                                    }
                                }
                                exeCount++;
                                if (anyExeFail)
                                {
                                    exeFail++;
                                }
                            }
                        }
                    }
                }
                if (!_options.Exe)
                {
                    logWriter.Write($"{ilcCount,4} | {(ilcCount - ilcFail),4} | {ilcFail,4} | ");

                }
                if (!_options.NoExe)
                {
                    logWriter.Write($"{exeCount,4} | {(exeCount - exeFail),4} | {exeFail,4} | ");

                }
                logWriter.WriteLine($"{relativeFolder}");
            }
        }

        class ExeSizeInfo
        {
            public readonly string CpaotPath;
            public readonly long CpaotSize;
            public readonly string CrossgenPath;
            public readonly long CrossgenSize;

            public ExeSizeInfo(string cpaotPath, long cpaotSize, string crossgenPath, long crossgenSize)
            {
                CpaotPath = cpaotPath;
                CpaotSize = cpaotSize;
                CrossgenPath = crossgenPath;
                CrossgenSize = crossgenSize;
            }
        }

        private void WriteExecutableSizeStatistics(StreamWriter logWriter)
        {
            var sizeStats = new List<ExeSizeInfo>();
            var libraryHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (BuildFolder folder in FoldersToBuild)
            {
                foreach (ProcessInfo[] compilation in folder.Compilations)
                {
                    ProcessInfo crossgenCompilation = compilation[(int)CompilerIndex.Crossgen];
                    ProcessInfo cpaotCompilation = compilation[(int)CompilerIndex.CPAOT];
                    if ((crossgenCompilation?.Succeeded ?? false) &&
                        (cpaotCompilation?.Succeeded ?? false))
                    {
                        long cpaotSize;
                        try
                        {
                            cpaotSize = new FileInfo(cpaotCompilation.Parameters.OutputFileName).Length;
                        }
                        catch (Exception)
                        {
                            Console.Error.WriteLine("Cannot find CPAOT output file '{0}', ignoring in size stats", cpaotCompilation.Parameters.OutputFileName);
                            continue;
                        }

                        long crossgenSize;
                        try
                        {
                            crossgenSize = new FileInfo(crossgenCompilation.Parameters.OutputFileName).Length;
                        }
                        catch (Exception)
                        {
                            Console.Error.WriteLine("Cannot find Crossgen output file '{0}', ignoring in size stats", crossgenCompilation.Parameters.OutputFileName);
                            continue;
                        }

                        string ext = Path.GetExtension(cpaotCompilation.Parameters.OutputFileName).ToLower();
                        if (ext == ".dll" || ext == ".so")
                        {
                            string hash = $"{Path.GetFileName(cpaotCompilation.Parameters.OutputFileName)}#{cpaotSize}#{crossgenSize}";
                            if (!libraryHashes.Add(hash))
                            {
                                // We ignore libraries with the same "simple name" if it has the same compiled size as many tests
                                // use support libraries that get separately compiled into their respective folders but semantically
                                // are "the same thing" so it doesn't make too much sense to report them multiple times.
                                continue;
                            }
                        }

                        sizeStats.Add(new ExeSizeInfo(
                            cpaotPath: cpaotCompilation.Parameters.OutputFileName,
                            cpaotSize: cpaotSize,
                            crossgenPath: crossgenCompilation.Parameters.OutputFileName,
                            crossgenSize: crossgenSize));

                    }
                }
            }

            if (sizeStats.Count == 0)
            {
                return;
            }

            long totalCpaotSize = sizeStats.Sum((stat) => stat.CpaotSize);
            long totalCrossgenSize = sizeStats.Sum((stat) => stat.CrossgenSize);

            const double MegaByte = 1024 * 1024;
            double KiloCount = 1024 * sizeStats.Count;

            logWriter.WriteLine();
            logWriter.WriteLine("Executable size statistics:");
            logWriter.WriteLine("Total CPAOT size:    {0:F3} MB ({1:F3} KB per app on average)", totalCpaotSize / MegaByte, totalCpaotSize / KiloCount);
            logWriter.WriteLine("Total Crossgen size: {0:F3} MB ({1:F3} KB per app on average)", totalCrossgenSize / MegaByte, totalCrossgenSize / KiloCount);

            long deltaSize = totalCpaotSize - totalCrossgenSize;
            logWriter.WriteLine("CPAOT - Crossgen:    {0:F3} MB ({1:F3} KB per app on average)", deltaSize / MegaByte, deltaSize / KiloCount);

            double percentageSizeRatio = totalCpaotSize * 100.0 / Math.Max(totalCrossgenSize, 1);
            logWriter.WriteLine("CPAOT / Crossgen:    {0:F3}%", percentageSizeRatio);

            sizeStats.Sort((a, b) => (b.CpaotSize - b.CrossgenSize).CompareTo(a.CpaotSize - a.CrossgenSize));

            const int TopExeCount = 10;

            int topCount;
            int bottomCount;

            if (sizeStats.Count <= 2 * TopExeCount)
            {
                topCount = sizeStats.Count;
                bottomCount = 0;
            }
            else
            {
                topCount = TopExeCount;
                bottomCount = TopExeCount;
            }

            logWriter.WriteLine();
            logWriter.WriteLine("CPAOT size |   Crossgen | CPAOT - CG | Highest exe size deltas");
            logWriter.WriteLine("--------------------------------------------------------------");
            foreach (ExeSizeInfo exeSize in sizeStats.Take(topCount))
            {
                logWriter.WriteLine(
                    "{0,10} | {1,10} | {2,10} | {3}",
                    exeSize.CpaotSize,
                    exeSize.CrossgenSize,
                    exeSize.CpaotSize - exeSize.CrossgenSize,
                    exeSize.CpaotPath);
            }

            if (bottomCount > 0)
            {
                logWriter.WriteLine();
                logWriter.WriteLine("CPAOT size |   Crossgen | CPAOT - CG | Lowest exe size deltas");
                logWriter.WriteLine("-------------------------------------------------------------");
                foreach (ExeSizeInfo exeSize in sizeStats.TakeLast(bottomCount))
                {
                    logWriter.WriteLine(
                        "{0,10} | {1,10} | {2,10} | {3}",
                        exeSize.CpaotSize,
                        exeSize.CrossgenSize,
                        exeSize.CpaotSize - exeSize.CrossgenSize,
                        exeSize.CpaotPath);
                }
            }

            sizeStats.Sort((a, b) => (b.CpaotSize * a.CrossgenSize).CompareTo(a.CpaotSize * b.CrossgenSize));

            logWriter.WriteLine();
            logWriter.WriteLine("CPAOT size |   Crossgen | CPAOT/CG % | Highest exe size ratios");
            logWriter.WriteLine("--------------------------------------------------------------");
            foreach (ExeSizeInfo exeSize in sizeStats.Take(topCount))
            {
                logWriter.WriteLine(
                    "{0,10} | {1,10} | {2,10:F3} | {3}",
                    exeSize.CpaotSize,
                    exeSize.CrossgenSize,
                    exeSize.CpaotSize * 100.0 / exeSize.CrossgenSize,
                    exeSize.CpaotPath);
            }

            if (bottomCount > 0)
            {
                logWriter.WriteLine();
                logWriter.WriteLine("CPAOT size |   Crossgen | CPAOT/CG % | Lowest exe size ratios");
                logWriter.WriteLine("-------------------------------------------------------------");
                foreach (ExeSizeInfo exeSize in sizeStats.TakeLast(bottomCount))
                {
                    logWriter.WriteLine(
                        "{0,10} | {1,10} | {2,10:F6} | {3}",
                        exeSize.CpaotSize,
                        exeSize.CrossgenSize,
                        exeSize.CpaotSize * 100.0 / exeSize.CrossgenSize,
                        exeSize.CpaotPath);
                }
            }
        }

        private IEnumerable<ProcessInfo> EnumerateCompilations()
        {
            foreach (BuildFolder folder in FoldersToBuild)
            {
                foreach (ProcessInfo[] compilation in folder.Compilations)
                {
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        ProcessInfo compilationProcess = compilation[(int)runner.Index];
                        if (compilationProcess != null)
                        {
                            yield return compilationProcess;
                        }
                    }
                }
            }
        }

        private IEnumerable<ProcessInfo> EnumerateExecutions()
        {
            foreach (BuildFolder folder in FoldersToBuild)
            {
                foreach (ProcessInfo[][] executions in folder.Executions)
                {
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        foreach (ProcessInfo execution in executions[(int)runner.Index])
                        {
                            ProcessInfo executionProcess = execution;
                            if (executionProcess != null)
                            {
                                yield return executionProcess;
                            }
                        }
                    }
                }
            }
        }

        public void WriteBuildLog(string buildLogPath)
        {
            using (var buildLogWriter = new StreamWriter(buildLogPath))
            {
                WriteBuildStatistics(buildLogWriter);
            }
        }

        public void WriteCombinedLog(string outputFile)
        {
            using (var combinedLog = new StreamWriter(outputFile))
            {
                var perRunnerLog = new StreamWriter[(int)CompilerIndex.Count];
                foreach (CompilerRunner runner in _compilerRunners)
                {
                    string runnerLogPath = Path.ChangeExtension(outputFile, "-" + runner.CompilerName + ".log");
                    perRunnerLog[(int)runner.Index] = new StreamWriter(runnerLogPath);
                }

                foreach (BuildFolder folder in FoldersToBuild)
                {
                    var compilationErrorPerRunner = new bool[(int)CompilerIndex.Count];
                    if (!_options.Exe)
                    {
                        foreach (ProcessInfo[] compilation in folder.Compilations)
                        {
                            foreach (CompilerRunner runner in _compilerRunners)
                            {
                                ProcessInfo compilationProcess = compilation[(int)runner.Index];
                                if (compilationProcess != null && !compilationProcess.IsEmpty)
                                {
                                    string log = $"\nCOMPILE {runner.CompilerName}:{compilationProcess.Parameters.OutputFileName}";
                                    StreamWriter runnerLog = perRunnerLog[(int)runner.Index];
                                    runnerLog.WriteLine(log);
                                    combinedLog.WriteLine(log);
                                    try
                                    {
                                        using (Stream input = new FileStream(compilationProcess.Parameters.LogPath, FileMode.Open, FileAccess.Read))
                                        {
                                            input.CopyTo(combinedLog.BaseStream);
                                            input.Seek(0, SeekOrigin.Begin);
                                            input.CopyTo(runnerLog.BaseStream);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        combinedLog.WriteLine(" -> " + ex.Message);
                                        runnerLog.WriteLine(" -> " + ex.Message);
                                    }

                                    if (!compilationProcess.Succeeded)
                                    {
                                        compilationErrorPerRunner[(int)runner.Index] = true;
                                    }
                                }
                            }
                        }
                    }
                    foreach (ProcessInfo[][] executions in folder.Executions)
                    {
                        foreach (CompilerRunner runner in _compilerRunners)
                        {
                            if (!compilationErrorPerRunner[(int)runner.Index])
                            {
                                StreamWriter runnerLog = perRunnerLog[(int)runner.Index];
                                foreach (ProcessInfo executionProcess in executions[(int)runner.Index])
                                {
                                    if (executionProcess != null)
                                    {
                                        string header = $"\nEXECUTE {runner.CompilerName}:{executionProcess.Parameters.OutputFileName}";
                                        combinedLog.WriteLine(header);
                                        runnerLog.WriteLine(header);
                                        try
                                        {
                                            using (Stream input = new FileStream(executionProcess.Parameters.LogPath, FileMode.Open, FileAccess.Read))
                                            {
                                                input.CopyTo(combinedLog.BaseStream);
                                                input.Seek(0, SeekOrigin.Begin);
                                                input.CopyTo(runnerLog.BaseStream);
                                            }
                                        }
                                        catch (Exception ex)
                                        {
                                            combinedLog.WriteLine(" -> " + ex.Message);
                                            runnerLog.WriteLine(" -> " + ex.Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                foreach (CompilerRunner runner in _compilerRunners)
                {
                    perRunnerLog[(int)runner.Index].Dispose();
                }
            }
        }

        private void WriteFoldersBlockedWithIssues(StreamWriter logWriter)
        {
            IEnumerable<BuildFolder> blockedFolders = _buildFolders.Where(folder => folder.IsBlockedWithIssue);

            int blockedCount = blockedFolders.Count();

            logWriter.WriteLine();
            logWriter.WriteLine($"Folders blocked with issues ({blockedCount} total):");
            logWriter.WriteLine("ISSUE | TEST");
            logWriter.WriteLine("------------");
            foreach (BuildFolder folder in blockedFolders)
            {
                logWriter.WriteLine($"{folder.IssueID,5} | {folder.InputFolder}");
            }
        }

        public void WriteLogs()
        {
            string timestamp = DateTime.Now.ToString("MMdd-HHmm");

            string suffix = (_options.Release ? "ret-" : "chk-") + timestamp + ".log";

            string buildLogPath = Path.Combine(_options.OutputDirectory.FullName, "build-" + suffix);
            WriteBuildLog(buildLogPath);

            string combinedSetLogPath = Path.Combine(_options.OutputDirectory.FullName, "combined-" + suffix);
            WriteCombinedLog(combinedSetLogPath);

            if (_options.Framework)
            {
                string frameworkExclusionsFile = Path.Combine(_options.OutputDirectory.FullName, "framework-exclusions-" + suffix);
                using (var writer = new StreamWriter(frameworkExclusionsFile))
                {
                    WriteFrameworkExclusions(writer);
                }
            }

            if (!_options.Exe && _options.Framework)
            {
                string frameworkBucketsFile = Path.Combine(_options.OutputDirectory.FullName, "framework-buckets-" + suffix);
                FrameworkCompilationFailureBuckets.WriteToFile(frameworkBucketsFile, detailed: true);
            }

            if (!_options.Exe)
            {
                string compilationBucketsFile = Path.Combine(_options.OutputDirectory.FullName, "compilation-buckets-" + suffix);
                CompilationFailureBuckets.WriteToFile(compilationBucketsFile, detailed: true);
            }

            if (!_options.NoExe)
            {
                string executionBucketsFile = Path.Combine(_options.OutputDirectory.FullName, "execution-buckets-" + suffix);
                ExecutionFailureBuckets.WriteToFile(executionBucketsFile, detailed: true);
            }

            if (!_options.Exe)
            {
                string compilationPassedFile = Path.Combine(_options.OutputDirectory.FullName, "compilation-passed-" + suffix);
                WriteFileListPerCompilationOutcome(compilationPassedFile, CompilationOutcome.PASS);

                string compilationFailedFile = Path.Combine(_options.OutputDirectory.FullName, "compilation-failed-" + suffix);
                WriteFileListPerCompilationOutcome(compilationFailedFile, CompilationOutcome.FAIL);
            }

            if (!_options.NoExe)
            {
                string executionPassedFile = Path.Combine(_options.OutputDirectory.FullName, "execution-passed-" + suffix);
                WriteFileListPerExecutionOutcome(executionPassedFile, ExecutionOutcome.PASS);

                string executionTimedOutFile = Path.Combine(_options.OutputDirectory.FullName, "execution-timed-out-" + suffix);
                WriteFileListPerExecutionOutcome(executionTimedOutFile, ExecutionOutcome.TIMED_OUT);

                string executionCrashedFile = Path.Combine(_options.OutputDirectory.FullName, "execution-crashed-" + suffix);
                WriteFileListPerExecutionOutcome(executionCrashedFile, ExecutionOutcome.CRASHED);

                string executionExitCodeFile = Path.Combine(_options.OutputDirectory.FullName, "execution-exit-code-" + suffix);
                WriteFileListPerExecutionOutcome(executionExitCodeFile, ExecutionOutcome.EXIT_CODE);
            }

            string cpaotManagedSequentialFile = Path.Combine(_options.OutputDirectory.FullName, "managed-sequential-cpaot-" + suffix);
            WriterMarkerLog(cpaotManagedSequentialFile, _cpaotManagedSequentialResults);

            string cpaotRequiresMarshalingFile = Path.Combine(_options.OutputDirectory.FullName, "requires-marshaling-cpaot-" + suffix);
            WriterMarkerLog(cpaotRequiresMarshalingFile, _cpaotRequiresMarshalingResults);
        }

        private static void WriterMarkerLog(string fileName, Dictionary<string, byte> markerResults)
        {
            if (markerResults.Count == 0)
            {
                // Don't emit marker logs when the instrumentation is off
                return;
            }

            using (var logWriter = new StreamWriter(fileName))
            {
                foreach (KeyValuePair<string, byte> kvp in markerResults.OrderBy((kvp) => kvp.Key))
                {
                    logWriter.WriteLine("{0}:{1}", kvp.Value, kvp.Key);
                }
            }
        }

        private static void WriterMarkerDiff(string fileName, Dictionary<string, byte> cpaot, Dictionary<string, byte> crossgen)
        {
            if (cpaot.Count == 0 && crossgen.Count == 0)
            {
                // Don't emit empty marker diffs just polluting the output folder
                return;
            }

            using (var logWriter = new StreamWriter(fileName))
            {
                int cpaotCount = cpaot.Count();
                logWriter.WriteLine("Objects queried by CPAOT:        {0}", cpaotCount);
                logWriter.WriteLine("CPAOT conflicting results:       {0}", cpaot.Count(kvp => kvp.Value == 2));
                int crossgenCount = crossgen.Count();
                logWriter.WriteLine("Objects queried by Crossgen:     {0}", crossgenCount);
                logWriter.WriteLine("Crossgen conflicting results:    {0}", crossgen.Count(kvp => kvp.Value == 2));
                int matchCount = cpaot.Count(kvp => crossgen.ContainsKey(kvp.Key) && crossgen[kvp.Key] == kvp.Value);
                int bothCount = cpaot.Count(kvp => crossgen.ContainsKey(kvp.Key));
                logWriter.WriteLine("Objects queried by both:         {0}", bothCount);
                logWriter.WriteLine("Matching results:                {0} ({1:F3}%)", matchCount, matchCount * 100.0 / Math.Max(bothCount, 1));
                logWriter.WriteLine("Mismatched results:              {0}",
                    cpaot.Count(kvp => crossgen.ContainsKey(kvp.Key) && crossgen[kvp.Key] != kvp.Value));
                logWriter.WriteLine("Objects not queried by Crossgen: {0}", cpaot.Count(kvp => !crossgen.ContainsKey(kvp.Key)));
                logWriter.WriteLine("Objects not queried by CPAOT:    {0}", crossgen.Count(kvp => !cpaot.ContainsKey(kvp.Key)));
                logWriter.WriteLine();

                WriterMarkerDiffSection(
                    logWriter,
                    "CPAOT = TRUE / CROSSGEN = FALSE",
                    cpaot
                        .Where(kvp => kvp.Value == 1 && crossgen.ContainsKey(kvp.Key) && crossgen[kvp.Key] == 0)
                        .Select(kvp => kvp.Key));

                WriterMarkerDiffSection(
                    logWriter,
                    "CPAOT = FALSE / CROSSGEN = TRUE",
                    cpaot
                        .Where(kvp => kvp.Value == 0 && crossgen.ContainsKey(kvp.Key) && crossgen[kvp.Key] == 1)
                        .Select(kvp => kvp.Key));

                WriterMarkerDiffSection(
                    logWriter,
                    "CROSSGEN - NO RESULT",
                    cpaot
                        .Where(kvp => !crossgen.ContainsKey(kvp.Key))
                        .Select(kvp => (kvp.Value.ToString() + ":" + kvp.Key)));

                WriterMarkerDiffSection(
                    logWriter,
                    "CPAOT - NO RESULT",
                    crossgen
                        .Where(kvp => !cpaot.ContainsKey(kvp.Key))
                        .Select(kvp => (kvp.Value.ToString() + ":" + kvp.Key)));
            }
        }

        private static void WriterMarkerDiffSection(StreamWriter logWriter, string title, IEnumerable<string> items)
        {
            bool first = true;
            foreach (string item in items)
            {
                if (first)
                {
                    logWriter.WriteLine();
                    logWriter.WriteLine(title);
                    logWriter.WriteLine(new string('-', title.Length));
                    first = false;
                }
                logWriter.WriteLine(item);
            }
        }

        private void WriteFileListPerCompilationOutcome(string outputFileName, CompilationOutcome outcome)
        {
            var filteredTestList = new List<string>();
            foreach (BuildFolder folder in _buildFolders)
            {
                foreach (ProcessInfo[] compilation in folder.Compilations)
                {
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        ProcessInfo compilationPerRunner = compilation[(int)runner.Index];
                        if (compilationPerRunner != null &&
                            GetCompilationOutcome(compilationPerRunner) == outcome &&
                            compilationPerRunner.Parameters != null)
                        {
                            filteredTestList.Add(compilationPerRunner.Parameters.OutputFileName);
                        }
                    }
                }
            }

            filteredTestList.Sort(StringComparer.OrdinalIgnoreCase);
            File.WriteAllLines(outputFileName, filteredTestList);
        }

        private void WriteFileListPerExecutionOutcome(string outputFileName, ExecutionOutcome outcome)
        {
            var filteredTestList = new List<string>();
            foreach (BuildFolder folder in _buildFolders)
            {
                foreach (ProcessInfo[][] executions in folder.Executions)
                {
                    foreach (CompilerRunner runner in _compilerRunners)
                    {
                        foreach (ProcessInfo executionPerRunner in executions[(int)runner.Index])
                        {
                            if (executionPerRunner != null &&
                                GetExecutionOutcome(executionPerRunner) == outcome &&
                                executionPerRunner.Parameters != null)
                            {
                                filteredTestList.Add(executionPerRunner.Parameters.OutputFileName);
                            }
                        }
                    }
                }
            }

            filteredTestList.Sort(StringComparer.OrdinalIgnoreCase);
            File.WriteAllLines(outputFileName, filteredTestList);
        }

        public IEnumerable<BuildFolder> FoldersToBuild => _buildFolders.Where(folder => !folder.IsBlockedWithIssue);

        public Buckets FrameworkCompilationFailureBuckets => _frameworkCompilationFailureBuckets;

        public Buckets CompilationFailureBuckets => _compilationFailureBuckets;

        public Buckets ExecutionFailureBuckets => _executionFailureBuckets;
    }
}
