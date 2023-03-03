// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace R2RTest
{
    public enum CompilerIndex
    {
        CPAOT,
        Crossgen,
        Jit,

        Count
    }

    public enum ExclusionType
    {
        Ignore,
        DontCrossgen2,
    }

    public sealed class FrameworkExclusion
    {
        private static FrameworkExclusion[] s_list =
        {
            new FrameworkExclusion(ExclusionType.Ignore, "CommandLine", "Not a framework assembly"),
            new FrameworkExclusion(ExclusionType.Ignore, "R2RDump", "Not a framework assembly"),

            // TODO (DavidWr): IBC-related failures
            new FrameworkExclusion(ExclusionType.DontCrossgen2, "Microsoft.CodeAnalysis.CSharp", "Ibc TypeToken 6200019a has type token which resolves to a nil token"),
            new FrameworkExclusion(ExclusionType.DontCrossgen2, "Microsoft.CodeAnalysis", "Ibc TypeToken 620001af unable to find external typedef"),
            new FrameworkExclusion(ExclusionType.DontCrossgen2, "Microsoft.CodeAnalysis.VisualBasic", "Ibc TypeToken 620002ce unable to find external typedef"),
        };

        public readonly ExclusionType ExclusionType;
        public readonly string SimpleName;
        public readonly string Reason;

        public FrameworkExclusion(ExclusionType exclusionType, string simpleName, string reason)
        {
            ExclusionType = exclusionType;
            SimpleName = simpleName;
            Reason = reason;
        }

        public static FrameworkExclusion Find(string simpleName)
        {
            return s_list.FirstOrDefault(fe => StringComparer.OrdinalIgnoreCase.Equals(simpleName, fe.SimpleName));
        }

        public static bool Exclude(string simpleName, CompilerIndex index, out string reason)
        {
            FrameworkExclusion exclusion = Find(simpleName);
            if (exclusion != null &&
                (exclusion.ExclusionType == ExclusionType.Ignore ||
                exclusion.ExclusionType == ExclusionType.DontCrossgen2 && index == CompilerIndex.CPAOT))
            {
                reason = exclusion.Reason;
                return true;
            }

            if (simpleName.StartsWith("xunit.", StringComparison.OrdinalIgnoreCase))
            {
                reason = "XUnit";
                return true;
            }

            reason = null;
            return false;
        }
    }

    public abstract class CompilerRunner
    {
        /// <summary>
        /// Timeout for running R2R Dump to disassemble compilation outputs.
        /// </summary>
        public const int R2RDumpTimeoutMilliseconds = 60 * 1000;

        protected readonly BuildOptions _options;
        protected readonly List<string> _referenceFolders = new List<string>();
        protected readonly string _overrideOutputPath;

        public CompilerRunner(BuildOptions options, IEnumerable<string> references, string overrideOutputPath = null)
        {
            _options = options;
            _overrideOutputPath = overrideOutputPath;

            foreach (var reference in references)
            {
                if (Directory.Exists(reference))
                {
                    _referenceFolders.Add(reference);
                }
            }
        }

        public IEnumerable<string> ReferenceFolders => _referenceFolders;

        public abstract CompilerIndex Index { get; }

        public string CompilerName => Index.ToString();

        protected abstract string CompilerRelativePath { get;  }
        protected abstract string CompilerFileName { get; }

        protected virtual string CompilerPath => Path.Combine(_options.CoreRootDirectory.FullName, CompilerRelativePath, CompilerFileName);

        protected abstract IEnumerable<string> BuildCommandLineArguments(IEnumerable<string> assemblyFileNames, string outputFileName);

        public virtual ProcessParameters CompilationProcess(string outputFileName, IEnumerable<string> inputAssemblyFileNames)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(outputFileName));

            string responseFile = outputFileName + ".rsp";
            var commandLineArgs = BuildCommandLineArguments(inputAssemblyFileNames, outputFileName);
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
                processParameters.TimeoutMilliseconds = (_options.Composite ? ProcessParameters.DefaultIlcCompositeTimeout : ProcessParameters.DefaultIlcTimeout);
            }
            processParameters.LogPath = outputFileName + ".ilc.log";
            processParameters.InputFileNames = inputAssemblyFileNames;
            processParameters.OutputFileName = outputFileName;

            foreach (string inputAssembly in inputAssemblyFileNames)
            {
                processParameters.CompilationCostHeuristic += new FileInfo(inputAssembly).Length;
            }

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
            param.InputFileNames = new string[] { compiledExecutable };
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

            if (!string.IsNullOrEmpty(_options.GCStress))
            {
                processParameters.EnvironmentOverrides["DOTNET_GCStress"] = _options.GCStress;
            }

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
            processParameters.EnvironmentOverrides["DOTNET_TieredCompilation"] = "0";
            processParameters.EnvironmentOverrides["COMPlus_TieredCompilation"] = "0";

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

            string coreRootDir;
            if (_options.Composite && !(_options.Framework || _options.UseFramework))
            {
                coreRootDir = GetOutputPath(outputRoot);
            }
            else
            {
                coreRootDir = _options.CoreRootOutputPath(Index, isFramework: false);
            }

            processParameters.InputFileNames = new string[] { scriptToRun };
            processParameters.OutputFileName = scriptToRun;
            processParameters.LogPath = scriptToRun + ".log";
            processParameters.EnvironmentOverrides["CORE_ROOT"] = coreRootDir;

            return processParameters;
        }

        public virtual ProcessParameters AppExecutionProcess(string outputRoot, string appPath, IEnumerable<string> modules, IEnumerable<string> folders)
        {
            string exeToRun = GetOutputFileName(outputRoot, appPath);
            ProcessParameters processParameters = ExecutionProcess(modules, folders, _options.NoEtw);
            processParameters.ProcessPath = _options.CoreRunPath(Index, isFramework: false);
            processParameters.Arguments = exeToRun;
            processParameters.InputFileNames = new string[] { exeToRun };
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

        public string GetOutputPath(string outputRoot) => _overrideOutputPath ?? Path.Combine(outputRoot, CompilerName + _options.ConfigurationSuffix);

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
        private readonly string _outputFileName;
        private readonly IEnumerable<string> _inputAssemblyFileNames;

        public CompilationProcessConstructor(CompilerRunner runner, string outputFileName, IEnumerable<string> inputAssemblyFileNames)
            : base(runner)
        {
            _outputFileName = outputFileName;
            _inputAssemblyFileNames = inputAssemblyFileNames;
        }

        public override ProcessParameters Construct()
        {
            return _runner.CompilationProcess(_outputFileName, _inputAssemblyFileNames);
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
            ProcessParameters processParameters = _runner.ScriptExecutionProcess(_outputRoot, _scriptPath, _modules, _folders);
            return processParameters;
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
