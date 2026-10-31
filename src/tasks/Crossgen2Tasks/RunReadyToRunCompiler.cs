// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This task is replicated in dotnet/sdk at src/Tasks/Microsoft.NET.Build.Tasks/RunReadyToRunCompiler.cs.
// Keep both copies synchronized.

#nullable disable

using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Microsoft.NET.Build.Tasks
{
    public class RunReadyToRunCompiler : ToolTask
    {
        private static readonly InvocationCacheConfiguration s_cacheConfiguration =
            new("Crossgen2", "CROSSGEN2_EXPERIMENTAL_CACHE", "CROSSGEN2_EXPERIMENTAL_CACHE_PATH");

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

            return ValidateOutputPaths();
        }

        private bool ValidateOutputPaths()
        {
            try
            {
                var outputs = new HashSet<string>(Crossgen2Cache.PathComparer);
                if (!IsPdbCompilation)
                {
                    outputs.Add(Path.GetFullPath(_outputR2RImage));
                }
                string symbols = GetOwnedSymbolOutputPath();
                if (!string.IsNullOrEmpty(symbols))
                {
                    outputs.Add(Path.GetFullPath(symbols));
                }

                ITaskItem tool = ActuallyUseCrossgen2 ? Crossgen2Tool : CrossgenTool;
                IEnumerable<string> assemblies = new[]
                {
                    _createCompositeImage ? null : CompilationEntry.ItemSpec,
                    ActuallyUseCrossgen2 && !_createCompositeImage ? CompilationEntry.ItemSpec.Trim() : null
                };
                IEnumerable<string> inputs = new[]
                {
                    tool.ItemSpec,
                    tool.GetMetadata(MetadataKeys.JitPath),
                    ActuallyUseCrossgen2 ? DotNetHostPath : DiaSymReader
                };
                IEnumerable<ITaskItem> references = _createCompositeImage
                    ? ReadyToRunCompositeBuildReferences : ImplementationAssemblyReferences;
                if (IsPdbCompilation && references is not null)
                {
                    references = references.Where(reference => !string.Equals(Path.GetFileName(reference.ItemSpec),
                        Path.GetFileName(_outputR2RImage), StringComparison.OrdinalIgnoreCase));
                }
                assemblies = assemblies.Concat((references ?? Array.Empty<ITaskItem>()).Select(item => item.ItemSpec));
                if (ActuallyUseCrossgen2)
                {
                    inputs = inputs.Concat((Crossgen2PgoFiles ?? Array.Empty<ITaskItem>()).Select(item => item.ItemSpec));
                    if (_createCompositeImage)
                    {
                        assemblies = assemblies.Concat((ReadyToRunCompositeBuildInput ?? Array.Empty<ITaskItem>()).SelectMany(item => new[] { item.ItemSpec, item.ItemSpec.Trim() }))
                            .Concat((ReadyToRunCompositeUnrootedBuildInput ?? Array.Empty<ITaskItem>()).Select(item => item.ItemSpec));
                    }
                    string directory = Path.GetDirectoryName(Path.GetFullPath(tool.ItemSpec));
                    inputs = inputs.Concat(new[]
                    {
                        Path.Combine(directory, OperatingSystem.IsWindows() ? "clrjit_win_x64_x64.dll" : "libclrjit_unix_x64_x64.so"),
                        Path.Combine(directory, OperatingSystem.IsWindows() ? "jitinterface_x64.dll" : "libjitinterface_x64.so")
                    });
                    if (_emitSymbols && Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) == "windows")
                    {
                        inputs = inputs.Append(Path.Combine(directory, "Microsoft.DiaSymReader.Native.dll"));
                    }
                }
                var assemblyPaths = new HashSet<string>(Crossgen2Cache.PathComparer);
                foreach (string assembly in assemblies)
                {
                    if (string.IsNullOrEmpty(assembly))
                    {
                        continue;
                    }
                    if (ActuallyUseCrossgen2 && assembly.AsSpan().IndexOfAny('*', '?') >= 0)
                    {
                        // Match crossgen2's non-recursive command-line wildcard expansion.
                        string directory = Path.GetDirectoryName(assembly);
                        if (string.IsNullOrEmpty(directory))
                        {
                            directory = ".";
                        }
                        if (Directory.Exists(directory))
                        {
                            foreach (string match in Directory.EnumerateFiles(directory, Path.GetFileName(assembly)))
                            {
                                assemblyPaths.Add(Path.GetFullPath(match));
                            }
                        }
                    }
                    else
                    {
                        assemblyPaths.Add(Path.GetFullPath(assembly));
                    }
                }
                foreach (string input in inputs.Concat(assemblyPaths))
                {
                    if (!ValidateInput(input))
                    {
                        return false;
                    }
                }
                foreach (string assembly in assemblyPaths)
                {
                    try
                    {
                        using FileStream stream = File.OpenRead(assembly);
                        using var pe = new PEReader(stream);
                        foreach (var candidate in Crossgen2Cache.GetSymbolCandidates(pe, assembly, Environment.CurrentDirectory,
                            skipNativePdbOutput: IsPdbCompilation && Crossgen2Cache.PathComparer.Equals(assembly, Path.GetFullPath(_outputR2RImage))))
                        {
                            if (!ValidateInput(candidate.Path))
                            {
                                return false;
                            }
                        }
                    }
                    catch (Exception ex) when (ex is BadImageFormatException or FileNotFoundException or DirectoryNotFoundException)
                    {
                        // Invalid or missing assemblies cannot supply symbols; let the compiler diagnose them.
                    }
                }

                bool ValidateInput(string input)
                {
                    if (!string.IsNullOrEmpty(input) && outputs.Contains(Path.GetFullPath(input)))
                    {
                        Log.LogError(Strings.CrossgenOutputOverlapsInput, input);
                        return false;
                    }

                    return true;
                }
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or InvalidDataException or UnauthorizedAccessException or NotSupportedException)
            {
                Log.LogErrorFromException(ex);
                return false;
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
            => InvocationCache.ExecuteCached(Log, s_cacheConfiguration,
                describeInvocation: () => DescribeInvocation(pathToTool, responseFileCommands, commandLineCommands),
                executeNormally: () => ExecuteNormally(pathToTool, responseFileCommands, commandLineCommands),
                readMetadata: ReadCachedTaskOutputs,
                writeMetadata: WriteCachedTaskOutputs);

#nullable enable annotations
        private InvocationDescription? DescribeInvocation(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            try
            {
                if (ShowCompilerWarnings || !ActuallyUseCrossgen2 || Crossgen2IsVersion5 || _createCompositeImage ||
                    !string.IsNullOrEmpty(DotNetHostPath) || UseCommandProcessor ||
                    EnvironmentVariables?.Length > 0 ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LD_LIBRARY_PATH")) ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LD_PRELOAD")) ||
                    !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("LD_AUDIT")) ||
                    (!string.IsNullOrEmpty(Crossgen2ContainerFormat) && Crossgen2ContainerFormat != "pe") ||
                    HasUnsafeExtraArguments(Crossgen2ExtraCommandLineArgs) ||
                    HasUnsafeExtraArguments(Crossgen2CompositeExtraCommandLineArgs) ||
                    !Crossgen2CacheInvocation.SupportsTarget(Crossgen2Tool) ||
                    !Crossgen2Cache.PathComparer.Equals(Crossgen2Cache.InputPath(pathToTool), Crossgen2Cache.InputPath(Crossgen2Tool.ItemSpec)))
                {
                    Crossgen2Cache.Report(Log, "bypass: unsupported invocation (requires directly executed Linux-x64 or Windows-x64 crossgen2 targeting its host, with non-composite PE output and without compiler warning display, unrecognized extra arguments or environment overrides)");
                    return null;
                }

                Crossgen2CacheInvocation invocation = Crossgen2CacheInvocation.Create(pathToTool,
                    Crossgen2Tool.GetMetadata(MetadataKeys.JitPath), _outputR2RImage, _outputPDBImage,
                    _emitSymbols, Crossgen2Tool.GetMetadata(MetadataKeys.PerfmapFormatVersion));
                return Crossgen2Cache.DescribeInvocation(invocation.CompilerDependencies,
                    _inputAssembly, ImplementationAssemblyReferences, Crossgen2PgoFiles,
                    invocation.Outputs, responseFileCommands, commandLineCommands,
                    string.Join("\n", ShowCompilerWarnings, LogStandardErrorAsError, StandardOutputImportance, StandardErrorImportance),
                    GetWorkingDirectory() ?? Directory.GetCurrentDirectory());
            }
            catch (Exception ex) when (Crossgen2Cache.IsCacheFailure(ex))
            {
                Crossgen2Cache.Report(Log, $"bypass: {ex.Message}");
                return null;
            }
        }

#nullable disable annotations

        private int ExecuteNormally(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            WarningsDetected = false;
            try
            {
                if (!IsPdbCompilation)
                {
                    DeleteOutputFile(_outputR2RImage);
                }

                string symbolOutput = GetOwnedSymbolOutputPath();
                if (!string.IsNullOrEmpty(symbolOutput))
                {
                    DeleteOutputFile(symbolOutput);
                }

                // Crossgen does not create parent directories for its output files.
                Directory.CreateDirectory(Path.GetDirectoryName(_outputR2RImage));
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.LogErrorFromException(ex);
                return -1;
            }

            return ExecuteCompiler(pathToTool, responseFileCommands, commandLineCommands);
        }

        private string GetOwnedSymbolOutputPath()
        {
            if (ActuallyUseCrossgen2 && !Crossgen2IsVersion5 &&
                (!_emitSymbols || Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) == "windows" ||
                 Crossgen2Tool.GetMetadata(MetadataKeys.PerfmapFormatVersion) == "1"))
            {
                return GetSymbolOutputPath(_outputR2RImage, _outputPDBImage,
                    Crossgen2Tool.GetMetadata(MetadataKeys.TargetOS) == "windows");
            }

            return _outputPDBImage;
        }

        internal static string GetSymbolOutputPath(string image, string symbols, bool windows)
        {
            string extension = ".ni.r2rmap";
            if (windows)
            {
                bool nativeImageName = image.EndsWith(".ni.dll", StringComparison.OrdinalIgnoreCase) ||
                    image.EndsWith(".ni.exe", StringComparison.OrdinalIgnoreCase);
                extension = nativeImageName ? ".pdb" : ".ni.pdb";
            }
            string output = Path.ChangeExtension(image, extension);
            return string.IsNullOrEmpty(symbols)
                ? output : Path.Combine(Path.GetDirectoryName(symbols), Path.GetFileName(output));
        }

        private static void DeleteOutputFile(string path)
        {
            try
            {
                File.Delete(path);
            }
            catch (DirectoryNotFoundException)
            {
                // A missing parent already guarantees the output is absent.
            }
        }

        private void ReadCachedTaskOutputs(BinaryReader reader) => WarningsDetected = reader.ReadBoolean();

        private void WriteCachedTaskOutputs(BinaryWriter writer) => writer.Write(WarningsDetected);

        internal virtual int ExecuteCompiler(string pathToTool, string responseFileCommands, string commandLineCommands)
            => base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);

        // Extra arguments can name hidden input files or override the modeled output paths, which the cache
        // cannot fingerprint from the argument string alone. These are the real-world (in-repo) extra arguments
        // known to be plain, pathless boolean switches, so they are safe to allow through unchanged: the full
        // argument text still participates in the cache key, so a different value still produces a different entry.
        private static readonly string[] s_safeExtraArgumentNames =
        {
            "--target-allows-runtime-code-generation",
            "--embed-pgo-data",
            "--verify-type-and-field-layout",
            "--enable-cached-interface-dispatch-support",
        };

        private static bool HasUnsafeExtraArguments(string arguments)
        {
            if (string.IsNullOrEmpty(arguments))
            {
                return false;
            }

            foreach (string argument in arguments.Split(';', StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmed = argument.Trim();
                if (trimmed.Length == 0)
                {
                    continue;
                }

                int colon = trimmed.IndexOf(':');
                string name = colon < 0 ? trimmed : trimmed.Substring(0, colon);
                string value = colon < 0 ? null : trimmed.Substring(colon + 1);

                if (Array.IndexOf(s_safeExtraArgumentNames, name) < 0)
                {
                    return true;
                }

                // These are boolean-only switches: anything other than a bare flag or an explicit
                // true/false value is an unrecognized shape, so bypass rather than risk caching it.
                if (value is not null && value != "true" && value != "false")
                {
                    return true;
                }
            }

            return false;
        }

        protected override void LogEventsFromTextOutput(string singleLine, MessageImportance messageImportance)
        {
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
