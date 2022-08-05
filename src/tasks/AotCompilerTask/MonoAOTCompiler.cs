// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Reflection.PortableExecutable;
using System.Text.Json.Serialization;

public class MonoAOTCompiler : Microsoft.Build.Utilities.Task
{
    /// <summary>
    /// Path to AOT cross-compiler binary (mono-aot-cross)
    /// </summary>
    [Required]
    public string CompilerBinaryPath { get; set; } = ""!;

    /// <summary>
    /// Assemblies to be AOTd. They need to be in a self-contained directory.
    ///
    ///  Metadata:
    ///   - AotArguments: semicolon-separated list of options that will be passed to --aot=
    ///   - ProcessArguments: semicolon-separated list of options that will be passed to the AOT compiler itself
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Paths to be passed as MONO_PATH environment variable, when running mono-cross-aot.
    /// These are in addition to the directory containing the assembly being precompiled.
    ///
    /// MONO_PATH=${dir_containing_assembly}:${AdditionalAssemblySearchPaths}
    ///
    /// </summary>
    public string[]? AdditionalAssemblySearchPaths { get; set; }

    /// <summary>
    /// Directory where the AOT'ed files will be emitted
    /// </summary>
    [NotNull]
    [Required]
    public string? OutputDir { get; set; }

    /// <summary>
    /// Target triple passed to the AOT compiler.
    /// </summary>
    public string? Triple { get; set; }

    /// <summary>
    /// Assemblies which were AOT compiled.
    ///
    /// Successful AOT compilation will set the following metadata on the items:
    ///   - AssemblerFile (when using OutputType=AsmOnly)
    ///   - ObjectFile (when using OutputType=Normal)
    ///   - LibraryFile (when using OutputType=Library)
    ///   - AotDataFile (when using UseAotDataFile=true)
    ///   - LlvmObjectFile (if using LLVM)
    ///   - LlvmBitcodeFile (if using LLVM-only)
    /// </summary>
    [Output]
    public ITaskItem[]? CompiledAssemblies { get; set; }

    /// <summary>
    /// Disable parallel AOT compilation
    /// </summary>
    public bool DisableParallelAot { get; set; }

    /// <summary>
    /// Use LLVM for AOT compilation.
    /// The cross-compiler must be built with LLVM support
    /// </summary>
    public bool UseLLVM { get; set; }

    /// <summary>
    /// This instructs the AOT code generator to output certain data constructs into a separate file. This can reduce the executable images some five to twenty percent.
    /// Developers need to then ship the resulting aotdata as a resource and register a hook to load the data on demand by using the mono_install_load_aot_data_hook() method.
    /// Defaults to true.
    /// </summary>
    public bool UseAotDataFile { get; set; } = true;

    /// <summary>
    /// Create an ELF object file (.o) or .s file which can be statically linked into an executable when embedding the mono runtime.
    /// Only valid if OutputType is ObjectFile or AsmOnly.
    /// </summary>
    public bool UseStaticLinking { get; set; }

    /// <summary>
    /// When this option is specified, icalls (internal calls made from the standard library into the mono runtime code) are invoked directly instead of going through the operating system symbol lookup operation.
    /// This requires UseStaticLinking=true.
    /// </summary>
    public bool UseDirectIcalls { get; set; }

    /// <summary>
    /// When this option is specified, P/Invoke methods are invoked directly instead of going through the operating system symbol lookup operation
    /// This requires UseStaticLinking=true.
    /// </summary>
    public bool UseDirectPInvoke { get; set; }

    /// <summary>
    /// Instructs the AOT compiler to emit DWARF debugging information.
    /// </summary>
    public bool UseDwarfDebug { get; set; }

    /// <summary>
    /// File to use for profile-guided optimization, *only* the methods described in the file will be AOT compiled.
    /// </summary>
    public string[]? AotProfilePath { get; set; }

    /// <summary>
    /// Mibc file to use for profile-guided optimization, *only* the methods described in the file will be AOT compiled.
    /// </summary>
    public string[] MibcProfilePath { get; set; } = Array.Empty<string>();

    /// <summary>
    /// List of profilers to use.
    /// </summary>
    public string[]? Profilers { get; set; }

    /// <summary>
    /// Generate a file containing mono_aot_register_module() calls for each AOT module which can be compiled into the app embedding mono.
    /// If set, this implies UseStaticLinking=true.
    /// </summary>
    public string? AotModulesTablePath { get; set; }

    /// <summary>
    /// Source code language of the AOT modules table. Supports "C" or "ObjC".
    /// Defaults to "C".
    /// </summary>
    public string? AotModulesTableLanguage { get; set; } = nameof(MonoAotModulesTableLanguage.C);

    /// <summary>
    /// Choose between 'Normal', 'JustInterp', 'Full', 'FullInterp', 'Hybrid', 'LLVMOnly', 'LLVMOnlyInterp'.
    /// LLVMOnly means to use only LLVM for FullAOT, AOT result will be a LLVM Bitcode file (the cross-compiler must be built with LLVM support)
    /// The "interp" options ('LLVMOnlyInterp' and 'FullInterp') mean generate necessary support to fall back to interpreter if AOT code is not possible for some methods.
    /// The difference between 'JustInterp' and 'FullInterp' is that 'FullInterp' will AOT all the methods in the given assemblies, while 'JustInterp' will only AOT the wrappers and trampolines necessary for the runtime to execute the managed methods using the interpreter and to interoperate with P/Invokes and unmanaged callbacks.
    /// </summary>
    public string Mode { get; set; } = nameof(MonoAotMode.Normal);

    /// <summary>
    /// Choose between 'ObjectFile', 'AsmOnly', 'Library'
    /// ObjectFile means the AOT compiler will produce an .o object file, AsmOnly will produce .s assembly code and Library will produce a .so/.dylib/.dll shared library.
    /// </summary>
    public string OutputType { get; set; } = nameof(MonoAotOutputType.ObjectFile);

    /// <summary>
    /// Choose between 'Dll', 'Dylib', 'So'. Only valid if OutputType is Library.
    /// Dll means the AOT compiler will produce a Windows PE .dll file, Dylib means an Apple Mach-O .dylib and So means a Linux/Android ELF .so file.
    /// </summary>
    public string? LibraryFormat { get; set; }

    /// <summary>
    /// Prefix that will be added to the library file name, e.g. to add 'lib' prefix required by some platforms. Only valid if OutputType is Library.
    /// </summary>
    public string LibraryFilePrefix { get; set; } = "";

    /// <summary>
    /// Path to the directory where LLVM binaries (opt and llc) are found.
    /// It's required if UseLLVM is set
    /// </summary>
    public string? LLVMPath { get; set; }

    /// <summary>
    /// Prepends a prefix to the name of tools ran by the AOT compiler, i.e. 'as'/'ld'.
    /// </summary>
    public string? ToolPrefix { get; set; }

    /// <summary>
    /// Path to the directory where msym artifacts are stored.
    /// </summary>
    public string? MsymPath { get; set; }

    /// <summary>
    /// The assembly whose AOT image will contained dedup-ed generic instances
    /// </summary>
    public string? DedupAssembly { get; set; }

    /// <summary>
    /// Debug option in llvm aot mode
    /// defaults to "nodebug" since some targes can't generate debug info
    /// </summary>
    public string? LLVMDebug { get; set; } = "nodebug";

    /// <summary>
    /// File used to track hashes of assemblies, to act as a cache
    /// Output files don't get written, if they haven't changed
    /// </summary>
    public string? CacheFilePath { get; set; }

    /// <summary>
    /// Passes additional, custom arguments to --aot
    /// </summary>
    public string? AotArguments { get; set; }

    /// <summary>
    /// Passes temp-path to the AOT compiler
    /// </summary>
    public string? TempPath { get; set; }

    /// <summary>
    /// Passes ld-name to the AOT compiler, for use with UseLLVM=true
    /// </summary>
    public string? LdName { get; set; }

    /// <summary>
    /// Passes ld-flags to the AOT compiler, for use with UseLLVM=true
    /// </summary>
    public string? LdFlags { get; set; }

    /// <summary>
    /// Specify WorkingDirectory for the AOT compiler
    /// </summary>
    public string? WorkingDirectory { get; set; }

    [Required]
    public string IntermediateOutputPath { get; set; } = string.Empty;

    [Output]
    public string[]? FileWrites { get; private set; }

    private static readonly Encoding s_utf8Encoding = new UTF8Encoding(false);
    private const string s_originalFullPathMetadataName = "__OriginalFullPath";

    private List<string> _fileWrites = new();

    private IList<ITaskItem>? _assembliesToCompile;
    private ConcurrentDictionary<string, ITaskItem> compiledAssemblies = new();

    private MonoAotMode parsedAotMode;
    private MonoAotOutputType parsedOutputType;
    private MonoAotLibraryFormat parsedLibraryFormat;
    private MonoAotModulesTableLanguage parsedAotModulesTableLanguage;

    private FileCache? _cache;
    private int _numCompiled;
    private int _totalNumAssemblies;

    private bool ProcessAndValidateArguments()
    {
        if (!File.Exists(CompilerBinaryPath))
        {
            Log.LogError($"{nameof(CompilerBinaryPath)}='{CompilerBinaryPath}' doesn't exist.");
            return false;
        }

        if (Assemblies.Length == 0)
        {
            Log.LogError($"'{nameof(Assemblies)}' is required.");
            return false;
        }

        // A relative path might be used along with WorkingDirectory,
        // only call Path.GetFullPath() if WorkingDirectory is blank.
        if (string.IsNullOrEmpty(WorkingDirectory) && !Path.IsPathRooted(OutputDir))
            OutputDir = Path.GetFullPath(OutputDir);

        if (!Directory.Exists(OutputDir))
        {
            Log.LogError($"OutputDir={OutputDir} doesn't exist");
            return false;
        }

        if (!Directory.Exists(IntermediateOutputPath))
            Directory.CreateDirectory(IntermediateOutputPath);

        if (AotProfilePath != null)
        {
            foreach (var path in AotProfilePath)
            {
                if (!File.Exists(path))
                {
                    Log.LogError($"AotProfilePath '{path}' doesn't exist.");
                    return false;
                }
            }
        }

        foreach (var path in MibcProfilePath)
        {
            if (!File.Exists(path))
            {
                Log.LogError($"MibcProfilePath '{path}' doesn't exist.");
                return false;
            }
        }

        if (UseLLVM)
        {
            if (string.IsNullOrEmpty(LLVMPath))
                // prevent using some random llc/opt from PATH (installed with clang)
                throw new LogAsErrorException($"'{nameof(LLVMPath)}' is required when '{nameof(UseLLVM)}' is true.");

            if (!Directory.Exists(LLVMPath))
            {
                Log.LogError($"Could not find LLVMPath=${LLVMPath}");
                return false;
            }
        }

        if (!Enum.TryParse(Mode, true, out parsedAotMode))
        {
            Log.LogError($"Unknown Mode value: {Mode}. '{nameof(Mode)}' must be one of: {string.Join(",", Enum.GetNames(typeof(MonoAotMode)))}");
            return false;
        }

        switch (OutputType)
        {
            case "ObjectFile": parsedOutputType = MonoAotOutputType.ObjectFile; break;
            case "AsmOnly": parsedOutputType = MonoAotOutputType.AsmOnly; break;
            case "Library": parsedOutputType = MonoAotOutputType.Library; break;
            case "Normal":
                Log.LogWarning($"'{nameof(OutputType)}=Normal' is deprecated, use 'ObjectFile' instead.");
                parsedOutputType = MonoAotOutputType.ObjectFile; break;
            default:
                throw new LogAsErrorException($"'{nameof(OutputType)}' must be one of: '{nameof(MonoAotOutputType.ObjectFile)}', '{nameof(MonoAotOutputType.AsmOnly)}', '{nameof(MonoAotOutputType.Library)}'. Received: '{OutputType}'.");
        }

        switch (LibraryFormat)
        {
            case "Dll": parsedLibraryFormat = MonoAotLibraryFormat.Dll; break;
            case "Dylib": parsedLibraryFormat = MonoAotLibraryFormat.Dylib; break;
            case "So": parsedLibraryFormat = MonoAotLibraryFormat.So; break;
            default:
                if (parsedOutputType == MonoAotOutputType.Library)
                    throw new LogAsErrorException($"'{nameof(LibraryFormat)}' must be one of: '{nameof(MonoAotLibraryFormat.Dll)}', '{nameof(MonoAotLibraryFormat.Dylib)}', '{nameof(MonoAotLibraryFormat.So)}'. Received: '{LibraryFormat}'.");
                break;
        }

        if (parsedAotMode == MonoAotMode.LLVMOnly && !UseLLVM)
        {
            throw new LogAsErrorException($"'{nameof(UseLLVM)}' must be true when '{nameof(Mode)}' is {nameof(MonoAotMode.LLVMOnly)}.");
        }

        switch (AotModulesTableLanguage)
        {
            case "C": parsedAotModulesTableLanguage = MonoAotModulesTableLanguage.C; break;
            case "ObjC": parsedAotModulesTableLanguage = MonoAotModulesTableLanguage.ObjC; break;
            default:
                throw new LogAsErrorException($"'{nameof(AotModulesTableLanguage)}' must be one of: '{nameof(MonoAotModulesTableLanguage.C)}', '{nameof(MonoAotModulesTableLanguage.ObjC)}'. Received: '{AotModulesTableLanguage}'.");
        }

        if (!string.IsNullOrEmpty(AotModulesTablePath))
        {
            // AOT modules for static linking, needs the aot modules table
            UseStaticLinking = true;
        }

        if (UseDirectIcalls && !UseStaticLinking)
        {
            throw new LogAsErrorException($"'{nameof(UseDirectIcalls)}' can only be used with '{nameof(UseStaticLinking)}=true'.");
        }

        if (UseDirectPInvoke && !UseStaticLinking)
        {
            throw new LogAsErrorException($"'{nameof(UseDirectPInvoke)}' can only be used with '{nameof(UseStaticLinking)}=true'.");
        }

        if (UseStaticLinking && (parsedOutputType == MonoAotOutputType.Library))
        {
            throw new LogAsErrorException($"'{nameof(OutputType)}=Library' can not be used with '{nameof(UseStaticLinking)}=true'.");
        }

        foreach (var asmItem in Assemblies)
        {
            string? fullPath = asmItem.GetMetadata("FullPath");
            if (!File.Exists(fullPath))
                throw new LogAsErrorException($"Could not find {fullPath} to AOT");
        }

        return !Log.HasLoggedErrors;
    }

    public override bool Execute()
    {
        try
        {
            return ExecuteInternal();
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError(laee.Message);
            return false;
        }
        finally
        {
            if (_cache != null && _cache.Save(CacheFilePath!))
                _fileWrites.Add(CacheFilePath!);
            FileWrites = _fileWrites.ToArray();
        }
    }

    private bool ExecuteInternal()
    {
        if (!ProcessAndValidateArguments())
            return false;

        IEnumerable<ITaskItem> managedAssemblies = FilterOutUnmanagedAssemblies(Assemblies);
        managedAssemblies = EnsureAllAssembliesInTheSameDir(managedAssemblies);
        _assembliesToCompile = managedAssemblies.Where(f => !ShouldSkipForAOT(f)).ToList();

        if (!string.IsNullOrEmpty(AotModulesTablePath) && !GenerateAotModulesTable(_assembliesToCompile, Profilers, AotModulesTablePath))
            return false;

        string? monoPaths = null;
        if (AdditionalAssemblySearchPaths != null)
            monoPaths = string.Join(Path.PathSeparator.ToString(), AdditionalAssemblySearchPaths);

        _cache = new FileCache(CacheFilePath, Log);

        List<PrecompileArguments> argsList = new();
        foreach (var assemblyItem in _assembliesToCompile)
            argsList.Add(GetPrecompileArgumentsFor(assemblyItem, monoPaths));

        _totalNumAssemblies = _assembliesToCompile.Count;
        if (CheckAllUpToDate(argsList))
        {
            Log.LogMessage(MessageImportance.High, "Everything is up-to-date, nothing to precompile");

            _fileWrites.AddRange(argsList.SelectMany(args => args.ProxyFiles).Select(pf => pf.TargetFile));
            foreach (var args in argsList)
                compiledAssemblies.GetOrAdd(args.AOTAssembly.ItemSpec, args.AOTAssembly);
        }
        else
        {
            int allowedParallelism = DisableParallelAot ? 1 : Math.Min(_assembliesToCompile.Count, Environment.ProcessorCount);
            if (BuildEngine is IBuildEngine9 be9)
                allowedParallelism = be9.RequestCores(allowedParallelism);

            /*
                From: https://github.com/dotnet/runtime/issues/46146#issuecomment-754021690

                Stephen Toub:
                "As such, by default ForEach works on a scheme whereby each
                thread takes one item each time it goes back to the enumerator,
                and then after a few times of this upgrades to taking two items
                each time it goes back to the enumerator, and then four, and
                then eight, and so on. This amortizes the cost of taking and
                releasing the lock across multiple items, while still enabling
                parallelization for enumerables containing just a few items. It
                does, however, mean that if you've got a case where the body
                takes a really long time and the work for every item is
                heterogeneous, you can end up with an imbalance."

                The time taken by individual compile jobs here can vary a
                lot, depending on various factors like file size. This can
                create an imbalance, like mentioned above, and we can end up
                in a situation where one of the partitions has a job that
                takes very long to execute, by which time other partitions
                have completed, so some cores are idle.  But the idle
                ones won't get any of the remaining jobs, because they are
                all assigned to that one partition.

                Instead, we want to use work-stealing so jobs can be run by any partition.
            */
            ParallelLoopResult result = Parallel.ForEach(
                                            Partitioner.Create(argsList, EnumerablePartitionerOptions.NoBuffering),
                                            new ParallelOptions { MaxDegreeOfParallelism = allowedParallelism },
                                            PrecompileLibraryParallel);

            if (result.IsCompleted)
            {
                int numUnchanged = _totalNumAssemblies - _numCompiled;
                if (numUnchanged > 0 && numUnchanged != _totalNumAssemblies)
                    Log.LogMessage(MessageImportance.High, $"[{numUnchanged}/{_totalNumAssemblies}] skipped unchanged assemblies.");
            }
            else if (!Log.HasLoggedErrors)
            {
                Log.LogError($"Precompiling failed due to unknown reasons. Check log for more info");
            }
        }

        CompiledAssemblies = ConvertAssembliesDictToOrderedList(compiledAssemblies, _assembliesToCompile).ToArray();
        return !Log.HasLoggedErrors;
    }

    private static bool CheckAllUpToDate(IList<PrecompileArguments> argsList)
    {
        foreach (var args in argsList)
        {
            // compare original assembly vs it's outputs.. all it's outputs!
            string assemblyPath = args.AOTAssembly.GetMetadata("FullPath");
            if (args.ProxyFiles.Any(pf => IsNewerThanOutput(assemblyPath, pf.TargetFile)))
                return false;
        }

        return true;

        static bool IsNewerThanOutput(string inFile, string outFile)
            => !File.Exists(inFile) || !File.Exists(outFile) ||
                    (File.GetLastWriteTimeUtc(inFile) > File.GetLastWriteTimeUtc(outFile));
    }

    private IEnumerable<ITaskItem> FilterOutUnmanagedAssemblies(IEnumerable<ITaskItem> assemblies)
    {
        List<ITaskItem> filteredAssemblies = new();
        foreach (var asmItem in assemblies)
        {
            if (ShouldSkipForAOT(asmItem))
            {
                if (parsedAotMode == MonoAotMode.LLVMOnly)
                    throw new LogAsErrorException($"Building in AOTMode=LLVMonly is not compatible with excluding any assemblies for AOT. Excluded assembly: {asmItem.ItemSpec}");

                Log.LogMessage(MessageImportance.Low, $"Skipping {asmItem.ItemSpec} because it has %(AOT_InternalForceToInterpret)=true");
            }
            else
            {
                string assemblyPath = asmItem.GetMetadata("FullPath");
                using var assemblyFile = File.OpenRead(assemblyPath);
                using PEReader reader = new(assemblyFile, PEStreamOptions.Default);
                if (!reader.HasMetadata)
                {
                    Log.LogMessage(MessageImportance.Low, $"Skipping unmanaged {assemblyPath} for AOT");
                    continue;
                }
            }

            filteredAssemblies.Add(asmItem);
        }

        return filteredAssemblies;
    }

    private static bool ShouldSkipForAOT(ITaskItem asmItem)
        => bool.TryParse(asmItem.GetMetadata("AOT_InternalForceToInterpret"), out bool skip) && skip;

    private IEnumerable<ITaskItem> EnsureAllAssembliesInTheSameDir(IEnumerable<ITaskItem> assemblies)
    {
        string firstAsmDir = Path.GetDirectoryName(assemblies.First().GetMetadata("FullPath")) ?? string.Empty;
        bool allInSameDir = assemblies.All(asm => Path.GetDirectoryName(asm.GetMetadata("FullPath")) == firstAsmDir);
        if (allInSameDir)
            return assemblies;

        // Copy to aot-in

        string aotInPath = Path.Combine(IntermediateOutputPath, "aot-in");
        Directory.CreateDirectory(aotInPath);

        List<ITaskItem> newAssemblies = new();
        foreach (var asmItem in assemblies)
        {
            string asmPath = asmItem.GetMetadata("FullPath");
            string newPath = Path.Combine(aotInPath, Path.GetFileName(asmPath));

            // FIXME: delete files not in originalAssemblies though
            // FIXME: or .. just delete the whole dir?
            if (Utils.CopyIfDifferent(asmPath, newPath, useHash: true))
                Log.LogMessage(MessageImportance.Low, $"Copying {asmPath} to {newPath}");
            _fileWrites.Add(newPath);

            ITaskItem newAsm = new TaskItem(newPath);
            asmItem.CopyMetadataTo(newAsm);
            asmItem.SetMetadata(s_originalFullPathMetadataName, asmPath);
            newAssemblies.Add(newAsm);
        }

        return newAssemblies;
    }

    private PrecompileArguments GetPrecompileArgumentsFor(ITaskItem assemblyItem, string? monoPaths)
    {
        string assembly = assemblyItem.GetMetadata("FullPath");
        string assemblyDir = Path.GetDirectoryName(assembly)!;
        var aotAssembly = new TaskItem(assembly);
        var aotArgs = new List<string>();
        var processArgs = new List<string>();
        bool isDedup = assembly == DedupAssembly;
        List<ProxyFile> proxyFiles = new(capacity: 5);

        var a = assemblyItem.GetMetadata("AotArguments");
        if (a != null)
        {
             aotArgs.AddRange(a.Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        var p = assemblyItem.GetMetadata("ProcessArguments");
        if (p != null)
        {
            processArgs.AddRange(p.Split(new char[]{ ';' }, StringSplitOptions.RemoveEmptyEntries));
        }

        processArgs.Add("--debug");

        // add LLVM options
        if (UseLLVM)
        {
            processArgs.Add("--llvm");

            if (!string.IsNullOrEmpty(LLVMDebug))
                aotArgs.Add(LLVMDebug);

            aotArgs.Add($"llvm-path={LLVMPath}");
        }
        else
        {
            processArgs.Add("--nollvm");
        }

        if (UseStaticLinking)
        {
            aotArgs.Add($"static");
        }

        if (UseDwarfDebug)
        {
            aotArgs.Add($"dwarfdebug");
        }

        if (!string.IsNullOrEmpty(Triple))
        {
            aotArgs.Add($"mtriple={Triple}");
        }

        if (!string.IsNullOrEmpty(ToolPrefix))
        {
            aotArgs.Add($"tool-prefix={ToolPrefix}");
        }

        string assemblyFilename = Path.GetFileName(assembly);

        if (isDedup)
        {
            aotArgs.Add($"dedup-include={assemblyFilename}");
        }
        else if (!string.IsNullOrEmpty (DedupAssembly))
        {
            aotArgs.Add("dedup-skip");
        }

        // compute output mode and file names
        if (parsedAotMode == MonoAotMode.LLVMOnly || parsedAotMode == MonoAotMode.LLVMOnlyInterp)
        {
            aotArgs.Add("llvmonly");

            string llvmBitcodeFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.bc"));
            ProxyFile proxyFile = _cache!.NewFile(llvmBitcodeFile);
            proxyFiles.Add(proxyFile);
            aotAssembly.SetMetadata("LlvmBitcodeFile", proxyFile.TargetFile);

            if (parsedAotMode == MonoAotMode.LLVMOnlyInterp)
            {
                aotArgs.Add("interp");
            }

            if (parsedOutputType == MonoAotOutputType.AsmOnly)
            {
                aotArgs.Add("asmonly");
                aotArgs.Add($"llvm-outfile={proxyFile.TempFile}");
            }
            else
            {
                aotArgs.Add($"outfile={proxyFile.TempFile}");
            }
        }
        else
        {
            if (parsedAotMode == MonoAotMode.Full || parsedAotMode == MonoAotMode.FullInterp)
            {
                aotArgs.Add("full");
            }

            if (parsedAotMode == MonoAotMode.Hybrid)
            {
                aotArgs.Add("hybrid");
            }

            if (parsedAotMode == MonoAotMode.FullInterp || parsedAotMode == MonoAotMode.JustInterp)
            {
                aotArgs.Add("interp");
            }

            switch (parsedOutputType)
            {
                case MonoAotOutputType.ObjectFile:
                {
                    string objectFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.o"));
                    ProxyFile proxyFile = _cache!.NewFile(objectFile);
                    proxyFiles.Add((proxyFile));
                    aotArgs.Add($"outfile={proxyFile.TempFile}");
                    aotAssembly.SetMetadata("ObjectFile", proxyFile.TargetFile);
                }
                break;

                case MonoAotOutputType.AsmOnly:
                {
                    aotArgs.Add("asmonly");

                    string assemblerFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.s"));
                    ProxyFile proxyFile = _cache!.NewFile(assemblerFile);
                    proxyFiles.Add(proxyFile);
                    aotArgs.Add($"outfile={proxyFile.TempFile}");
                    aotAssembly.SetMetadata("AssemblerFile", proxyFile.TargetFile);
                }
                break;

                case MonoAotOutputType.Library:
                {
                    string extension = parsedLibraryFormat switch {
                        MonoAotLibraryFormat.Dll => ".dll",
                        MonoAotLibraryFormat.Dylib => ".dylib",
                        MonoAotLibraryFormat.So => ".so",
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    string libraryFileName = $"{LibraryFilePrefix}{assemblyFilename}{extension}";
                    string libraryFilePath = Path.Combine(OutputDir, libraryFileName);
                    ProxyFile proxyFile = _cache!.NewFile(libraryFilePath);
                    proxyFiles.Add(proxyFile);

                    aotArgs.Add($"outfile={proxyFile.TempFile}");
                    aotAssembly.SetMetadata("LibraryFile", proxyFile.TargetFile);
                }
                break;

                default:
                    throw new Exception($"Bug: Unhandled MonoAotOutputType: {parsedAotMode}");
            }

            if (UseLLVM)
            {
                string llvmObjectFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll-llvm.o"));
                ProxyFile proxyFile = _cache.NewFile(llvmObjectFile);
                proxyFiles.Add(proxyFile);
                aotArgs.Add($"llvm-outfile={proxyFile.TempFile}");
                aotAssembly.SetMetadata("LlvmObjectFile", proxyFile.TargetFile);
            }
        }

        // pass msym-dir if specified
        if (MsymPath != null)
        {
            aotArgs.Add($"msym-dir={MsymPath}");
        }

        if (UseAotDataFile)
        {
            string aotDataFile = Path.ChangeExtension(assembly, ".aotdata");
            ProxyFile proxyFile = _cache.NewFile(aotDataFile);
            proxyFiles.Add(proxyFile);
            aotArgs.Add($"data-outfile={proxyFile.TempFile}");
            aotAssembly.SetMetadata("AotDataFile", proxyFile.TargetFile);
        }

        if (AotProfilePath?.Length > 0)
        {
            aotArgs.Add("profile-only");
            foreach (var path in AotProfilePath)
            {
                aotArgs.Add($"profile={path}");
            }
        }

        if (MibcProfilePath.Length > 0)
        {
            aotArgs.Add("profile-only");
            foreach (var path in MibcProfilePath)
            {
                aotArgs.Add($"mibc-profile={path}");
            }
        }

        if (!string.IsNullOrEmpty(AotArguments))
        {
            aotArgs.Add(AotArguments);
        }

        if (!string.IsNullOrEmpty(TempPath))
        {
            aotArgs.Add($"temp-path={TempPath}");
        }

        if (!string.IsNullOrEmpty(LdName))
        {
            aotArgs.Add($"ld-name={LdName}");
        }

        if (!string.IsNullOrEmpty(LdFlags))
        {
            aotArgs.Add($"ld-flags={LdFlags}");
        }

        // we need to quote the entire --aot arguments here to make sure it is parsed
        // on Windows as one argument. Otherwise it will be split up into multiple
        // values, which wont work.
        processArgs.Add($"\"--aot={string.Join(",", aotArgs)}\"");

        if (isDedup)
        {
            foreach (var aItem in _assembliesToCompile!)
                processArgs.Add(aItem.ItemSpec);
        }
        else
        {
            if (string.IsNullOrEmpty(WorkingDirectory))
            {
                processArgs.Add('"' + assemblyFilename + '"');
            }
            else
            {
                // If WorkingDirectory is supplied, the caller could be passing in a relative path
                // Use the original ItemSpec that was passed in.
                processArgs.Add('"' + assemblyItem.ItemSpec + '"');
            }
        }

        monoPaths = $"{assemblyDir}{Path.PathSeparator}{monoPaths}";
        var envVariables = new Dictionary<string, string>
        {
            {"MONO_PATH", monoPaths },
            {"MONO_ENV_OPTIONS", string.Empty} // we do not want options to be provided out of band to the cross compilers
        };

        var responseFileContent = string.Join(" ", processArgs);
        var responseFilePath = Path.GetTempFileName();
        using (var sw = new StreamWriter(responseFilePath, append: false, encoding: s_utf8Encoding))
        {
            sw.WriteLine(responseFileContent);
        }

        return new PrecompileArguments(ResponseFilePath: responseFilePath,
                                        EnvironmentVariables: envVariables,
                                        WorkingDir: string.IsNullOrEmpty(WorkingDirectory) ? assemblyDir : WorkingDirectory,
                                        AOTAssembly: aotAssembly,
                                        ProxyFiles: proxyFiles);
    }

    private bool PrecompileLibrary(PrecompileArguments args)
    {
        string assembly = args.AOTAssembly.GetMetadata("FullPath");
        string output;
        try
        {
            string msgPrefix = $"[{Path.GetFileName(assembly)}] ";

            // run the AOT compiler
            (int exitCode, output) = Utils.TryRunProcess(Log,
                                                                CompilerBinaryPath,
                                                                $"--response=\"{args.ResponseFilePath}\"",
                                                                args.EnvironmentVariables,
                                                                args.WorkingDir,
                                                                silent: true,
                                                                debugMessageImportance: MessageImportance.Low,
                                                                label: Path.GetFileName(assembly));

            var importance = exitCode == 0 ? MessageImportance.Low : MessageImportance.High;
            // Log the command in a compact format which can be copy pasted
            {
                StringBuilder envStr = new StringBuilder(string.Empty);
                foreach (KeyValuePair<string, string> kvp in args.EnvironmentVariables)
                    envStr.Append($"{kvp.Key}={kvp.Value} ");
                Log.LogMessage(importance, $"{msgPrefix}Exec (with response file contents expanded) in {args.WorkingDir}: {envStr}{CompilerBinaryPath} {File.ReadAllText(args.ResponseFilePath, s_utf8Encoding)}");
            }

            if (exitCode != 0)
            {
                Log.LogError($"Precompiling failed for {assembly} with exit code {exitCode}.{Environment.NewLine}{output}");
                return false;
            }

            Log.LogMessage(importance, output);
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low, ex.ToString());
            Log.LogError($"Precompiling failed for {assembly}: {ex.Message}");
            return false;
        }
        finally
        {
            File.Delete(args.ResponseFilePath);
        }

        bool copied = false;
        foreach (var proxyFile in args.ProxyFiles)
        {
            copied |= proxyFile.CopyOutputFileIfChanged();
            _fileWrites.Add(proxyFile.TargetFile);
        }

        if (copied)
        {
            string copiedFiles = string.Join(", ", args.ProxyFiles.Select(tf => Path.GetFileName(tf.TargetFile)));
            int count = Interlocked.Increment(ref _numCompiled);
            Log.LogMessage(MessageImportance.High, $"[{count}/{_totalNumAssemblies}] {Path.GetFileName(assembly)} -> {copiedFiles}");
        }

        compiledAssemblies.GetOrAdd(args.AOTAssembly.ItemSpec, args.AOTAssembly);
        return true;
    }

    private void PrecompileLibraryParallel(PrecompileArguments args, ParallelLoopState state)
    {
        try
        {
            if (PrecompileLibrary(args))
                return;
        }
        catch (LogAsErrorException laee)
        {
            Log.LogError($"Precompiling failed for {args.AOTAssembly}: {laee.Message}");
        }
        catch (Exception ex)
        {
            if (Log.HasLoggedErrors)
                Log.LogMessage(MessageImportance.Low, $"Precompile failed for {args.AOTAssembly}: {ex}");
            else
                Log.LogError($"Precompiling failed for {args.AOTAssembly}: {ex}");
        }

        state.Break();
    }

    private bool GenerateAotModulesTable(IEnumerable<ITaskItem> assemblies, string[]? profilers, string outputFile)
    {
        var symbols = new List<string>();
        foreach (var asm in assemblies)
        {
            string asmPath = asm.ItemSpec;
            if (!File.Exists(asmPath))
            {
                Log.LogError($"Could not find assembly {asmPath}");
                return false;
            }

            if (!TryGetAssemblyName(asmPath, out string? assemblyName))
                return false;

            string symbolName = assemblyName.Replace ('.', '_').Replace ('-', '_').Replace(' ', '_');
            symbols.Add($"mono_aot_module_{symbolName}_info");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(outputFile)!);

        string tmpAotModulesTablePath = Path.GetTempFileName();
        using (var writer = File.CreateText(tmpAotModulesTablePath))
        {
            if (parsedAotModulesTableLanguage == MonoAotModulesTableLanguage.C)
            {
                writer.WriteLine("#include <mono/jit/jit.h>");

                foreach (var symbol in symbols)
                {
                    writer.WriteLine($"extern void *{symbol};");
                }
                writer.WriteLine("void register_aot_modules ()");
                writer.WriteLine("{");
                foreach (var symbol in symbols)
                {
                    writer.WriteLine($"\tmono_aot_register_module ({symbol});");
                }
                writer.WriteLine("}");

                foreach (var profiler in profilers ?? Enumerable.Empty<string>())
                {
                    writer.WriteLine($"void mono_profiler_init_{profiler} (const char *desc);");
                    writer.WriteLine("EMSCRIPTEN_KEEPALIVE void mono_wasm_load_profiler_" + profiler + " (const char *desc) { mono_profiler_init_" + profiler + " (desc); }");
                }

                if (parsedAotMode == MonoAotMode.LLVMOnly)
                {
                    writer.WriteLine("#define EE_MODE_LLVMONLY 1");
                }

                if (parsedAotMode == MonoAotMode.LLVMOnlyInterp)
                {
                    writer.WriteLine("#define EE_MODE_LLVMONLY_INTERP 1");
                }
            }
            else if (parsedAotModulesTableLanguage == MonoAotModulesTableLanguage.ObjC)
            {
                writer.WriteLine("#include <mono/jit/jit.h>");
                writer.WriteLine("#include <TargetConditionals.h>");
                writer.WriteLine("");
                writer.WriteLine("#if TARGET_OS_IPHONE && (!TARGET_IPHONE_SIMULATOR || FORCE_AOT)");

                foreach (var symbol in symbols)
                {
                    writer.WriteLine($"extern void *{symbol};");
                }

                writer.WriteLine("void register_aot_modules (void)");
                writer.WriteLine("{");
                foreach (var symbol in symbols)
                {
                    writer.WriteLine($"\tmono_aot_register_module ({symbol});");
                }
                writer.WriteLine("}");
                writer.WriteLine("#endif");
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        if (Utils.CopyIfDifferent(tmpAotModulesTablePath, outputFile, useHash: false))
        {
            _fileWrites.Add(outputFile);
            Log.LogMessage(MessageImportance.Low, $"Generated {outputFile}");
        }

        return true;
    }

    private bool TryGetAssemblyName(string asmPath, [NotNullWhen(true)] out string? assemblyName)
    {
        assemblyName = null;

        try
        {
            using var fs = new FileStream(asmPath, FileMode.Open, FileAccess.Read);
            using var peReader = new PEReader(fs);
            MetadataReader mr = peReader.GetMetadataReader();
            assemblyName = mr.GetAssemblyDefinition().GetAssemblyName().Name;

            if (string.IsNullOrEmpty(assemblyName))
            {
                Log.LogError($"Could not get assembly name for {asmPath}");
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to get assembly name for {asmPath}: {ex.Message}");
            return false;
        }
    }

    private static IList<ITaskItem> ConvertAssembliesDictToOrderedList(ConcurrentDictionary<string, ITaskItem> dict, IList<ITaskItem> originalAssemblies)
    {
        List<ITaskItem> outItems = new(originalAssemblies.Count);
        foreach (ITaskItem item in originalAssemblies)
        {
            if (dict.TryGetValue(item.GetMetadata("FullPath"), out ITaskItem? dictItem))
                outItems.Add(dictItem);
        }
        return outItems;
    }

    internal sealed class PrecompileArguments
    {
        public PrecompileArguments(string ResponseFilePath, IDictionary<string, string> EnvironmentVariables, string WorkingDir, ITaskItem AOTAssembly, IList<ProxyFile> ProxyFiles)
        {
            this.ResponseFilePath  = ResponseFilePath;
            this.EnvironmentVariables  = EnvironmentVariables;
            this.WorkingDir  = WorkingDir;
            this.AOTAssembly  = AOTAssembly;
            this.ProxyFiles  = ProxyFiles;
        }

        public string                       ResponseFilePath     { get; private set; }
        public IDictionary<string, string>  EnvironmentVariables { get; private set; }
        public string                       WorkingDir           { get; private set; }
        public ITaskItem                    AOTAssembly          { get; private set; }
        public IList<ProxyFile>             ProxyFiles           { get; private set; }
    }
}

public enum MonoAotMode
{
    Normal,
    JustInterp,
    Full,
    FullInterp,
    Hybrid,
    LLVMOnly,
    LLVMOnlyInterp
}

public enum MonoAotOutputType
{
    ObjectFile,
    AsmOnly,
    Library,
}

public enum MonoAotLibraryFormat
{
    Dll,
    Dylib,
    So,
}

public enum MonoAotModulesTableLanguage
{
    C,
    ObjC
}
