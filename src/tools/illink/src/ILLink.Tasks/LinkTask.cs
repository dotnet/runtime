// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
    public class ILLink : ToolTask
    {
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
        /// Gets or sets physical source roots with stable MappedPath metadata for cache identity.
        /// Eligible single-root invocations use root-relative execution paths.
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

        protected override string GetWorkingDirectory() => _executionWorkingDirectory ?? base.GetWorkingDirectory();

        protected override int ExecuteTool(string pathToTool, string responseFileCommands, string commandLineCommands)
        {
            DateTime outputTimestampUtc = DateTime.UtcNow;
            ILLinkCache cache = ILLinkCache.TryCreateFromEnvironment(Log);
            string inputHash = null;
            (string WorkingDirectory, string CommandLine, string ResponseFile)? invocation = null;
            if (cache is not null && !TryComputeCacheKey(pathToTool, commandLineCommands, responseFileCommands, out inputHash, out invocation))
                cache = null;

            if (cache is not null && cache.TryRestore(inputHash, OutputDirectory.ItemSpec, outputTimestampUtc))
                return 0;

            try
            {
                string outputDirectory = Path.GetFullPath(OutputDirectory.ItemSpec);
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
                if (invocation is { } relative)
                {
                    _executionWorkingDirectory = relative.WorkingDirectory;
                    pathToTool = Path.GetFullPath(pathToTool);
                    commandLineCommands = relative.CommandLine;
                    responseFileCommands = relative.ResponseFile;
                }
                exitCode = base.ExecuteTool(pathToTool, responseFileCommands, commandLineCommands);
            }
            finally
            {
                _executionWorkingDirectory = previousWorkingDirectory;
            }
            if (exitCode == 0 && !Log.HasLoggedErrors && cache is not null)
                cache.Store(inputHash, OutputDirectory.ItemSpec);

            return exitCode;
        }

        private bool TryComputeCacheKey(string pathToTool, string commandLineCommands, string responseFileCommands,
            out string inputHash, out (string WorkingDirectory, string CommandLine, string ResponseFile)? invocation)
        {
            inputHash = string.Empty;
            invocation = null;
            if (CustomSteps?.Length > 0 || CustomData?.Length > 0 || DumpDependencies ||
                !string.IsNullOrEmpty(DependenciesFileFormat) || EnvironmentVariables?.Length > 0)
            {
                Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: custom steps/data, dependency dumps, and task environment overrides are not supported.");
                return false;
            }
            if (RootAssemblyNames.Any(root => HasPathSyntax(root.ItemSpec) ||
                root.ItemSpec.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) ||
                root.ItemSpec.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
            {
                Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: roots must be assembly names, not file paths.");
                return false;
            }

            try
            {
                var files = new SortedSet<string>(StringComparer.Ordinal);
                var directories = new SortedSet<string>(StringComparer.Ordinal);
                if (!TryAddCacheableExtraArgsInputs(files, directories, out List<(string Option, string Path)> extraPaths))
                {
                    Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: extra arguments are not supported.");
                    return false;
                }

                var paths = new CachePathMap(SourceRoots);
                if (paths.ExecutionRoot is not null && !Directory.Exists(paths.ExecutionRoot))
                {
                    Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: the execution SourceRoot directory does not exist.");
                    return false;
                }
                string workingDirectory = GetWorkingDirectory();
                if (!string.IsNullOrEmpty(workingDirectory) && Path.GetFullPath(workingDirectory) != Environment.CurrentDirectory)
                {
                    Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: an overridden working directory is not supported.");
                    return false;
                }
                if (paths.ExecutionRoot is not null)
                {
                    string previousWorkingDirectory = _executionWorkingDirectory;
                    try
                    {
                        _executionWorkingDirectory = paths.ExecutionRoot;
                        if (Path.GetFullPath(GetWorkingDirectory() ?? Environment.CurrentDirectory)
                            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) != paths.ExecutionRoot)
                        {
                            Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: an overridden working directory ignores the execution SourceRoot.");
                            return false;
                        }
                    }
                    finally
                    {
                        _executionWorkingDirectory = previousWorkingDirectory;
                    }
                }
                // ToolTask adds a leading space and adjusts slashes before ExecuteTool.
                // Do not ignore arguments supplied by an overridden command generator.
                if (commandLineCommands != AdjustCommandsForOperatingSystem(" " + Quote(ILLinkPath)) ||
                    responseFileCommands != AdjustCommandsForOperatingSystem(GenerateResponseFileCommandsCore(null, ExtraArgs)))
                {
                    Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: overridden command generation is not supported.");
                    return false;
                }

                var assemblies = new HashSet<string>(StringComparer.Ordinal);
                var xmlFiles = new HashSet<string>(StringComparer.Ordinal);
                foreach (ITaskItem assembly in AssemblyPaths)
                {
                    if (!AddAssembly(assembly.ItemSpec))
                        return false;
                }
                foreach (ITaskItem assembly in ReferenceAssemblyPaths ?? Array.Empty<ITaskItem>())
                {
                    if (!AddAssembly(assembly.ItemSpec, checkOutputPaths: false))
                        return false;
                }
                foreach (ITaskItem descriptor in RootDescriptorFiles ?? Array.Empty<ITaskItem>())
                {
                    if (!AddXmlFile(descriptor.ItemSpec))
                        return false;
                }
                foreach (var argument in extraPaths)
                {
                    if (argument.Option != "-d" && !AddXmlFile(argument.Path))
                        return false;
                }

                // Keep search-directory ordering in the arguments; hash even shadowed candidates.
                foreach (string directory in directories)
                {
                    foreach (string file in Directory.EnumerateFiles(directory))
                    {
                        string extension = Path.GetExtension(file);
                        if (extension.Equals(".dll", StringComparison.OrdinalIgnoreCase) ||
                            extension.Equals(".exe", StringComparison.OrdinalIgnoreCase) ||
                            extension.Equals(".winmd", StringComparison.OrdinalIgnoreCase))
                        {
                            if (!AddAssembly(file))
                                return false;
                        }
                    }
                }

#pragma warning disable IL3000 // MSBuild tasks are loaded from assemblies on disk.
                string taskAssemblyPath = typeof(ILLink).Assembly.Location;
#pragma warning restore IL3000
                AddFile(taskAssemblyPath);
                AddFile(pathToTool);
                AddFile(ILLinkPath);
                var toolFiles = new HashSet<string>(StringComparer.Ordinal)
                {
                    Path.GetFullPath(pathToTool),
                    Path.GetFullPath(ILLinkPath),
                    Path.GetFullPath(taskAssemblyPath)
                };

                Func<string, string> renderPath = paths.ExecutionRoot is not null ? paths.MapForExecution : paths.Map;
                var cacheableExtraArgs = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(ExtraArgs))
                {
                    cacheableExtraArgs.AppendLine("--ignore-link-attributes true");
                    foreach (var argument in extraPaths)
                        cacheableExtraArgs.Append(argument.Option).Append(' ').AppendLine(Quote(renderPath(argument.Path)));
                }
                string modeledCommandLine = AdjustCommandsForOperatingSystem(" " + Quote(renderPath(ILLinkPath)));
                string modeledResponseFile = AdjustCommandsForOperatingSystem(GenerateResponseFileCommandsCore(renderPath, cacheableExtraArgs.ToString()));
                using var sha256 = SHA256.Create();
                using var description = new MemoryStream();
                using var writer = new BinaryWriter(description, Encoding.UTF8, leaveOpen: true);
                writer.Write("ILLink task cache key v3: relative execution paths");
                writer.Write(paths.ExecutionRoot is not null);
                writer.Write(paths.Map(ILLinkPath));
                writer.Write(modeledCommandLine);
                writer.Write(modeledResponseFile);
                writer.Write(paths.Map(paths.ExecutionRoot ?? Environment.CurrentDirectory));
                writer.Write(paths.Map(OutputDirectory.ItemSpec));
                writer.Write(RuntimeInformation.OSDescription);
                writer.Write(RuntimeInformation.ProcessArchitecture.ToString());
                writer.Write(CultureInfo.CurrentCulture.Name);
                writer.Write(CultureInfo.CurrentUICulture.Name);
                writer.Write(files.Count);
                var logicalFiles = new SortedDictionary<string, string>(StringComparer.Ordinal);
                foreach (string file in files)
                    logicalFiles.Add(paths.Map(file), file);
                foreach (var file in logicalFiles)
                {
                    writer.Write(file.Key);
                    using var input = File.OpenRead(file.Value);
                    if (!assemblies.Contains(file.Value) && !toolFiles.Contains(file.Value))
                    {
                        paths.CheckForPhysicalRoots(input, 0, input.Length);
                    }
                    writer.Write(sha256.ComputeHash(input));
                }

                writer.Flush();
                description.Position = 0;
#if NET
                inputHash = Convert.ToHexStringLower(sha256.ComputeHash(description));
#else
                inputHash = BitConverter.ToString(sha256.ComputeHash(description)).Replace("-", "").ToLowerInvariant();
#endif
                if (paths.ExecutionRoot is not null)
                    invocation = (paths.ExecutionRoot, modeledCommandLine, modeledResponseFile);
                return true;

                void AddFile(string path) => files.Add(Path.GetFullPath(path));

                bool AddXmlFile(string path)
                {
                    path = Path.GetFullPath(path);
                    AddFile(path);
                    if (!xmlFiles.Add(path))
                        return true;
                    using var reader = XmlReader.Create(path, new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null });
                    while (reader.Read())
                    {
                        if (reader.NodeType != XmlNodeType.Element || reader.LocalName != "assembly")
                            continue;
                        string fullName = reader.GetAttribute("fullname");
                        if (fullName is not null && HasPathSyntax(fullName.Split(',')[0].Trim()))
                        {
                            Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: Path-shaped assembly names in XML can resolve outside the input inventory.");
                            return false;
                        }
                    }
                    return true;
                }

                void AddOptionalFile(string path)
                {
                    if (File.Exists(path))
                        AddFile(path);
                }

                bool AddAssembly(string path, bool checkOutputPaths = true)
                {
                    path = Path.GetFullPath(path);
                    if (!assemblies.Add(path))
                        return true;

                    AddFile(path);
                    AddOptionalFile(path + ".config");
                    AddOptionalFile(path + ".mdb");
                    AddOptionalFile(Path.ChangeExtension(path, ".pdb"));
                    string directory = Path.GetDirectoryName(path);
                    string satelliteName = Path.GetFileNameWithoutExtension(path) + ".resources.dll";
                    foreach (string satelliteDirectory in Directory.EnumerateDirectories(directory))
                    {
                        string satellite = Path.Combine(satelliteDirectory, satelliteName);
                        if (File.Exists(satellite) && !AddAssembly(satellite, checkOutputPaths))
                            return false;
                    }

                    using var input = File.OpenRead(path);
                    using var pe = new PEReader(input);
                    if (!pe.HasMetadata)
                        throw new BadImageFormatException("Input PE image has no managed metadata.", path);
                    MetadataReader metadata = pe.GetMetadataReader();
                    if (!metadata.IsAssembly ||
                        metadata.GetString(metadata.GetAssemblyDefinition().Name) != Path.GetFileNameWithoutExtension(path))
                        throw new NotSupportedException("Assembly names must match their filenames for shared-cache action identity.");
                    if (metadata.AssemblyFiles.Count != 0)
                        throw new NotSupportedException("Additional modules and linked resources are not supported by the shared cache.");
                    foreach (AssemblyReferenceHandle reference in metadata.AssemblyReferences)
                    {
                        if (HasPathSyntax(metadata.GetString(metadata.GetAssemblyReference(reference).Name)))
                        {
                            Log.LogMessage(MessageImportance.Low, "ILLink cache bypassed: Path-shaped assembly references can resolve outside the input inventory.");
                            return false;
                        }
                    }
                    using (var scan = File.OpenRead(path))
                    {
                        paths.CheckForPhysicalRoots(scan,
                            checkOutputPaths ? 0 : pe.PEHeaders.MetadataStartOffset,
                            checkOutputPaths ? scan.Length : pe.PEHeaders.MetadataSize);
                    }
                    if (checkOutputPaths)
                        CheckCacheableSymbols(path, pe, paths);
                    return true;
                }
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException or BadImageFormatException or InvalidDataException or XmlException)
            {
                Log.LogMessage(MessageImportance.Low, $"ILLink cache bypassed: input identity could not be computed: {ex.Message}");
                return false;
            }
        }

        private static bool HasPathSyntax(string name) => name.IndexOf('/') >= 0 || name.IndexOf('\\') >= 0 || name.IndexOf(':') >= 0;

        private bool TryAddCacheableExtraArgsInputs(SortedSet<string> files, SortedSet<string> directories, out List<(string Option, string Path)> extraPaths)
        {
            extraPaths = new();
            ReadOnlySpan<char> arguments = ExtraArgs.AsSpan().Trim();
            if (arguments.IsEmpty)
                return true;

            // The runtime library builds pass this policy through ExtraArgs. Keep the
            // supported shape narrow so arbitrary linker arguments continue to bypass caching.
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

                if (option == "-d")
                    directories.Add(Path.GetFullPath(path));
                else
                    files.Add(Path.GetFullPath(path));
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

        private void CheckCacheableSymbols(string assemblyPath, PEReader pe, CachePathMap paths)
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
                        paths.CheckSymbolPath(pe.ReadCodeViewDebugDirectoryData(entry).Path);
                        break;
                    case DebugDirectoryEntryType.EmbeddedPortablePdb:
                        using (MetadataReaderProvider provider = pe.ReadEmbeddedPortablePdbDebugDirectoryData(entry))
                            CheckDocuments(provider.GetMetadataReader(), paths);
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
                CheckDocuments(provider.GetMetadataReader(), paths);
            }

            static void CheckDocuments(MetadataReader metadata, CachePathMap paths)
            {
                foreach (DocumentHandle handle in metadata.Documents)
                    paths.CheckSymbolPath(metadata.GetString(metadata.GetDocument(handle).Name));
                foreach (CustomDebugInformationHandle handle in metadata.CustomDebugInformation)
                    paths.CheckForPhysicalRoots(metadata.GetBlobBytes(metadata.GetCustomDebugInformation(handle).Value));
            }
        }

        private sealed class CachePathMap
        {
            private readonly List<(string Physical, string Logical)> _roots = new();
            private readonly List<byte[]> _physicalPrefixes = new();
            internal string ExecutionRoot { get; }
            private static StringComparison PathComparison => Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            internal CachePathMap(ITaskItem[] sourceRoots)
            {
                if (sourceRoots is null || sourceRoots.Length == 0)
                    throw new NotSupportedException("SourceRoots with stable MappedPath metadata are required for the shared cache.");
                foreach (ITaskItem root in sourceRoots)
                {
                    if (!Path.IsPathRooted(root.ItemSpec))
                        throw new NotSupportedException("SourceRoots must be absolute.");
                    string physical = Path.GetFullPath(root.ItemSpec).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string logical = root.GetMetadata("MappedPath");
                    if (physical.Length == 0 || physical.Equals(Path.GetPathRoot(physical), PathComparison) ||
                        !IsLogicalPath(logical) || !logical.EndsWith("/", StringComparison.Ordinal))
                        throw new NotSupportedException("SourceRoots require stable /_/ or /_N/ MappedPath prefixes.");
                    foreach (var previous in _roots)
                    {
                        if (physical.Equals(previous.Physical, PathComparison) ||
                            logical.Equals(previous.Logical, StringComparison.Ordinal))
                            throw new NotSupportedException("Duplicate physical or logical SourceRoots are not supported.");
                        CheckNested(physical, logical, previous.Physical, previous.Logical);
                        CheckNested(previous.Physical, previous.Logical, physical, logical);
                    }
                    _roots.Add((physical, logical));
                    foreach (string prefix in new[] { physical, physical.Replace('\\', '/') }.Distinct(StringComparer.Ordinal))
                    {
                        _physicalPrefixes.Add(Encoding.UTF8.GetBytes(prefix));
                        _physicalPrefixes.Add(Encoding.Unicode.GetBytes(prefix));
                    }
                }
                _roots.Sort((left, right) => right.Physical.Length.CompareTo(left.Physical.Length));
                string outerRoot = _roots[_roots.Count - 1].Physical;
                if (_roots.All(root => root.Physical.Equals(outerRoot, PathComparison) ||
                    root.Physical.StartsWith(outerRoot + Path.DirectorySeparatorChar, PathComparison)))
                    ExecutionRoot = outerRoot;

                static void CheckNested(string child, string childLogical, string parent, string parentLogical)
                {
                    if (child.StartsWith(parent + Path.DirectorySeparatorChar, PathComparison) &&
                        childLogical != parentLogical + child.Substring(parent.Length + 1).Replace('\\', '/') + "/")
                        throw new NotSupportedException("Nested SourceRoots must preserve their relative layout.");
                }
            }

            internal string Map(string path)
            {
                path = GetValidatedFullPath(path);
                foreach (var root in _roots)
                {
                    if (path.TrimEnd(Path.DirectorySeparatorChar).Equals(root.Physical, PathComparison))
                        return "mapped:" + root.Logical;
                    if (path.StartsWith(root.Physical + Path.DirectorySeparatorChar, PathComparison))
                        return "mapped:" + root.Logical + path.Substring(root.Physical.Length + 1).Replace(Path.DirectorySeparatorChar, '/');
                }
                return "absolute:" + path;
            }

            internal string MapForExecution(string path)
            {
                // Resolve against the caller's cwd before changing the child process's cwd.
                path = GetValidatedFullPath(path);
                if (path.TrimEnd(Path.DirectorySeparatorChar).Equals(ExecutionRoot, PathComparison))
                    return ".";
                if (path.StartsWith(ExecutionRoot + Path.DirectorySeparatorChar, PathComparison))
                    return path.Substring(ExecutionRoot.Length + 1);
                return path;
            }

            private static string GetValidatedFullPath(string path)
            {
                path = Path.GetFullPath(path);
                if (path.Length > Path.GetPathRoot(path).Length)
                    path = path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (path.IndexOfAny(new[] { '"', '\r', '\n' }) >= 0 ||
                    (Path.DirectorySeparatorChar == '/' && path.IndexOf('\\') >= 0))
                    throw new NotSupportedException("Quoted or multiline paths are not supported by the shared cache.");
                return path;
            }

            internal void CheckSymbolPath(string path)
            {
                if (IsLogicalPath(path))
                    return;
                if (path.StartsWith("/", StringComparison.Ordinal) || path.StartsWith("\\", StringComparison.Ordinal) ||
                    (path.Length > 1 && path[1] == ':') || path.Split('/', '\\').Any(part => part == ".."))
                    throw new NotSupportedException($"Physical or escaping symbol path '{path}' is not supported by the shared cache.");
            }

            internal void CheckForPhysicalRoots(ReadOnlySpan<byte> bytes)
            {
                foreach (byte[] prefix in _physicalPrefixes)
                {
                    if (bytes.IndexOf(prefix) >= 0)
                        throw new NotSupportedException("An input contains a physical SourceRoot path.");
                }
            }

            internal void CheckForPhysicalRoots(Stream input, long offset, long length)
            {
                int overlap = _physicalPrefixes.Max(prefix => prefix.Length) - 1;
                byte[] buffer = ArrayPool<byte>.Shared.Rent(Math.Max(64 * 1024, checked(2 * overlap + 1)));
                long originalPosition = input.Position;
                try
                {
                    input.Position = offset;
                    int retained = 0;
                    while (length > 0)
                    {
                        int read = input.Read(buffer, retained, (int)Math.Min(length, buffer.Length - retained));
                        if (read == 0)
                            throw new EndOfStreamException("An input changed while checking shared-cache paths.");
                        int count = retained + read;
                        CheckForPhysicalRoots(buffer.AsSpan(0, count));
                        retained = Math.Min(overlap, count);
                        Buffer.BlockCopy(buffer, count - retained, buffer, 0, retained);
                        length -= read;
                    }
                }
                finally
                {
                    input.Position = originalPosition;
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }

            private static bool IsLogicalPath(string path)
            {
                if (!path.StartsWith("/_", StringComparison.Ordinal))
                    return false;
                int index = 2;
                while (index < path.Length && path[index] is >= '0' and <= '9')
                    index++;
                return index < path.Length && path[index] == '/' &&
                    path.IndexOf('\\') < 0 && !path.Split('/').Any(part => part is "." or "..");
            }
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

        private static string Quote(string path)
        {
            return $"\"{path.TrimEnd('\\')}\"";
        }

        protected override string GenerateCommandLineCommands()
        {
            var args = new StringBuilder();
            var path = ILLinkPath;
            args.Append(Quote(path));
            Log.LogMessage(MessageImportance.Normal, $"ILLink.Tasks path: {path}");

            return args.ToString();
        }

        private static void SetOpt(StringBuilder args, string opt, bool enabled)
        {
            args.Append(enabled ? "--enable-opt " : "--disable-opt ").AppendLine(opt);
        }

        private static void SetOpt(StringBuilder args, string opt, string assembly, bool enabled)
        {
            args.Append(enabled ? "--enable-opt " : "--disable-opt ").Append(opt).Append(' ').AppendLine(assembly);
        }

        protected override string GenerateResponseFileCommands() => GenerateResponseFileCommandsCore(null, ExtraArgs);

        private string GenerateResponseFileCommandsCore(Func<string, string> mapPath, string extraArgs)
        {
            var args = new StringBuilder();

            if (RootDescriptorFiles != null)
            {
                foreach (var rootFile in RootDescriptorFiles)
                    args.Append("-x ").AppendLine(Quote(mapPath?.Invoke(rootFile.ItemSpec) ?? rootFile.ItemSpec));
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

                args.Append("-reference ").AppendLine(Quote(mapPath?.Invoke(assemblyPath) ?? assemblyPath));

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

                    args.Append("-reference ").AppendLine(Quote(mapPath?.Invoke(assemblyPath) ?? assemblyPath));

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

            if (extraArgs != null)
                args.AppendLine(extraArgs);

            if (DumpDependencies)
                args.AppendLine("--dump-dependencies");

            if (DependenciesFileFormat != null)
            {
                args.Append("--dependencies-file-format ").AppendLine(DependenciesFileFormat);
            }

            // Keep the tool's output directory consistent with the directory owned by the task.
            if (OutputDirectory is not null)
                args.Append("-out ").AppendLine(Quote(mapPath?.Invoke(OutputDirectory.ItemSpec) ?? OutputDirectory.ItemSpec));

            return args.ToString();
        }
    }
}
