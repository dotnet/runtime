// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using Microsoft.Build.Framework;
using Microsoft.NET.Build.Tasks;

namespace ILLink.Tasks
{
    public class ILLink : CachedToolTask
    {
        private static readonly InvocationCacheConfiguration s_cacheConfiguration =
            new("ILLink", "ILLINK_EXPERIMENTAL_CACHE", "ILLINK_EXPERIMENTAL_CACHE_PATH");

        /// <summary>
        ///   Paths to the assembly files that should be considered as
        ///   input to the ILLink.
        ///   Optional metadata:
        ///       TrimMode ("copy", "link", etc...): sets the illink action to take for this assembly.
        ///   There is an optional metadata for each optimization that can be set to "True" or "False" to
        ///   enable or disable it per-assembly:
        ///       BeforeFieldInit
        ///       OverrideRemoval
        ///       UnreachableBodies
        ///       UnusedInterfaces
        ///       IPConstProp
        ///       Sealer
        ///   Optional metadata "TrimmerSingleWarn" may also be set to "True"/"False" to control
        ///   whether ILLink produces granular warnings for this assembly.
        ///   Maps to '-reference', and possibly '--action', '--enable-opt', '--disable-opt', '--verbose'
        /// </summary>
        [Required]
        public ITaskItem[] AssemblyPaths { get; set; }

        /// <summary>
        ///    Paths to assembly files that are reference assemblies,
        ///    representing the surface area for compilation.
        ///    Maps to '-reference', with action set to 'skip' via '--action'.
        /// </summary>
        public ITaskItem[] ReferenceAssemblyPaths { get; set; }

        /// <summary>
        ///   The names of the assemblies to root. This should contain
        ///   assembly names without an extension, not file names or
        ///   paths. The default is to root everything in these assemblies.
        //    For more fine-grained control, set RootMode metadata.
        /// </summary>
        [Required]
        public ITaskItem[] RootAssemblyNames { get; set; }

        /// <summary>
        ///   The directory in which to place linked assemblies.
        ///   The task deletes and recreates this directory before running ILLink.
        ///   It must not contain inputs or files that need to be preserved.
        ///    Maps to '-out'.
        /// </summary>
        [Required]
        public ITaskItem OutputDirectory { get; set; }

        /// <summary>
        /// Gets or sets physical source roots for root-relative execution paths.
        /// Stable MappedPath metadata is additionally required for cache identity.
        /// These mappings do not normalize paths embedded in inputs or outputs.
        /// </summary>
        public ITaskItem[] SourceRoots { get; set; }

        /// <summary>
        /// The subset of warnings that have to be turned off.
        /// Maps to '--nowarn'.
        /// </summary>
        public string NoWarn { get; set; }

        /// <summary>
        /// The warning version to use.
        /// Maps to '--warn'.
        /// </summary>
        public string Warn { get; set; }

        /// <summary>
        /// Treat all warnings as errors.
        /// Maps to '--warnaserror' if true, '--warnaserror-' if false.
        /// </summary>
        public bool TreatWarningsAsErrors { set => _treatWarningsAsErrors = value; }
        bool? _treatWarningsAsErrors;

        /// <summary>
        /// Produce at most one trim analysis warning per assembly.
        /// Maps to '--singlewarn' if true, '--singlewarn-' if false.
        /// </summary>
        public bool SingleWarn { set => _singleWarn = value; }
        bool? _singleWarn;

        /// <summary>
        /// The list of warnings to report as errors.
        /// Maps to '--warnaserror LIST-OF-WARNINGS'.
        /// </summary>
        public string WarningsAsErrors { get; set; }

        /// <summary>
        /// The list of warnings to report as usual.
        /// Maps to '--warnaserror- LIST-OF-WARNINGS'.
        /// </summary>
        public string WarningsNotAsErrors { get; set; }

        /// <summary>
        ///   A list of XML root descriptor files specifying ILLink
        ///   roots at a granular level. See the dotnet/linker
        ///   documentation for details about the format.
        ///   Maps to '-x'.
        /// </summary>
        public ITaskItem[] RootDescriptorFiles { get; set; }

        /// <summary>
        ///   Boolean specifying whether to enable beforefieldinit optimization globally.
        ///   Maps to '--enable-opt beforefieldinit' or '--disable-opt beforefieldinit'.
        /// </summary>
        public bool BeforeFieldInit { set => _beforeFieldInit = value; }
        bool? _beforeFieldInit;

        /// <summary>
        ///   Boolean specifying whether to enable overrideremoval optimization globally.
        ///   Maps to '--enable-opt overrideremoval' or '--disable-opt overrideremoval'.
        /// </summary>
        public bool OverrideRemoval { set => _overrideRemoval = value; }
        bool? _overrideRemoval;

        /// <summary>
        ///   Boolean specifying whether to enable unreachablebodies optimization globally.
        ///   Maps to '--enable-opt unreachablebodies' or '--disable-opt unreachablebodies'.
        /// </summary>
        public bool UnreachableBodies { set => _unreachableBodies = value; }
        bool? _unreachableBodies;

        /// <summary>
        ///   Boolean specifying whether to enable unusedinterfaces optimization globally.
        ///   Maps to '--enable-opt unusedinterfaces' or '--disable-opt unusedinterfaces'.
        /// </summary>
        public bool UnusedInterfaces { set => _unusedInterfaces = value; }
        bool? _unusedInterfaces;

        /// <summary>
        ///   Boolean specifying whether to enable ipconstprop optimization globally.
        ///   Maps to '--enable-opt ipconstprop' or '--disable-opt ipconstprop'.
        /// </summary>
        public bool IPConstProp { set => _iPConstProp = value; }
        bool? _iPConstProp;

        /// <summary>
        ///   A list of feature names used by the body substitution logic.
        ///   Each Item requires "Value" boolean metadata with the value of
        ///   the feature setting.
        ///   Maps to '--feature'.
        /// </summary>
        public ITaskItem[] FeatureSettings { get; set; }

        /// <summary>
        ///   Boolean specifying whether to enable sealer optimization globally.
        ///   Maps to '--enable-opt sealer' or '--disable-opt sealer'.
        /// </summary>
        public bool Sealer { set => _sealer = value; }
        bool? _sealer;

        static readonly string[] _optimizationNames = new string[] {
            "BeforeFieldInit",
            "OverrideRemoval",
            "UnreachableBodies",
            "UnusedInterfaces",
            "IPConstProp",
            "Sealer"
        };

        /// <summary>
        ///   Custom data key-value pairs to pass to ILLink.
        ///   The name of the item is the key, and the required "Value"
        ///   metadata is the value. Maps to '--custom-data key=value'.
        /// </summary>
        public ITaskItem[] CustomData { get; set; }

        /// <summary>
        ///   Extra arguments to pass to illink, delimited by spaces.
        /// </summary>
        public string ExtraArgs { get; set; }

        /// <summary>
        ///   Make illink dump dependencies file for ILLink analyzer tool.
        ///   Maps to '--dump-dependencies'.
        /// </summary>
        public bool DumpDependencies { get; set; }

        /// <summary>
        ///   Make illink dump dependencies to the specified file type.
        ///   Maps to '--dependencies-file-format'.
        /// </summary>
        public string DependenciesFileFormat { get; set; }

        /// <summary>
        ///   Remove debug symbols from linked assemblies.
        ///   Maps to '-b' if false.
        ///   Default if not specified is to remove symbols, like
        ///   the command-line. (Target files will likely set their own defaults to keep symbols.)
        /// </summary>
        public bool RemoveSymbols { set => _removeSymbols = value; }
        bool? _removeSymbols;

        /// <summary>
        ///   Preserve original path to debug symbols from each assembly's debug header.
        ///   Maps to '--preserve-symbol-paths' if true.
        ///   Default if not specified is to write out the full path to the pdb in the debug header.
        /// </summary>
        public bool PreserveSymbolPaths { get; set; }

        /// <summary>
        ///   Sets the default action for trimmable assemblies.
        ///   Maps to '--trim-mode'
        /// </summary>
        public string TrimMode { get; set; }

        /// <summary>
        ///   Sets the default action for assemblies which have not opted into trimming.
        ///   Maps to '--action'
        /// </summary>
        public string DefaultAction { get; set; }

        /// <summary>
        ///   A list of custom steps to insert into the ILLink pipeline.
        ///   Each ItemSpec should be the path to the assembly containing the custom step.
        ///   Each Item requires "Type" metadata with the name of the custom step type.
        ///   Optional metadata:
        ///   BeforeStep: The name of a ILLink step. The custom step will be inserted before it.
        ///   AfterStep: The name of a ILLink step. The custom step will be inserted after it.
        ///   The default (if neither BeforeStep or AfterStep is specified) is to insert the
        ///   custom step at the end of the pipeline.
        ///   It is an error to specify both BeforeStep and AfterStep.
        ///   Maps to '--custom-step'.
        /// </summary>
        public ITaskItem[] CustomSteps { get; set; }

        /// <summary>
        ///   A list selected metadata which should not be trimmed. It maps to 'keep-metadata' option
        /// </summary>
        public ITaskItem[] KeepMetadata { get; set; }

        private const string DotNetHostPathEnvironmentName = "DOTNET_HOST_PATH";

        private string _dotnetPath;

        private string DotNetPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_dotnetPath))
                    return _dotnetPath;

                _dotnetPath = Environment.GetEnvironmentVariable(DotNetHostPathEnvironmentName);
                if (string.IsNullOrEmpty(_dotnetPath))
                    throw new InvalidOperationException($"{DotNetHostPathEnvironmentName} is not set");

                return _dotnetPath;
            }
        }


        /// ToolTask implementation

        protected override MessageImportance StandardErrorLoggingImportance => MessageImportance.High;

        protected override string ToolName => Path.GetFileName(DotNetPath);

        protected override string GenerateFullPathToTool() => DotNetPath;

        private string _executionWorkingDirectory;
        private InvocationBuilder _invocation;
        private string _pendingResponseFile;

        protected override string GetWorkingDirectory() => _executionWorkingDirectory ?? base.GetWorkingDirectory();

        private protected override InvocationCacheConfiguration CacheConfiguration => s_cacheConfiguration;

        protected override int ExecuteToolCore(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            try
            {
                string outputDirectory = _invocation?.Paths is { } paths
                    ? paths.Resolve(_invocation.OutputDirectory)
                    : Path.GetFullPath(OutputDirectory.ItemSpec);
                if (Directory.Exists(outputDirectory))
                    Directory.Delete(outputDirectory, recursive: true);

                Directory.CreateDirectory(outputDirectory);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                Log.LogError($"Could not prepare ILLink output directory '{OutputDirectory.ItemSpec}': {ex.Message}");
                return -1;
            }

            int exitCode;
            string previousWorkingDirectory = _executionWorkingDirectory;
            try
            {
                if (_invocation?.Paths?.ExecutionRoot is { } executionRoot)
                {
                    _executionWorkingDirectory = executionRoot;
                    pathToTool = Path.GetFullPath(pathToTool);
                }
                exitCode = base.ExecuteToolCore(pathToTool, responseFileCommands, commandLineCommands);
            }
            finally
            {
                _executionWorkingDirectory = previousWorkingDirectory;
            }
            return exitCode;
        }

        private InvocationBuilder CreateInvocationBuilder()
        {
            if (CustomSteps?.Length > 0 || CustomData?.Length > 0 || DumpDependencies ||
                !string.IsNullOrEmpty(DependenciesFileFormat) || EnvironmentVariables?.Length > 0)
            {
                return new InvocationBuilder(ILLinkPath, null, null,
                    "custom steps/data, dependency dumps, and task environment overrides are not supported.");
            }
            try
            {
                if (!TryGetExtraArgsPaths(out List<(string Option, string Path)> extraPaths))
                    return new InvocationBuilder(ILLinkPath, null, null, "extra arguments are not supported.");

                var paths = new SourceRootPathMap(SourceRoots);
                if (paths.ExecutionRoot is not null && !Directory.Exists(paths.ExecutionRoot))
                    return new InvocationBuilder(ILLinkPath, null, null, "the execution SourceRoot directory does not exist.");
                return new InvocationBuilder(ILLinkPath, paths, extraPaths);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return new InvocationBuilder(ILLinkPath, null, null, ex.Message);
            }
        }

        private protected override InvocationDescription DescribeInvocation(
            string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            InvocationBuilder invocation = _invocation;
            if (invocation?.Paths is null)
            {
                Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: the invocation could not be normalized.");
                return null;
            }
            try
            {
                SourceRootPathMap paths = invocation.Paths;
                paths.ValidateMappings();
                var files = new SortedSet<string>(StringComparer.Ordinal);
                var directories = new SortedSet<string>(StringComparer.Ordinal);
                var assemblies = new HashSet<string>(StringComparer.Ordinal);
                foreach (var assembly in invocation.Assemblies)
                {
                    if (!AddAssembly(assembly.Path, assembly.CheckOutputSymbols))
                        return null;
                }
                files.UnionWith(invocation.Files);
                foreach (var argument in invocation.ExtraPaths)
                {
                    if (argument.Option == "-d")
                        directories.Add(argument.Path);
                    else
                        files.Add(argument.Path);
                }

                // Keep search-directory ordering in the arguments; hash even shadowed candidates.
                foreach (string directory in directories)
                {
                    foreach (string file in Directory.EnumerateFiles(paths.Resolve(directory)))
                    {
                        string extension = Path.GetExtension(file);
                        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                            extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                            extension.Equals(".winmd", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!AddAssembly(paths.Map(file)))
                                return null;
                        }
                    }
                }

#pragma warning disable IL3000 // MSBuild tasks are loaded from assemblies on disk.
                string taskAssemblyPath = typeof(ILLink).Assembly.Location;
#pragma warning restore IL3000
                files.Add(paths.Map(taskAssemblyPath));
                files.Add(paths.Map(pathToTool));
                files.Add(invocation.ToolPath);
                foreach (string file in files)
                {
                    // Required inputs must be readable, not represented as absent by shared hashing.
                    using var input = File.OpenRead(paths.Resolve(file));
                }

                return new InvocationDescription(new InvocationInputs(files.ToArray()),
                    new[] { "ILLink", paths.ExecutionRoot is not null ? "relative" : "original",
                        AdjustCommandsForOperatingSystem(" " + invocation.CommandIdentity),
                        AdjustCommandsForOperatingSystem(invocation.ResponseIdentity) },
                    new InvocationOutputs(invocation.OutputDirectory, Array.Empty<string>(), paths),
                    workingDirectory: invocation.WorkingDirectory, pathMap: paths);

                void AddOptionalFile(string path)
                {
                    if (File.Exists(path))
                        files.Add(paths.Map(path));
                }

                bool AddAssembly(string input, bool checkOutputSymbols = true)
                {
                    if (!assemblies.Add(input))
                        return true;

                    files.Add(input);
                    string path = paths.Resolve(input);
                    AddOptionalFile(path + ".config");
                    AddOptionalFile(path + ".mdb");
                    AddOptionalFile(Path.ChangeExtension(path, ".pdb"));
                    string directory = Path.GetDirectoryName(path);
                    string satelliteName = Path.GetFileNameWithoutExtension(path) + ".resources.dll";
                    foreach (string satelliteDirectory in Directory.EnumerateDirectories(directory))
                    {
                        string satellite = Path.Combine(satelliteDirectory, satelliteName);
                        if (File.Exists(satellite) && !AddAssembly(paths.Map(satellite), checkOutputSymbols))
                            return false;
                    }

                    using var stream = File.OpenRead(path);
                    using var pe = new PEReader(stream);
                    if (!pe.HasMetadata)
                        throw new BadImageFormatException("Input PE image has no managed metadata.", path);
                    MetadataReader metadata = pe.GetMetadataReader();
                    foreach (AssemblyReferenceHandle reference in metadata.AssemblyReferences)
                    {
                        if (HasPathSyntax(metadata.GetString(metadata.GetAssemblyReference(reference).Name)))
                        {
                            Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: Path-shaped assembly references can resolve outside the input inventory.");
                            return false;
                        }
                    }
                    if (checkOutputSymbols)
                        CheckCacheableSymbols(path, pe);
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or BadImageFormatException or InvalidDataException)
            {
                Log.LogMessage(MessageImportance.Low, $"ILLink cache bypassed: input identity could not be computed: {ex.Message}");
                return null;
            }
        }

        private static bool HasPathSyntax(string name) => name.AsSpan().IndexOfAny('/', '\\', ':') >= 0;

        private bool TryGetExtraArgsPaths(out List<(string Option, string Path)> extraPaths)
        {
            extraPaths = new();
            ReadOnlySpan<char> arguments = ExtraArgs.AsSpan().Trim();
            if (arguments.IsEmpty)
                return true;

            // This tokenizer does not model the linker's backslash-before-quote escapes.
            if (arguments.IndexOf("\\\"".AsSpan()) >= 0)
                return false;

            // The runtime library builds pass this policy through ExtraArgs. Keep the
            // supported shape narrow so arbitrary linker arguments retain their original invocation.
            if (!TryReadArgument(ref arguments, out string option) ||
                option != "--ignore-link-attributes" ||
                !TryReadArgument(ref arguments, out string value) ||
                value != "true")
            {
                return false;
            }

            while (!arguments.TrimStart().IsEmpty)
            {
                if (!TryReadArgument(ref arguments, out option) ||
                    option is not ("--link-attributes" or "--substitutions" or "-d") ||
                    !TryReadArgument(ref arguments, out string path) ||
                    string.IsNullOrEmpty(path))
                {
                    return false;
                }

                extraPaths.Add((option, path));
            }

            return true;

            static bool TryReadArgument(ref ReadOnlySpan<char> arguments, out string argument)
            {
                arguments = arguments.TrimStart();
                if (arguments.IsEmpty)
                {
                    argument = string.Empty;
                    return false;
                }

                if (arguments[0] == '"')
                {
                    int closingQuote = arguments.Slice(1).IndexOf('"');
                    if (closingQuote < 0)
                    {
                        argument = string.Empty;
                        return false;
                    }

                    argument = arguments.Slice(1, closingQuote).ToString();
                    arguments = arguments.Slice(closingQuote + 2);
                    return arguments.IsEmpty || char.IsWhiteSpace(arguments[0]);
                }

                int end = 0;
                while (end < arguments.Length && !char.IsWhiteSpace(arguments[end]))
                    end++;

                ReadOnlySpan<char> token = arguments.Slice(0, end);
                if (token.IndexOf('"') >= 0)
                {
                    argument = string.Empty;
                    return false;
                }

                argument = token.ToString();
                arguments = arguments.Slice(end);
                return true;
            }
        }

        private void CheckCacheableSymbols(string assemblyPath, PEReader pe)
        {
            if (File.Exists(assemblyPath + ".mdb"))
                throw new NotSupportedException("MDB symbols are not supported by the shared cache.");

            foreach (DebugDirectoryEntry entry in pe.ReadDebugDirectory())
            {
                switch (entry.Type)
                {
                    case DebugDirectoryEntryType.CodeView:
                        if (!entry.IsPortableCodeView)
                            throw new NotSupportedException("Native PDB symbols are not supported by the shared cache.");
                        CheckSymbolPath(pe.ReadCodeViewDebugDirectoryData(entry).Path);
                        break;
                    case DebugDirectoryEntryType.EmbeddedPortablePdb:
                        using (MetadataReaderProvider provider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(entry))
                            CheckDocuments(provider.GetMetadataReader());
                        break;
                    case DebugDirectoryEntryType.Reproducible:
                    case DebugDirectoryEntryType.PdbChecksum:
                        break;
                    default:
                        throw new NotSupportedException($"Debug directory type '{entry.Type}' is not supported by the shared cache.");
                }
            }

            string pdbPath = Path.ChangeExtension(assemblyPath, ".pdb");
            if (File.Exists(pdbPath))
            {
                if (_removeSymbols == false && !PreserveSymbolPaths)
                    throw new NotSupportedException("Portable PDB emission requires explicit PreserveSymbolPaths for the shared cache.");
                using var input = File.OpenRead(pdbPath);
                using var provider = MetadataReaderProvider.FromPortablePdbStream(input);
                CheckDocuments(provider.GetMetadataReader());
            }

            static void CheckDocuments(MetadataReader metadata)
            {
                foreach (DocumentHandle handle in metadata.Documents)
                    CheckSymbolPath(metadata.GetString(metadata.GetDocument(handle).Name));
            }
        }

        private static void CheckSymbolPath(string path)
        {
            if (SourceRootPathMap.IsLogicalPath(path))
                return;
            if ((path.Length > 0 && path[0] is '/' or '\\') ||
                (path.Length > 1 && path[1] == ':') || path.Split('/', '\\').Any(part => part == ".."))
                throw new NotSupportedException($"Physical or escaping symbol path '{path}' is not supported by the shared cache.");
        }

        private string _illinkPath = "";

        public string ILLinkPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_illinkPath))
                    return _illinkPath;

#pragma warning disable IL3000 // Avoid accessing Assembly file path when publishing as a single file
                var taskDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
#pragma warning restore IL3000 // Avoid accessing Assembly file path when publishing as a single file

                // IL Linker always runs on .NET Core, even when using desktop MSBuild to host ILLink.Tasks.
                _illinkPath = Path.Combine(Path.GetDirectoryName(taskDirectory), "net", "illink.dll");

                return _illinkPath;
            }
            set => _illinkPath = value;
        }

        private static string Quote(string path) => InvocationBuilder.Quote(path);

        protected override string GenerateCommandLineCommands()
        {
            _invocation = BuildInvocation();
            _pendingResponseFile = _invocation.ToString();
            Log.LogMessage(MessageImportance.Normal, $"ILLink.Tasks path: {ILLinkPath}");
            return _invocation.CommandLine;
        }

        private static void SetOpt(InvocationBuilder args, string opt, bool enabled)
        {
            args.Append(enabled ? "--enable-opt " : "--disable-opt ").AppendLine(opt);
        }

        private static void SetOpt(InvocationBuilder args, string opt, string assembly, bool enabled)
        {
            args.Append(enabled ? "--enable-opt " : "--disable-opt ").Append(opt).Append(' ').AppendLine(assembly);
        }

        protected override string GenerateResponseFileCommands()
        {
            if (_pendingResponseFile is { } responseFile)
            {
                _pendingResponseFile = null;
                return responseFile;
            }
            _invocation = BuildInvocation();
            return _invocation.ToString();
        }

        private InvocationBuilder BuildInvocation()
        {
            InvocationBuilder args = CreateInvocationBuilder();

            if (RootDescriptorFiles != null)
            {
                foreach (var rootFile in RootDescriptorFiles)
                    args.Append("-x ").AppendFile(rootFile.ItemSpec).AppendLine();
            }

            foreach (var assemblyItem in RootAssemblyNames)
            {
                args.Append("-a ").Append(Quote(assemblyItem.ItemSpec));

                string rootMode = assemblyItem.GetMetadata("RootMode");
                if (!string.IsNullOrEmpty(rootMode))
                {
                    args.Append(' ');
                    args.Append(rootMode);
                }

                args.AppendLine();
            }

            if (_singleWarn is bool generalSingleWarn)
            {
                if (generalSingleWarn)
                    args.AppendLine("--singlewarn");
                else
                    args.AppendLine("--singlewarn-");
            }

            string trimMode = TrimMode switch
            {
                "full" => "link",
                "partial" => "link",
                var x => x
            };
            if (trimMode != null)
                args.Append("--trim-mode ").AppendLine(trimMode);

            string defaultAction = TrimMode switch
            {
                "full" => "link",
                "partial" => "copy",
                _ => DefaultAction
            };
            if (defaultAction != null)
                args.Append("--action ").AppendLine(defaultAction);


            HashSet<string> assemblyNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var assembly in AssemblyPaths)
            {
                var assemblyPath = assembly.ItemSpec;
                var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                // If there are multiple paths with the same assembly name, only use the first one.
                if (!assemblyNames.Add(assemblyName))
                    continue;

                args.Append("-reference ").AppendAssembly(assemblyPath).AppendLine();

                string assemblyTrimMode = assembly.GetMetadata("TrimMode");
                string isTrimmable = assembly.GetMetadata("IsTrimmable");
                if (string.IsNullOrEmpty(assemblyTrimMode))
                {
                    if (isTrimmable.Equals("true", StringComparison.OrdinalIgnoreCase))
                    {
                        // isTrimmable ~= true
                        assemblyTrimMode = trimMode;
                    }
                    else if (isTrimmable.Equals("false", StringComparison.OrdinalIgnoreCase))
                    {
                        // isTrimmable ~= false
                        assemblyTrimMode = "copy";
                    }
                }
                if (!string.IsNullOrEmpty(assemblyTrimMode))
                {
                    args.Append("--action ");
                    args.Append(assemblyTrimMode);
                    args.Append(' ').AppendLine(Quote(assemblyName));
                }

                // Add per-assembly optimization arguments
                foreach (var optimization in _optimizationNames)
                {
                    string optimizationValue = assembly.GetMetadata(optimization);
                    if (string.IsNullOrEmpty(optimizationValue))
                        continue;

                    if (!bool.TryParse(optimizationValue, out bool enabled))
                        throw new ArgumentException($"optimization metadata {optimization} must be True or False");

                    SetOpt(args, optimization, assemblyName, enabled);
                }

                // Add per-assembly verbosity arguments
                string singleWarn = assembly.GetMetadata("TrimmerSingleWarn");
                if (!string.IsNullOrEmpty(singleWarn))
                {
                    if (!bool.TryParse(singleWarn, out bool value))
                        throw new ArgumentException($"TrimmerSingleWarn metadata must be True or False");

                    if (value)
                        args.Append("--singlewarn ").AppendLine(Quote(assemblyName));
                    else
                        args.Append("--singlewarn- ").AppendLine(Quote(assemblyName));
                }
            }

            if (ReferenceAssemblyPaths != null)
            {
                foreach (var assembly in ReferenceAssemblyPaths)
                {
                    var assemblyPath = assembly.ItemSpec;
                    var assemblyName = Path.GetFileNameWithoutExtension(assemblyPath);

                    // Don't process references for which we already have
                    // implementation assemblies.
                    if (assemblyNames.Contains(assemblyName))
                        continue;

                    args.Append("-reference ").AppendAssembly(assemblyPath, checkOutputSymbols: false).AppendLine();

                    // Treat reference assemblies as "skip". Ideally we
                    // would not even look at the IL, but only use them to
                    // resolve surface area.
                    args.Append("--action skip ").AppendLine(Quote(assemblyName));
                }
            }

            if (NoWarn != null)
                args.Append("--nowarn ").AppendLine(Quote(NoWarn));

            if (Warn != null)
                args.Append("--warn ").AppendLine(Quote(Warn));

            if (_treatWarningsAsErrors is bool treatWarningsAsErrors && treatWarningsAsErrors)
                args.Append("--warnaserror ");
            else
                args.Append("--warnaserror- ");

            if (WarningsAsErrors != null)
                args.Append("--warnaserror ").AppendLine(Quote(WarningsAsErrors));

            if (WarningsNotAsErrors != null)
                args.Append("--warnaserror- ").AppendLine(Quote(WarningsNotAsErrors));

            // Add global optimization arguments
            if (_beforeFieldInit is bool beforeFieldInit)
                SetOpt(args, "beforefieldinit", beforeFieldInit);

            if (_overrideRemoval is bool overrideRemoval)
                SetOpt(args, "overrideremoval", overrideRemoval);

            if (_unreachableBodies is bool unreachableBodies)
                SetOpt(args, "unreachablebodies", unreachableBodies);

            if (_unusedInterfaces is bool unusedInterfaces)
                SetOpt(args, "unusedinterfaces", unusedInterfaces);

            if (_iPConstProp is bool iPConstProp)
                SetOpt(args, "ipconstprop", iPConstProp);

            if (_sealer is bool sealer)
                SetOpt(args, "sealer", sealer);

            if (CustomData != null)
            {
                foreach (var customData in CustomData)
                {
                    var key = customData.ItemSpec;
                    var value = customData.GetMetadata("Value");
                    if (string.IsNullOrEmpty(value))
                        throw new ArgumentException("custom data requires \"Value\" metadata");
                    args.Append("--custom-data ").Append(' ').Append(key).Append('=').AppendLine(Quote(value));
                }
            }

            if (FeatureSettings != null)
            {
                foreach (var featureSetting in FeatureSettings)
                {
                    var feature = featureSetting.ItemSpec;
                    var featureValue = featureSetting.GetMetadata("Value");
                    if (string.IsNullOrEmpty(featureValue))
                        throw new ArgumentException("feature settings require \"Value\" metadata");
                    args.Append("--feature ").Append(feature).Append(' ').AppendLine(featureValue);
                }
            }

            if (KeepMetadata != null)
            {
                foreach (var metadata in KeepMetadata)
                    args.Append("--keep-metadata ").AppendLine(Quote(metadata.ItemSpec));
            }

            if (_removeSymbols == false)
                args.AppendLine("-b");

            if (PreserveSymbolPaths)
                args.AppendLine("--preserve-symbol-paths");

            if (CustomSteps != null)
            {
                foreach (var customStep in CustomSteps)
                {
                    args.Append("--custom-step ");
                    var stepPath = customStep.ItemSpec;
                    var stepType = customStep.GetMetadata("Type");
                    if (stepType == null)
                        throw new ArgumentException("custom step requires \"Type\" metadata");
                    var customStepString = $"{stepType},{stepPath}";

                    // handle optional before/aftersteps
                    var beforeStep = customStep.GetMetadata("BeforeStep");
                    var afterStep = customStep.GetMetadata("AfterStep");
                    if (!string.IsNullOrEmpty(beforeStep) && !string.IsNullOrEmpty(afterStep))
                        throw new ArgumentException("custom step may not have both \"BeforeStep\" and \"AfterStep\" metadata");
                    if (!string.IsNullOrEmpty(beforeStep))
                        customStepString = $"-{beforeStep}:{customStepString}";
                    if (!string.IsNullOrEmpty(afterStep))
                        customStepString = $"+{afterStep}:{customStepString}";

                    args.AppendLine(Quote(customStepString));
                }
            }

            args.AppendExtraArgs(ExtraArgs);

            if (DumpDependencies)
                args.AppendLine("--dump-dependencies");

            if (DependenciesFileFormat != null)
            {
                args.Append("--dependencies-file-format ").AppendLine(DependenciesFileFormat);
            }

            // Keep the tool's output directory consistent with the directory owned by the task.
            if (OutputDirectory is not null)
                args.Append("-out ").AppendOutputDirectory(OutputDirectory.ItemSpec).AppendLine();

            if (args.BypassReason is not null && BuildEngine is not null)
                Log.LogMessage(MessageImportance.Low, $"ILLink invocation unchanged: {args.BypassReason}");
            return args;
        }
    }
}
