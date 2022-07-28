// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace R2RTest
{
    public class BuildFolder
    {
        private static string[] s_runtimeExecutables =
        {
            "corerun"
        };

        private static string[] s_runtimeLibraries =
        {
            "coreclr",
            "clrjit",
            "mscordaccore",
            "mscordbi",
        };

        private static string[] s_runtimeWindowsOnlyLibraries =
        {
            "mscorrc",
        };

        private List<string> _compilationInputFiles;

        private List<string> _mainExecutables;

        private List<string> _executionScripts;

        private readonly List<ProcessInfo[]> _compilations;

        private string _inputFolder;

        private string _outputFolder;

        private readonly List<ProcessInfo[][]> _executions;

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
            _executions = new List<ProcessInfo[][]>();

            if (options.Composite)
            {
                ProcessInfo[] fileCompilations = new ProcessInfo[(int)CompilerIndex.Count];
                foreach (CompilerRunner runner in compilerRunners)
                {
                    string outputFile = runner.GetOutputFileName(_outputFolder, "composite-r2r.dll");
                    ProcessInfo compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, outputFile, _compilationInputFiles));
                    fileCompilations[(int)runner.Index] = compilationProcess;
                }
                _compilations.Add(fileCompilations);
            }
            else
            {
                foreach (string file in _compilationInputFiles)
                {
                    ProcessInfo[] fileCompilations = new ProcessInfo[(int)CompilerIndex.Count];
                    foreach (CompilerRunner runner in compilerRunners)
                    {
                        string outputFile = runner.GetOutputFileName(_outputFolder, file);
                        ProcessInfo compilationProcess = new ProcessInfo(new CompilationProcessConstructor(runner, outputFile, new string[] { file }));
                        fileCompilations[(int)runner.Index] = compilationProcess;
                    }
                    _compilations.Add(fileCompilations);
                }
            }

            if (!options.NoExe)
            {
                foreach (string script in _executionScripts ?? Enumerable.Empty<string>())
                {
                    ProcessInfo[][] scriptExecutions = new ProcessInfo[(int)CompilerIndex.Count][];
                    _executions.Add(scriptExecutions);

                    foreach (CompilerRunner runner in compilerRunners)
                    {
                        HashSet<string> modules = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        HashSet<string> folders = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                        modules.Add(runner.GetOutputFileName(_outputFolder, script));
                        modules.UnionWith(_compilationInputFiles);
                        modules.UnionWith(_compilationInputFiles.Select(file => runner.GetOutputFileName(_outputFolder, file)));
                        folders.Add(Path.GetDirectoryName(script));
                        folders.UnionWith(runner.ReferenceFolders);

                        ProcessConstructor constructor = new ScriptExecutionProcessConstructor(runner, _outputFolder, script, modules, folders);
                        ProcessInfo[] iterations = new ProcessInfo[options.Iterations];
                        for (int iterationIndex = 0; iterationIndex < options.Iterations; iterationIndex++)
                        {
                            iterations[iterationIndex] = new ProcessInfo(constructor);
                        }

                        scriptExecutions[(int)runner.Index] = iterations;
                    }
                }
            }
        }

        public static BuildFolder FromDirectory(string inputDirectory, IEnumerable<CompilerRunner> compilerRunners, string outputRoot, BuildOptions options)
        {
            List<string> compilationInputFiles = new List<string>();
            HashSet<string> passThroughFiles = new HashSet<string>();
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
                if ((!isManagedAssembly || options.Composite) &&
                    (Path.GetExtension(file) != ".pdb") && (Path.GetExtension(file) != ".ilk")) // exclude .pdb and .ilk files that are large and not needed in the target folder
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

            if (options.Composite && !(options.Framework || options.UseFramework))
            {
                // In composite mode we copy the native runtime to the app folder and pretend that is CORE_ROOT,
                // otherwise CoreRun picks up the original MSIL versions of framework assemblies from CORE_ROOT
                // instead of the rewritten ones next to the app.
                foreach (string exe in s_runtimeExecutables)
                {
                    passThroughFiles.Add(Path.Combine(options.CoreRootDirectory.FullName, exe.AppendOSExeSuffix()));
                }
                string libraryPrefix = (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "" : "lib");
                foreach (string lib in s_runtimeLibraries)
                {
                    passThroughFiles.Add(Path.Combine(options.CoreRootDirectory.FullName, (libraryPrefix + lib).AppendOSDllSuffix()));
                }
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    foreach (string lib in s_runtimeWindowsOnlyLibraries)
                    {
                        passThroughFiles.Add(Path.Combine(options.CoreRootDirectory.FullName, lib.AppendOSDllSuffix()));
                    }
                }
                else
                {
                    // Several native lib*.so / dylib are needed by the runtime
                    foreach (string nativeLib in Directory.EnumerateFiles(options.CoreRootDirectory.FullName, "lib*".AppendOSDllSuffix()))
                    {
                        passThroughFiles.Add(nativeLib);
                    }
                }
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
            ProcessInfo[] executionProcesses = _executions[executionIndex][(int)compilerIndex];
            if (executionProcesses != null)
            {
                foreach (ProcessInfo executionProcess in executionProcesses.Where(ep => ep.JittedMethods != null))
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

        public IList<ProcessInfo[][]> Executions => _executions;
    }
}
