// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace ReadyToRun.SuperIlc
{
    public enum CompilerIndex
    {
        CPAOT,
        Crossgen,
        Jit,

        Count
    }

    public abstract class CompilerRunner
    {
        /// <summary>
        /// Timeout for running R2R Dump to disassemble compilation outputs.
        /// </summary>
        public const int R2RDumpTimeoutMilliseconds = 60 * 1000;

        protected readonly BuildOptions _options;
        protected readonly IEnumerable<string> _referenceFolders;

        public CompilerRunner(BuildOptions options, IEnumerable<string> referenceFolders)
        {
            _options = options;
            _referenceFolders = referenceFolders;
        }

        public IEnumerable<string> ReferenceFolders => _referenceFolders;

        public abstract CompilerIndex Index { get; }

        public string CompilerName => Index.ToString();

        protected abstract string CompilerRelativePath { get;  }
        protected abstract string CompilerFileName { get; }

        protected virtual string CompilerPath => Path.Combine(_options.CoreRootDirectory.FullName, CompilerRelativePath, CompilerFileName);

        protected abstract IEnumerable<string> BuildCommandLineArguments(string assemblyFileName, string outputFileName);

        public virtual ProcessParameters CompilationProcess(string outputRoot, string assemblyFileName)
        {
            CreateOutputFolder(outputRoot);

            string outputFileName = GetOutputFileName(outputRoot, assemblyFileName);
            string responseFile = GetResponseFileName(outputRoot, assemblyFileName);
            var commandLineArgs = BuildCommandLineArguments(assemblyFileName, outputFileName);
            CreateResponseFile(responseFile, commandLineArgs);

            ProcessParameters processParameters = new ProcessParameters();
            processParameters.ProcessPath = CompilerPath;
            processParameters.Arguments = $"@{responseFile}";
            if (_options.CompilationTimeoutMinutes != 0)
            {
                processParameters.TimeoutMilliseconds = _options.CompilationTimeoutMinutes * 60 * 1000;
            }
            else
            {
                processParameters.TimeoutMilliseconds = ProcessParameters.DefaultIlcTimeout;
            }
            processParameters.LogPath = outputFileName + ".ilc.log";
            processParameters.InputFileName = assemblyFileName;
            processParameters.OutputFileName = outputFileName;
            processParameters.CompilationCostHeuristic = new FileInfo(assemblyFileName).Length;

            return processParameters;
        }

        public ProcessParameters CompilationR2RDumpProcess(string compiledExecutable, bool naked)
        {
            if (_options.R2RDumpPath == null)
            {
                return null;
            }

            StringBuilder commonBuilder = new StringBuilder();

            commonBuilder.Append($@"""{_options.R2RDumpPath.FullName}""");

            commonBuilder.Append(" --normalize");
            commonBuilder.Append(" --sc");
            commonBuilder.Append(" --disasm");

            foreach (string referencePath in _options.ReferencePaths())
            {
                commonBuilder.Append($@" --rp ""{referencePath}""");
            }

            if (_options.CoreRootDirectory != null)
            {
                commonBuilder.Append($@" --rp ""{_options.CoreRootDirectory.FullName}""");
            }

            commonBuilder.Append($@" --in ""{compiledExecutable}""");

            StringBuilder builder = new StringBuilder(commonBuilder.ToString());
            if (naked)
            {
                builder.Append(" --naked");
            }

            string outputFileName = compiledExecutable + (naked ? ".naked.r2r" : ".raw.r2r");
            builder.Append($@" --out ""{outputFileName}""");

            ProcessParameters param = new ProcessParameters();
            param.ProcessPath = "dotnet";
            param.Arguments = builder.ToString();
            param.TimeoutMilliseconds = R2RDumpTimeoutMilliseconds;
            param.LogPath = compiledExecutable + (naked ? ".naked.r2r.log" : ".raw.r2r.log");
            param.InputFileName = compiledExecutable;
            param.OutputFileName = outputFileName;
            try
            {
                param.CompilationCostHeuristic = new FileInfo(compiledExecutable).Length;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("File not found: {0}: {1}", compiledExecutable, ex);
                param.CompilationCostHeuristic = 0;
            }

            return param;
        }

        protected virtual ProcessParameters ExecutionProcess(IEnumerable<string> modules, IEnumerable<string> folders, bool noEtw)
        {
            ProcessParameters processParameters = new ProcessParameters();

            if (_options.ExecutionTimeoutMinutes != 0)
            {
                processParameters.TimeoutMilliseconds = _options.ExecutionTimeoutMinutes * 60 * 1000;
            }
            else if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("__GCSTRESSLEVEL")))
            {
                processParameters.TimeoutMilliseconds = ProcessParameters.DefaultExeTimeout;
            }
            else
            {
                processParameters.TimeoutMilliseconds = ProcessParameters.DefaultExeTimeoutGCStress;
            }

            // TODO: support for tier jitting - for now we just turn it off as it may distort the JIT statistics
            processParameters.EnvironmentOverrides["COMPLUS_TieredCompilation"] = "0";

            processParameters.CollectJittedMethods = !noEtw;
            if (!noEtw)
            {
                processParameters.MonitorModules = modules;
                processParameters.MonitorFolders = folders;
            }

            return processParameters;
        }

        public virtual ProcessParameters ScriptExecutionProcess(string outputRoot, string scriptPath, IEnumerable<string> modules, IEnumerable<string> folders)
        {
            string scriptToRun = GetOutputFileName(outputRoot, scriptPath);
            ProcessParameters processParameters = ExecutionProcess(modules, folders, _options.NoEtw);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                processParameters.ProcessPath = scriptToRun;
                processParameters.Arguments = null;
            }
            else
            {
                Linux.MakeExecutable(scriptToRun);
                processParameters.ProcessPath = "bash";
                processParameters.Arguments = "-c " + scriptToRun;
            }

            processParameters.InputFileName = scriptToRun;
            processParameters.OutputFileName = scriptToRun;
            processParameters.LogPath = scriptToRun + ".log";
            processParameters.EnvironmentOverrides["CORE_ROOT"] = _options.CoreRootOutputPath(Index, isFramework: false);
            return processParameters;
        }

        public virtual ProcessParameters AppExecutionProcess(string outputRoot, string appPath, IEnumerable<string> modules, IEnumerable<string> folders)
        {
            string exeToRun = GetOutputFileName(outputRoot, appPath);
            ProcessParameters processParameters = ExecutionProcess(modules, folders, _options.NoEtw);
            processParameters.ProcessPath = _options.CoreRunPath(Index, isFramework: false);
            processParameters.Arguments = exeToRun;
            processParameters.InputFileName = exeToRun;
            processParameters.OutputFileName = exeToRun;
            processParameters.LogPath = exeToRun + ".log";
            processParameters.ExpectedExitCode = 100;
            return processParameters;
        }

        public void CreateOutputFolder(string outputRoot)
        {
            string outputPath = GetOutputPath(outputRoot);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
        }

        protected void CreateResponseFile(string responseFile, IEnumerable<string> commandLineArguments)
        {
            using (TextWriter tw = File.CreateText(responseFile))
            {
                foreach (var arg in commandLineArguments)
                {
                    tw.WriteLine(arg);
                }
            }
        }

        public string GetOutputPath(string outputRoot) => Path.Combine(outputRoot, CompilerName + _options.ConfigurationSuffix);

        // <input>\a.dll -> <output>\a.dll
        public string GetOutputFileName(string outputRoot, string fileName) =>
            Path.Combine(GetOutputPath(outputRoot), Path.GetFileName(fileName));

        public string GetResponseFileName(string outputRoot, string assemblyFileName) =>
            Path.Combine(GetOutputPath(outputRoot), Path.GetFileName(assemblyFileName) + ".rsp");
    }

    public abstract class CompilerRunnerProcessConstructor : ProcessConstructor
    {
        protected readonly CompilerRunner _runner;

        public CompilerRunnerProcessConstructor(CompilerRunner runner)
        {
            _runner = runner;
        }
    }

    public class CompilationProcessConstructor : CompilerRunnerProcessConstructor
    {
        private readonly string _outputRoot;
        private readonly string _assemblyFileName;

        public CompilationProcessConstructor(CompilerRunner runner, string outputRoot, string assemblyFileName)
            : base(runner)
        {
            _outputRoot = outputRoot;
            _assemblyFileName = assemblyFileName;
        }

        public override ProcessParameters Construct()
        {
            return _runner.CompilationProcess(_outputRoot, _assemblyFileName);
        }
    }

    public class R2RDumpProcessConstructor : CompilerRunnerProcessConstructor
    {
        private readonly string _compiledExecutable;
        private readonly bool _naked;

        public R2RDumpProcessConstructor(CompilerRunner runner, string compiledExecutable, bool naked)
            : base(runner)
        {
            _compiledExecutable = compiledExecutable;
            _naked = naked;
        }

        public override ProcessParameters Construct()
        {
            return _runner.CompilationR2RDumpProcess(_compiledExecutable, _naked);
        }
    }

    public sealed class ScriptExecutionProcessConstructor : CompilerRunnerProcessConstructor
    {
        private readonly string _outputRoot;
        private readonly string _scriptPath;
        private readonly IEnumerable<string> _modules;
        private readonly IEnumerable<string> _folders;

        public ScriptExecutionProcessConstructor(CompilerRunner runner, string outputRoot, string scriptPath, IEnumerable<string> modules, IEnumerable<string> folders)
            : base(runner)
        {
            _outputRoot = outputRoot;
            _scriptPath = scriptPath;
            _modules = modules;
            _folders = folders;
        }

        public override ProcessParameters Construct()
        {
            return _runner.ScriptExecutionProcess(_outputRoot, _scriptPath, _modules, _folders);
        }
    }

    public sealed class AppExecutionProcessConstructor : CompilerRunnerProcessConstructor
    {
        private readonly string _outputRoot;
        private readonly string _appPath;
        private readonly IEnumerable<string> _modules;
        private readonly IEnumerable<string> _folders;

        public AppExecutionProcessConstructor(CompilerRunner runner, string outputRoot, string appPath, IEnumerable<string> modules, IEnumerable<string> folders)
            : base(runner)
        {
            _outputRoot = outputRoot;
            _appPath = appPath;
            _modules = modules;
            _folders = folders;
        }

        public override ProcessParameters Construct()
        {
            return _runner.AppExecutionProcess(_outputRoot, _appPath, _modules, _folders);
        }
    }
}
