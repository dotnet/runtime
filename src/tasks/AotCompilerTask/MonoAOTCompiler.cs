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
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Reflection.PortableExecutable;

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
    public string? AotProfilePath { get; set; }

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

    [Output]
    public string[]? FileWrites { get; private set; }

    private List<string> _fileWrites = new();

    private ConcurrentBag<ITaskItem> compiledAssemblies = new ConcurrentBag<ITaskItem>();
    private MonoAotMode parsedAotMode;
    private MonoAotOutputType parsedOutputType;
    private MonoAotLibraryFormat parsedLibraryFormat;
    private MonoAotModulesTableLanguage parsedAotModulesTableLanguage;

    public override bool Execute()
    {
        if (string.IsNullOrEmpty(CompilerBinaryPath))
        {
            throw new ArgumentException($"'{nameof(CompilerBinaryPath)}' is required.", nameof(CompilerBinaryPath));
        }

        if (!File.Exists(CompilerBinaryPath))
        {
            throw new ArgumentException($"'{CompilerBinaryPath}' doesn't exist.", nameof(CompilerBinaryPath));
        }

        if (Assemblies.Length == 0)
        {
            throw new ArgumentException($"'{nameof(Assemblies)}' is required.", nameof(Assemblies));
        }

        if (!Path.IsPathRooted(OutputDir))
            OutputDir = Path.GetFullPath(OutputDir);

        if (!Directory.Exists(OutputDir))
        {
            Log.LogError($"OutputDir={OutputDir} doesn't exist");
            return false;
        }

        if (!string.IsNullOrEmpty(AotProfilePath) && !File.Exists(AotProfilePath))
        {
            Log.LogError($"'{AotProfilePath}' doesn't exist.", nameof(AotProfilePath));
            return false;
        }

        if (UseLLVM)
        {
            if (string.IsNullOrEmpty(LLVMPath))
                // prevent using some random llc/opt from PATH (installed with clang)
                throw new ArgumentException($"'{nameof(LLVMPath)}' is required when '{nameof(UseLLVM)}' is true.", nameof(LLVMPath));

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
                throw new ArgumentException($"'{nameof(OutputType)}' must be one of: '{nameof(MonoAotOutputType.ObjectFile)}', '{nameof(MonoAotOutputType.AsmOnly)}', '{nameof(MonoAotOutputType.Library)}'. Received: '{OutputType}'.", nameof(OutputType));
        }

        switch (LibraryFormat)
        {
            case "Dll": parsedLibraryFormat = MonoAotLibraryFormat.Dll; break;
            case "Dylib": parsedLibraryFormat = MonoAotLibraryFormat.Dylib; break;
            case "So": parsedLibraryFormat = MonoAotLibraryFormat.So; break;
            default:
                if (parsedOutputType == MonoAotOutputType.Library)
                    throw new ArgumentException($"'{nameof(LibraryFormat)}' must be one of: '{nameof(MonoAotLibraryFormat.Dll)}', '{nameof(MonoAotLibraryFormat.Dylib)}', '{nameof(MonoAotLibraryFormat.So)}'. Received: '{LibraryFormat}'.", nameof(LibraryFormat));
                break;
        }

        if (parsedAotMode == MonoAotMode.LLVMOnly && !UseLLVM)
        {
            throw new ArgumentException($"'{nameof(UseLLVM)}' must be true when '{nameof(Mode)}' is {nameof(MonoAotMode.LLVMOnly)}.", nameof(UseLLVM));
        }

        switch (AotModulesTableLanguage)
        {
            case "C": parsedAotModulesTableLanguage = MonoAotModulesTableLanguage.C; break;
            case "ObjC": parsedAotModulesTableLanguage = MonoAotModulesTableLanguage.ObjC; break;
            default:
                throw new ArgumentException($"'{nameof(AotModulesTableLanguage)}' must be one of: '{nameof(MonoAotModulesTableLanguage.C)}', '{nameof(MonoAotModulesTableLanguage.ObjC)}'. Received: '{AotModulesTableLanguage}'.", nameof(AotModulesTableLanguage));
        }

        if (!string.IsNullOrEmpty(AotModulesTablePath))
        {
            // AOT modules for static linking, needs the aot modules table
            UseStaticLinking = true;

            if (!GenerateAotModulesTable(Assemblies, Profilers))
                return false;
        }

        if (UseDirectIcalls && !UseStaticLinking)
        {
            throw new ArgumentException($"'{nameof(UseDirectIcalls)}' can only be used with '{nameof(UseStaticLinking)}=true'.", nameof(UseDirectIcalls));
        }

        if (UseDirectPInvoke && !UseStaticLinking)
        {
            throw new ArgumentException($"'{nameof(UseDirectPInvoke)}' can only be used with '{nameof(UseStaticLinking)}=true'.", nameof(UseDirectPInvoke));
        }

        if (UseStaticLinking && (parsedOutputType == MonoAotOutputType.Library))
        {
            throw new ArgumentException($"'{nameof(OutputType)}=Library' can not be used with '{nameof(UseStaticLinking)}=true'.", nameof(OutputType));
        }

        string? monoPaths = null;
        if (AdditionalAssemblySearchPaths != null)
            monoPaths = string.Join(Path.PathSeparator.ToString(), AdditionalAssemblySearchPaths);

        if (DisableParallelAot)
        {
            foreach (var assemblyItem in Assemblies)
            {
                if (!PrecompileLibrary(assemblyItem, monoPaths))
                    return !Log.HasLoggedErrors;
            }
        }
        else
        {
            Parallel.ForEach(Assemblies,
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                assemblyItem => PrecompileLibrary(assemblyItem, monoPaths));
        }

        CompiledAssemblies = compiledAssemblies.ToArray();
        FileWrites = _fileWrites.ToArray();

        return !Log.HasLoggedErrors;
    }

    private bool PrecompileLibrary(ITaskItem assemblyItem, string? monoPaths)
    {
        string assembly = assemblyItem.ItemSpec;
        string assemblyDir = Path.GetDirectoryName(assembly)!;
        var aotAssembly = new TaskItem(assembly);
        var aotArgs = new List<string>();
        var processArgs = new List<string>();
        bool isDedup = assembly == DedupAssembly;
        string msgPrefix = $"[{Path.GetFileName(assembly)}] ";

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

        Log.LogMessage(MessageImportance.Low, $"[AOT] {assembly}");

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
            aotAssembly.SetMetadata("LlvmBitcodeFile", llvmBitcodeFile);

            if (parsedAotMode == MonoAotMode.LLVMOnlyInterp)
            {
                aotArgs.Add("interp");
            }

            if (parsedOutputType == MonoAotOutputType.AsmOnly)
            {
                aotArgs.Add("asmonly");
                aotArgs.Add($"llvm-outfile={llvmBitcodeFile}");
            }
            else
            {
                aotArgs.Add($"outfile={llvmBitcodeFile}");
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

            if (parsedOutputType == MonoAotOutputType.ObjectFile)
            {
                string objectFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.o"));
                aotArgs.Add($"outfile={objectFile}");
                aotAssembly.SetMetadata("ObjectFile", objectFile);
            }
            else if (parsedOutputType == MonoAotOutputType.AsmOnly)
            {
                aotArgs.Add("asmonly");

                string assemblerFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.s"));
                aotArgs.Add($"outfile={assemblerFile}");
                aotAssembly.SetMetadata("AssemblerFile", assemblerFile);
            }
            else if (parsedOutputType == MonoAotOutputType.Library)
            {
                string extension = parsedLibraryFormat switch {
                    MonoAotLibraryFormat.Dll => ".dll",
                    MonoAotLibraryFormat.Dylib => ".dylib",
                    MonoAotLibraryFormat.So => ".so",
                    _ => throw new ArgumentOutOfRangeException()
                };
                string libraryFileName = $"{LibraryFilePrefix}{assemblyFilename}{extension}";
                string libraryFilePath = Path.Combine(OutputDir, libraryFileName);

                aotArgs.Add($"outfile={libraryFilePath}");
                aotAssembly.SetMetadata("LibraryFile", libraryFilePath);
            }

            if (UseLLVM)
            {
                string llvmObjectFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll-llvm.o"));
                aotArgs.Add($"llvm-outfile={llvmObjectFile}");
                aotAssembly.SetMetadata("LlvmObjectFile", llvmObjectFile);
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
            aotArgs.Add($"data-outfile={aotDataFile}");
            aotAssembly.SetMetadata("AotDataFile", aotDataFile);
        }

        if (!string.IsNullOrEmpty(AotProfilePath))
        {
            aotArgs.Add($"profile={AotProfilePath},profile-only");
        }

        // we need to quote the entire --aot arguments here to make sure it is parsed
        // on Windows as one argument. Otherwise it will be split up into multiple
        // values, which wont work.
        processArgs.Add($"\"--aot={string.Join(",", aotArgs)}\"");

        string paths = "";
        if (isDedup)
        {
            StringBuilder sb = new StringBuilder();
            HashSet<string> allPaths = new HashSet<string>();
            foreach (var aItem in Assemblies)
            {
                string filename = aItem.ItemSpec;
                processArgs.Add(filename);
                string dir = Path.GetDirectoryName(filename)!;
                if (!allPaths.Contains(dir))
                {
                    allPaths.Add(dir);
                    if (sb.Length > 0)
                        sb.Append(Path.PathSeparator);
                    sb.Append(dir);
                }
            }
            if (sb.Length > 0)
                sb.Append(Path.PathSeparator);
            sb.Append(monoPaths);
            paths = sb.ToString();
        }
        else
        {
            paths = $"{assemblyDir}{Path.PathSeparator}{monoPaths}";
            processArgs.Add('"' + assemblyFilename + '"');
        }

        var envVariables = new Dictionary<string, string>
        {
            {"MONO_PATH", paths},
            {"MONO_ENV_OPTIONS", string.Empty} // we do not want options to be provided out of band to the cross compilers
        };

        var responseFileContent = string.Join(" ", processArgs);
        var responseFilePath = Path.GetTempFileName();
        using (var sw = new StreamWriter(responseFilePath, append: false, encoding: new UTF8Encoding(false)))
        {
            sw.WriteLine(responseFileContent);
        }

        string workingDir = assemblyDir;

        // Log the command in a compact format which can be copy pasted
        {
            StringBuilder envStr = new StringBuilder(string.Empty);
            foreach (KeyValuePair<string, string> kvp in envVariables)
                envStr.Append($"{kvp.Key}={kvp.Value} ");
            Log.LogMessage(MessageImportance.Low, $"{msgPrefix}Exec (with response file contents expanded) in {workingDir}: {envStr}{CompilerBinaryPath} {responseFileContent}");
        }

        try
        {
            // run the AOT compiler
            (int exitCode, string output) = Utils.TryRunProcess(Log,
                                                                CompilerBinaryPath,
                                                                $"--response=\"{responseFilePath}\"",
                                                                envVariables,
                                                                workingDir,
                                                                silent: false,
                                                                debugMessageImportance: MessageImportance.Low,
                                                                label: Path.GetFileName(assembly));
            if (exitCode != 0)
            {
                Log.LogError($"Precompiling failed for {assembly}: {output}");
                return false;
            }
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low, ex.ToString());
            Log.LogError($"Precompiling failed for {assembly}: {ex.Message}");
            return false;
        }

        File.Delete(responseFilePath);

        compiledAssemblies.Add(aotAssembly);
        return true;
    }

    private bool GenerateAotModulesTable(ITaskItem[] assemblies, string[]? profilers)
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

            string symbolName = assemblyName.Replace ('.', '_').Replace ('-', '_');
            symbols.Add($"mono_aot_module_{symbolName}_info");
        }

        Directory.CreateDirectory(Path.GetDirectoryName(AotModulesTablePath!)!);

        using (var writer = File.CreateText(AotModulesTablePath!))
        {
            _fileWrites.Add(AotModulesTablePath!);
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
            Log.LogMessage(MessageImportance.Low, $"Generated {AotModulesTablePath}");
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
