// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace ReadyToRun.SuperIlc
{
    public class BuildFolder
    {
        private List<string> _compilationInputFiles;

        private List<string> _mainExecutables;

        private List<string> _executionScripts;

        private readonly List<ProcessInfo[]> _compilations;

        private string _inputFolder;

        private string _outputFolder;

        private readonly List<ProcessInfo[]> _executions;

        public string IssueID;

        public BuildFolder(
            List<string> compilationInputFiles,
            List<string> mainExecutables,
            List<string> executionScripts,
            IEnumerable<CompilerRunner> compilerRunners,
            string inputFolder,
            string outputFolder,
            BuildOptions options)
        {
            _compilationInputFiles = compilationInputFiles;
            _mainExecutables = mainExecutables;
            _executionScripts = executionScripts;
            _inputFolder = inputFolder;
            _outputFolder = outputFolder;

            _compilations = new List<ProcessInfo[]>();
            _executions = new List<ProcessInfo[]>();

            foreach (string file in _compilationInputFiles)
            {
                ProcessInfo[] fileCompilations = new ProcessInfo[(int)CompilerIndex.Count];
                foreach (CompilerRunner runner in compilerRunners)
                {
                    ProcessInfo compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, _outputFolder, file));
                    fileCompilations[(int)runner.Index] = compilationProcess;
                }
                _compilations.Add(fileCompilations);
            }

            if (!options.NoExe)
            {
                HashSet<string> scriptedExecutables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (string script in _executionScripts ?? Enumerable.Empty<string>())
                {
                    ProcessInfo[] scriptExecutions = new ProcessInfo[(int)CompilerIndex.Count];
                    _executions.Add(scriptExecutions);
                    scriptedExecutables.Add(Path.ChangeExtension(script, ".exe"));

                    foreach (CompilerRunner runner in compilerRunners)
                    {
                        HashSet<string> modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        modules.Add(runner.GetOutputFileName(_outputFolder, script));
                        modules.UnionWith(_compilationInputFiles);
                        modules.UnionWith(_compilationInputFiles.Select(file => runner.GetOutputFileName(_outputFolder, file)));
                        folders.Add(Path.GetDirectoryName(script));
                        folders.UnionWith(runner.ReferenceFolders);

                        scriptExecutions[(int)runner.Index] = new ProcessInfo(new ScriptExecutionProcessConstructor(runner, _outputFolder, script, modules, folders));
                    }
                }

                if (options.CoreRootDirectory != null)
                {
                    foreach (string mainExe in _mainExecutables ?? Enumerable.Empty<string>())
                    {
                        if (scriptedExecutables.Contains(mainExe))
                        {
                            // Skip direct exe launch assuming it was run by the corresponding cmd script
                            continue;
                        }

                        ProcessInfo[] appExecutions = new ProcessInfo[(int)CompilerIndex.Count];
                        _executions.Add(appExecutions);
                        foreach (CompilerRunner runner in compilerRunners)
                        {
                            HashSet<string> modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                            HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                            modules.Add(mainExe);
                            modules.Add(runner.GetOutputFileName(_outputFolder, mainExe));
                            modules.UnionWith(_compilationInputFiles);
                            modules.UnionWith(_compilationInputFiles.Select(file => runner.GetOutputFileName(_outputFolder, file)));
                            folders.Add(Path.GetDirectoryName(mainExe));
                            folders.UnionWith(runner.ReferenceFolders);

                            appExecutions[(int)runner.Index] = new ProcessInfo(new AppExecutionProcessConstructor(runner, _outputFolder, mainExe, modules, folders));
                        }
                    }
                }
            }
        }

        public static BuildFolder FromDirectory(string inputDirectory, IEnumerable<CompilerRunner> compilerRunners, string outputRoot, BuildOptions options)
        {
            List<string> compilationInputFiles = new List<string>();
            List<string> passThroughFiles = new List<string>();
            List<string> mainExecutables = new List<string>();
            List<string> executionScripts = new List<string>();

            string scriptExtension = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh");

            // Copy unmanaged files (runtime, native dependencies, resources, etc)
            foreach (string file in Directory.EnumerateFiles(inputDirectory, options.InputFileSearchString ?? "*"))
            {
                bool isManagedAssembly = ComputeManagedAssemblies.IsManaged(file);
                if (isManagedAssembly)
                {
                    compilationInputFiles.Add(file);
                }
                else if ((Path.GetExtension(file) != ".pdb") && (Path.GetExtension(file) != ".ilk")) // exclude .pdb and .ilk files that are large and not needed in the target folder
                {
                    passThroughFiles.Add(file);
                }
                string ext = Path.GetExtension(file);
                if (ext.Equals(".exe", StringComparison.OrdinalIgnoreCase))
                {
                    mainExecutables.Add(file);
                }
                else if (ext.Equals(scriptExtension, StringComparison.OrdinalIgnoreCase))
                {
                    executionScripts.Add(file);
                }
            }

            if (compilationInputFiles.Count == 0)
            {
                return null;
            }

            foreach (CompilerRunner runner in compilerRunners)
            {
                string runnerOutputPath = runner.GetOutputPath(outputRoot);
                if (!options.Exe)
                {
                    runnerOutputPath.RecreateDirectory();
                    foreach (string file in passThroughFiles)
                    {
                        File.Copy(file, Path.Combine(runnerOutputPath, Path.GetFileName(file)));
                    }
                }
            }

            return new BuildFolder(compilationInputFiles, mainExecutables, executionScripts, compilerRunners, inputDirectory, outputRoot, options);
        }

        public void AddModuleToJittedMethodsMapping(Dictionary<string, HashSet<string>> moduleToJittedMethods, int executionIndex, CompilerIndex compilerIndex)
        {
            ProcessInfo executionProcess = _executions[executionIndex][(int)compilerIndex];
            if (executionProcess != null && executionProcess.JittedMethods != null)
            {
                foreach (KeyValuePair<string, HashSet<string>> moduleMethodKvp in executionProcess.JittedMethods)
                {
                    HashSet<string> jittedMethodsPerModule;
                    if (!moduleToJittedMethods.TryGetValue(moduleMethodKvp.Key, out jittedMethodsPerModule))
                    {
                        jittedMethodsPerModule = new HashSet<string>();
                        moduleToJittedMethods.Add(moduleMethodKvp.Key, jittedMethodsPerModule);
                    }
                    jittedMethodsPerModule.UnionWith(moduleMethodKvp.Value);
                }
            }
        }

        public static void WriteJitStatistics(TextWriter writer, Dictionary<string, HashSet<string>>[] perCompilerStatistics, IEnumerable<CompilerRunner> compilerRunners)
        {
            Dictionary<string, int> moduleNameUnion = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (CompilerRunner compilerRunner in compilerRunners)
            {
                foreach (KeyValuePair<string, HashSet<string>> kvp in perCompilerStatistics[(int)compilerRunner.Index])
                {
                    int methodCount;
                    moduleNameUnion.TryGetValue(kvp.Key, out methodCount);
                    moduleNameUnion[kvp.Key] = Math.Max(methodCount, kvp.Value.Count);
                }
            }

            if (moduleNameUnion.Count == 0)
            {
                // No JIT statistics available
                return;
            }

            writer.WriteLine();
            writer.WriteLine("Jitted method statistics:");

            foreach (CompilerRunner compilerRunner in compilerRunners)
            {
                writer.Write($"{compilerRunner.Index.ToString(),9} |");
            }
            writer.WriteLine(" Assembly Name");
            writer.WriteLine(new string('-', 11 * compilerRunners.Count() + 14));
            foreach (string moduleName in moduleNameUnion.OrderByDescending(kvp => kvp.Value).Select(kvp => kvp.Key))
            {
                foreach (CompilerRunner compilerRunner in compilerRunners)
                {
                    HashSet<string> jittedMethodsPerModule;
                    perCompilerStatistics[(int)compilerRunner.Index].TryGetValue(moduleName, out jittedMethodsPerModule);
                    writer.Write(string.Format("{0,9} |", jittedMethodsPerModule != null ? jittedMethodsPerModule.Count.ToString() : ""));
                }
                writer.Write(' ');
                writer.WriteLine(moduleName);
            }
        }

        public void WriteJitStatistics(Dictionary<string, HashSet<string>>[] perCompilerStatistics, IEnumerable<CompilerRunner> compilerRunners)
        {
            for (int exeIndex = 0; exeIndex < _mainExecutables.Count; exeIndex++)
            {
                string jitStatisticsFile = Path.ChangeExtension(_mainExecutables[exeIndex], ".jit-statistics");
                using (StreamWriter streamWriter = new StreamWriter(jitStatisticsFile))
                {
                    WriteJitStatistics(streamWriter, perCompilerStatistics, compilerRunners);
                }
            }
        }

        public bool IsBlockedWithIssue => IssueID != null;

        public string InputFolder => _inputFolder;

        public string OutputFolder => _outputFolder;

        public IList<string> MainExecutables => _mainExecutables;

        public IList<String> ExecutionScripts => _executionScripts;

        public IList<ProcessInfo[]> Compilations => _compilations;

        public IList<ProcessInfo[]> Executions => _executions;
    }
}
