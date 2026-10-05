// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This task is replicated in dotnet/sdk at src/Tasks/Microsoft.NET.Build.Tasks/RunReadyToRunCompiler.cs.
// Keep both copies synchronized.

#nullable disable

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    public class RunReadyToRunCompiler : ToolTask
    {
        public ITaskItem CrossgenTool { get; set; }
        public ITaskItem Crossgen2Tool { get; set; }

        [Required]
        public ITaskItem CompilationEntry { get; set; }
        [Required]
        public ITaskItem[] ImplementationAssemblyReferences { get; set; }
        public ITaskItem[] ReadyToRunCompositeBuildReferences { get; set; }
        public ITaskItem[] ReadyToRunCompositeBuildInput { get; set; }
        public ITaskItem[] ReadyToRunCompositeUnrootedBuildInput { get; set; }
        public bool ShowCompilerWarnings { get; set; }
        public bool UseCrossgen2 { get; set; }
        public string Crossgen2ExtraCommandLineArgs { get; set; }
        public string Crossgen2CompositeExtraCommandLineArgs { get; set; }
        public ITaskItem[] Crossgen2PgoFiles { get; set; }
        public string Crossgen2ContainerFormat { get; set; }

        [Output]
        public bool WarningsDetected { get; set; }

        private bool _emitSymbols;
        private string _inputAssembly;
        private string _outputR2RImage;
        private string _outputPDBImage;
        private string _createPDBCommand;
        private bool _createCompositeImage;
        private List<Crossgen2Cache.Diagnostic> _cacheDiagnostics;

        private bool IsPdbCompilation => !string.IsNullOrEmpty(_createPDBCommand);
        private bool ActuallyUseCrossgen2 => UseCrossgen2 && !IsPdbCompilation;

        private string DotNetHostPath => Crossgen2Tool?.GetMetadata(MetadataKeys.DotNetHostPath);

        private bool Crossgen2IsVersion5
        {
            get
            {
                string version5 = Crossgen2Tool?.GetMetadata(MetadataKeys.IsVersion5);
                return !string.IsNullOrEmpty(version5) && bool.Parse(version5);
            }
        }

        protected override string ToolName
        {
            get
            {
                if (ActuallyUseCrossgen2)
                {
                    string hostPath = DotNetHostPath;
                    if (!string.IsNullOrEmpty(hostPath))
                    {
                        return hostPath;
                    }
                    return Crossgen2Tool.ItemSpec;
                }
                return CrossgenTool.ItemSpec;
            }
        }

        protected override string GenerateFullPathToTool() => ToolName;

        private string DiaSymReader => CrossgenTool.GetMetadata(MetadataKeys.DiaSymReader);

        public RunReadyToRunCompiler()
        {
            LogStandardErrorAsError = true;
        }

        protected override bool ValidateParameters()
        {
            string emitSymbolsMetadata = CompilationEntry.GetMetadata(MetadataKeys.EmitSymbols);
            _emitSymbols = !string.IsNullOrEmpty(emitSymbolsMetadata) && bool.Parse(emitSymbolsMetadata);
            _createPDBCommand = CompilationEntry.GetMetadata(MetadataKeys.CreatePDBCommand);
            string createCompositeImageMetadata = CompilationEntry.GetMetadata(MetadataKeys.CreateCompositeImage);
            _createCompositeImage = !string.IsNullOrEmpty(createCompositeImageMetadata) && bool.Parse(createCompositeImageMetadata);

            if (IsPdbCompilation && CrossgenTool == null)
            {
                // PDB compilation is a step specific to Crossgen1 and 5.0 Crossgen2
                // which didn't support PDB generation. 6.0  Crossgen2 produces symbols
                // directly during native compilation.
                Log.LogError(Strings.CrossgenToolMissingInPDBCompilationMode);
                return false;
            }

            if (ActuallyUseCrossgen2)
            {
                if (Crossgen2Tool == null)
                {
                    Log.LogError(Strings.Crossgen2ToolMissingWhenUseCrossgen2IsSet);
                    return false;
                }
                if (!File.Exists(Crossgen2Tool.ItemSpec))
                {
                    Log.LogError(Strings.Crossgen2ToolExecutableNotFound, Crossgen2Tool.ItemSpec);
                    return false;
                }
                string hostPath = DotNetHostPath;
                if (!string.IsNullOrEmpty(hostPath) && !File.Exists(hostPath))
                {
                    Log.LogError(Strings.DotNetHostExecutableNotFound, hostPath);
                    return false;
                }
                string jitPath = Crossgen2Tool.GetMetadata(MetadataKeys.JitPath);
                if (!string.IsNullOrEmpty(jitPath))
                {
                    if (!File.Exists(jitPath))
                    {
                        Log.LogError(Strings.JitLibraryNotFound, jitPath);
                        return false;
                    }
                }
                else if (Crossgen2IsVersion5)
                {
                    // We expect JitPath to be set for .NET 5 and {TargetOS, TargetArch} to be set for .NET 6 and later
                    Log.LogError(Strings.Crossgen2MissingRequiredMetadata, MetadataKeys.JitPath);
                    return false;
                }
                else
                {
                    // For smooth switchover we accept both JitPath and TargetOS / TargetArch in .NET 6 Crossgen2
                    if (string.IsNullOrEmpty(Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS)))
                    {
                        Log.LogError(Strings.Crossgen2MissingRequiredMetadata, MetadataKeys.TargetOS);
                        return false;
                    }
                    if (string.IsNullOrEmpty(Crossgen2Tool.GetMetadata(MetadataKeys.TargetArch)))
                    {
                        Log.LogError(Strings.Crossgen2MissingRequiredMetadata, MetadataKeys.TargetArch);
                        return false;
                    }
                }
            }
            else
            {
                if (CrossgenTool == null)
                {
                    Log.LogError(Strings.CrossgenToolMissingWhenUseCrossgen2IsNotSet);
                    return false;
                }
                if (!File.Exists(CrossgenTool.ItemSpec))
                {
                    Log.LogError(Strings.CrossgenToolExecutableNotFound, CrossgenTool.ItemSpec);
                    return false;
                }
                if (!File.Exists(CrossgenTool.GetMetadata(MetadataKeys.JitPath)))
                {
                    Log.LogError(Strings.JitLibraryNotFound, MetadataKeys.JitPath);
                    return false;
                }
            }

            _outputPDBImage = CompilationEntry.GetMetadata(MetadataKeys.OutputPDBImage);

            if (IsPdbCompilation)
            {
                _outputR2RImage = CompilationEntry.ItemSpec;

                if (!string.IsNullOrEmpty(DiaSymReader) && !File.Exists(DiaSymReader))
                {
                    Log.LogError(Strings.DiaSymReaderLibraryNotFound, DiaSymReader);
                    return false;
                }

                // R2R image has to be created before emitting native symbols (crossgen needs this as an input argument)
                if (string.IsNullOrEmpty(_outputPDBImage))
                {
                    Log.LogError(Strings.MissingOutputPDBImagePath);
                }

                if (!File.Exists(_outputR2RImage))
                {
                    Log.LogError(Strings.PDBGeneratorInputExecutableNotFound, _outputR2RImage);
                    return false;
                }
            }
            else
            {
                _outputR2RImage = CompilationEntry.GetMetadata(MetadataKeys.OutputR2RImage);

                if (!_createCompositeImage)
                {
                    _inputAssembly = CompilationEntry.ItemSpec;
                    if (!File.Exists(_inputAssembly))
                    {
                        Log.LogError(Strings.InputAssemblyNotFound, _inputAssembly);
                        return false;
                    }
                }
                else
                {
                    _inputAssembly = "CompositeImage";
                }

                if (string.IsNullOrEmpty(_outputR2RImage))
                {
                    Log.LogError(Strings.MissingOutputR2RImageFileName);
                    return false;
                }

                if (_emitSymbols && string.IsNullOrEmpty(_outputPDBImage))
                {
                    Log.LogError(Strings.MissingOutputPDBImagePath);
                }
            }

            return true;
        }

        private string GetAssemblyReferencesCommands()
        {
            StringBuilder result = new();

            var references = _createCompositeImage ? ReadyToRunCompositeBuildReferences : ImplementationAssemblyReferences;

            if (references != null)
            {
                foreach (var reference in (_createCompositeImage ? ReadyToRunCompositeBuildReferences : ImplementationAssemblyReferences))
                {
                    // When generating PDBs, we must not add a reference to the IL version of the R2R image for which we're trying to generate a PDB
                    if (IsPdbCompilation && string.Equals(Path.GetFileName(reference.ItemSpec), Path.GetFileName(_outputR2RImage), StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (UseCrossgen2 && !IsPdbCompilation)
                    {
                        result.AppendLine($"-r:\"{reference.ItemSpec}\"");
                    }
                    else
                    {
                        result.AppendLine($"-r \"{reference.ItemSpec}\"");
                    }
                }
            }

            return result.ToString();
        }

        protected override string GenerateCommandLineCommands()
        {
            if (ActuallyUseCrossgen2 && !string.IsNullOrEmpty(DotNetHostPath))
            {
                return $"\"{Crossgen2Tool.ItemSpec}\"";
            }
            return null;
        }

        protected override string GenerateResponseFileCommands()
        {
            // Crossgen2 5.0 doesn't support PDB generation so Crossgen1 is used for that purpose.
            if (ActuallyUseCrossgen2)
            {
                return GenerateCrossgen2ResponseFile();
            }
            else
            {
                return GenerateCrossgenResponseFile();
            }
        }

        private string GenerateCrossgenResponseFile()
        {
            StringBuilder result = new();

            result.AppendLine("/nologo");

            if (IsPdbCompilation)
            {
                result.Append(GetAssemblyReferencesCommands());

                if (!string.IsNullOrEmpty(DiaSymReader))
                {
                    result.AppendLine($"/DiasymreaderPath \"{DiaSymReader}\"");
                }

                result.AppendLine(_createPDBCommand);
                result.AppendLine($"\"{_outputR2RImage}\"");
            }
            else
            {
                result.AppendLine("/MissingDependenciesOK");
                result.AppendLine($"/JITPath \"{CrossgenTool.GetMetadata(MetadataKeys.JitPath)}\"");
                result.Append(GetAssemblyReferencesCommands());
                result.AppendLine($"/out \"{_outputR2RImage}\"");
                result.AppendLine($"\"{_inputAssembly}\"");
            }

            return result.ToString();
        }

        private string GenerateCrossgen2ResponseFile()
        {
            StringBuilder result = new();

            string jitPath = Crossgen2Tool.GetMetadata(MetadataKeys.JitPath);
            if (!string.IsNullOrEmpty(jitPath))
            {
                result.AppendLine($"--jitpath:\"{jitPath}\"");
            }
            else
            {
                result.AppendLine($"--targetos:{Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS)}");
                result.AppendLine($"--targetarch:{Crossgen2Tool.GetMetadata(MetadataKeys.TargetArch)}");
            }

            result.AppendLine("-O");

            // 5.0 Crossgen2 doesn't support PDB generation.
            if (!Crossgen2IsVersion5 && _emitSymbols)
            {
                if (Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) == "windows")
                {
                    result.AppendLine("--pdb");
                    result.AppendLine($"--pdb-path:{Path.GetDirectoryName(_outputPDBImage)}");
                }
                else
                {
                    result.AppendLine("--perfmap");
                    result.AppendLine($"--perfmap-path:{Path.GetDirectoryName(_outputPDBImage)}");

                    string perfmapFormatVersion = Crossgen2Tool.GetMetadata(MetadataKeys.PerfmapFormatVersion);
                    if (!string.IsNullOrEmpty(perfmapFormatVersion))
                    {
                        result.AppendLine($"--perfmap-format-version:{perfmapFormatVersion}");
                    }
                }
            }

            if (Crossgen2PgoFiles != null)
            {
                foreach (var mibc in Crossgen2PgoFiles)
                {
                    result.AppendLine($"-m:\"{mibc.ItemSpec}\"");
                }
            }

            if (!string.IsNullOrEmpty(Crossgen2ContainerFormat))
            {
                result.AppendLine($"--obj-format:{Crossgen2ContainerFormat}");
            }

            if (!string.IsNullOrEmpty(Crossgen2ExtraCommandLineArgs))
            {
                foreach (string extraArg in Crossgen2ExtraCommandLineArgs.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    result.AppendLine(extraArg);
                }
            }

            if (_createCompositeImage)
            {
                result.AppendLine("--composite");

                // Crossgen2 v5 only supported compilation with --inputbubble specified
                if (Crossgen2IsVersion5)
                    result.AppendLine("--inputbubble");

                result.AppendLine($"--out:\"{_outputR2RImage}\"");

                result.Append(GetAssemblyReferencesCommands());

                // Note: do not add double quotes around the input assembly, even if the file path contains spaces. The command line
                // parsing logic will append this string to the working directory if it's a relative path, so any double quotes will result in errors.
                foreach (var reference in ReadyToRunCompositeBuildInput)
                {
                    result.AppendLine(reference.ItemSpec);
                }

                if (ReadyToRunCompositeUnrootedBuildInput != null)
                {
                    foreach (var unrooted in ReadyToRunCompositeUnrootedBuildInput)
                    {
                        result.AppendLine($"-u:\"{unrooted.ItemSpec}\"");
                    }
                }

                if (!string.IsNullOrEmpty(Crossgen2CompositeExtraCommandLineArgs))
                {
                    foreach (string extraArg in Crossgen2CompositeExtraCommandLineArgs.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        result.AppendLine(extraArg);
                    }
                }
            }
            else
            {
                result.Append(GetAssemblyReferencesCommands());
                result.AppendLine($"--out:\"{_outputR2RImage}\"");

                // Note: do not add double quotes around the input assembly, even if the file path contains spaces. The command line
                // parsing logic will append this string to the working directory if it's a relative path, so any double quotes will result in errors.
                result.AppendLine($"{_inputAssembly}");
            }

            return result.ToString();
        }

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            // Ensure output sub-directories exists - Crossgen does not create directories for output files. Any relative path used with the
            // '/out' parameter has to have an existing directory.
            Directory.CreateDirectory(Path.GetDirectoryName(_outputR2RImage));

            WarningsDetected = false;

            DateTime timestamp = DateTime.UtcNow;
            Crossgen2Cache cache = Crossgen2Cache.FromEnvironment(Log);
            string key = null;
            string[] outputs = null;
            if (cache is not null)
            {
                try
                {
                    if (!ActuallyUseCrossgen2 || Crossgen2IsVersion5 || _createCompositeImage ||
                        !string.IsNullOrEmpty(DotNetHostPath) || UseCommandProcessor ||
                        EnvironmentVariables?.Length > 0 ||
                        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")) ||
                        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LD_PRELOAD")) ||
                        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LD_AUDIT")) ||
                        !string.IsNullOrEmpty(Crossgen2ContainerFormat) ||
                        HasExtraArguments(Crossgen2ExtraCommandLineArgs) ||
                        HasExtraArguments(Crossgen2CompositeExtraCommandLineArgs) ||
                        !RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                        RuntimeInformation.ProcessArchitecture != Architecture.X64 ||
                        Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) != "linux" ||
                        Crossgen2Tool.GetMetadata(MetadataKeys.TargetArch) != "x64" ||
                        Path.GetFullPath(pathToTool) != Path.GetFullPath(Crossgen2Tool.ItemSpec))
                    {
                        cache.Report("bypass: unsupported invocation (requires native Linux-x64, non-composite PE crossgen2 without extra arguments or environment overrides)");
                    }
                    else
                    {
                        string image = Crossgen2Cache.InputPath(_outputR2RImage);
                        string map = Path.ChangeExtension(image, ".ni.r2rmap");
                        if (_emitSymbols && (Crossgen2Tool.GetMetadata(MetadataKeys.PerfmapFormatVersion) != "1" ||
                            Crossgen2Cache.InputPath(_outputPDBImage) != map))
                        {
                            cache.Report("bypass: symbols require a version-1 perf map beside the image with the compiler-generated filename");
                        }
                        else
                        {
                            outputs = new[] { image, map };
                            key = cache.ComputeKey(pathToTool, Crossgen2Tool.GetMetadata(MetadataKeys.JitPath),
                                _inputAssembly, ImplementationAssemblyReferences, Crossgen2PgoFiles,
                                outputs, responseFileCommands, commandLineCommands,
                                string.Join("\n", ShowCompilerWarnings, LogStandardErrorAsError, StandardOutputImportance, StandardErrorImportance),
                                GetWorkingDirectory() ?? Directory.GetCurrentDirectory());
                        }
                    }
                }
                catch (Exception ex) when (Crossgen2Cache.IsCacheFailure(ex))
                {
                    cache.Report($"bypass: {ex.Message}");
                }
            }

            if (key is null)
            {
                return ExecuteCompiler(pathToTool, responseFileCommands, commandLineCommands);
            }

            if (cache.TryRestore(key, outputs, timestamp, out List<Crossgen2Cache.Diagnostic> diagnostics))
            {
                foreach (Crossgen2Cache.Diagnostic diagnostic in diagnostics)
                {
                    LogEventsFromTextOutput(diagnostic.Text, diagnostic.Importance);
                }
                return 0;
            }

            // Remove only the declared outputs, including any partially restored files.
            // A cleanup failure must fail the task rather than reuse stale optional outputs.
            foreach (string output in outputs)
            {
                File.Delete(output);
            }

            _cacheDiagnostics = new List<Crossgen2Cache.Diagnostic>();
            int exitCode;
            try
            {
                exitCode = ExecuteCompiler(pathToTool, responseFileCommands, commandLineCommands);
                if (exitCode == 0 && !Log.HasLoggedErrors)
                {
                    cache.Store(key, outputs, _cacheDiagnostics);
                }
            }
            finally
            {
                _cacheDiagnostics = null;
            }
            return exitCode;
        }

        internal virtual int ExecuteCompiler(string pathToTool, string responseFileCommands, string commandLineCommands)
            => base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

        private static bool HasExtraArguments(string arguments)
            => !string.IsNullOrEmpty(arguments) && arguments.Any(c => c != ';' && !char.IsWhiteSpace(c));

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
            if (_cacheDiagnostics is not null)
            {
                lock (_cacheDiagnostics)
                {
                    _cacheDiagnostics.Add(new Crossgen2Cache.Diagnostic(singleLine, messageImportance));
                }
            }

            if (!ShowCompilerWarnings && singleLine.Contains("warning:", StringComparison.OrdinalIgnoreCase))
            {
                Log.LogMessage(MessageImportance.Normal, singleLine);
                WarningsDetected = true;
            }
            else
            {
                base.LogEventsFromTextOutput(singleLine, messageImportance);
            }
        }
    }
}
