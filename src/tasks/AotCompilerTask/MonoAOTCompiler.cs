// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

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
    /// Assemblies which were AOT compiled.
    ///
    /// Successful AOT compilation will set the following metadata on the items:
    ///   - AssemblerFile (when using OutputType=AsmOnly)
    ///   - ObjectFile (when using OutputType=Normal)
    ///   - AotDataFile
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
    /// Use a separate .aotdata file for the AOT data.
    /// Defaults to true.
    /// </summary>
    public bool UseAotDataFile { get; set; } = true;

    /// <summary>
    /// File to use for profile-guided optimization.
    /// </summary>
    public string? AotProfilePath { get; set; }

    /// <summary>
    /// List of profilers to use.
    /// </summary>
    public string[]? Profilers { get; set; }

    /// <summary>
    /// Generate a file containing mono_aot_register_module() calls for each AOT module
    /// Defaults to false.
    /// </summary>
    public string? AotModulesTablePath { get; set; }

    /// <summary>
    /// Source code language of the AOT modules table. Supports "C" or "ObjC".
    /// Defaults to "C".
    /// </summary>
    public string? AotModulesTableLanguage { get; set; } = nameof(MonoAotModulesTableLanguage.C);

    /// <summary>
    /// Choose between 'Normal', 'Full', 'LLVMOnly'.
    /// LLVMOnly means to use only LLVM for FullAOT, AOT result will be a LLVM Bitcode file (the cross-compiler must be built with LLVM support)
    /// </summary>
    public string Mode { get; set; } = nameof(MonoAotMode.Normal);

    /// <summary>
    /// Choose between 'Normal', 'AsmOnly'
    /// AsmOnly means the AOT compiler will produce .s assembly code instead of an .o object file.
    /// </summary>
    public string OutputType { get; set; } = nameof(MonoAotOutputType.Normal);

    /// <summary>
    /// Path to the directory where LLVM binaries (opt and llc) are found.
    /// It's required if UseLLVM is set
    /// </summary>
    public string? LLVMPath { get; set; }

    /// <summary>
    /// Path to the directory where msym artifacts are stored.
    /// </summary>
    public string? MsymPath { get; set; }

    /// <summary>
    /// The assembly whose AOT image will contained dedup-ed generic instances
    /// </summary>
    public string? DedupAssembly { get; set; }

    [Output]
    public string[]? FileWrites { get; private set; }

    private List<string> _fileWrites = new();

    private ConcurrentBag<ITaskItem> compiledAssemblies = new ConcurrentBag<ITaskItem>();
    private MonoAotMode parsedAotMode;
    private MonoAotOutputType parsedOutputType;
    private MonoAotModulesTableLanguage parsedAotModulesTableLanguage;

    public override bool Execute()
    {
        Utils.Logger = Log;

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
            Log.LogError($"Unknown Mode value: {Mode}. '{nameof(Mode)}' must be one of: {string.Join(',', Enum.GetNames(typeof(MonoAotMode)))}");
            return false;
        }

        switch (OutputType)
        {
            case "Normal": parsedOutputType = MonoAotOutputType.Normal; break;
            case "AsmOnly": parsedOutputType = MonoAotOutputType.AsmOnly; break;
            default:
                throw new ArgumentException($"'{nameof(OutputType)}' must be one of: '{nameof(MonoAotOutputType.Normal)}', '{nameof(MonoAotOutputType.AsmOnly)}'. Received: '{OutputType}'.", nameof(OutputType));
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
            GenerateAotModulesTable(Assemblies, Profilers);
        }

        string? monoPaths = null;
        if (AdditionalAssemblySearchPaths != null)
            monoPaths = string.Join(Path.PathSeparator, AdditionalAssemblySearchPaths);

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

        var a = assemblyItem.GetMetadata("AotArguments");
        if (a != null)
        {
             aotArgs.AddRange(a.Split(";", StringSplitOptions.RemoveEmptyEntries));
        }

        var p = assemblyItem.GetMetadata("ProcessArguments");
        if (p != null)
        {
            processArgs.AddRange(p.Split(";", StringSplitOptions.RemoveEmptyEntries));
        }

        Log.LogMessage(MessageImportance.Low, $"[AOT] {assembly}");

        processArgs.Add("--debug");

        // add LLVM options
        if (UseLLVM)
        {
            processArgs.Add("--llvm");

            aotArgs.Add($"nodebug"); // can't use debug symbols with LLVM
            aotArgs.Add($"llvm-path={LLVMPath}");
        }
        else
        {
            processArgs.Add("--nollvm");
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
        if (parsedAotMode == MonoAotMode.LLVMOnly || parsedAotMode == MonoAotMode.AotInterp)
        {
            aotArgs.Add("llvmonly");

            string llvmBitcodeFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.bc"));
            aotAssembly.SetMetadata("LlvmBitcodeFile", llvmBitcodeFile);

            if (parsedAotMode == MonoAotMode.AotInterp)
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
            if (parsedAotMode == MonoAotMode.Full)
            {
                aotArgs.Add("full");
            }

            if (parsedOutputType == MonoAotOutputType.AsmOnly)
            {
                aotArgs.Add("asmonly");

                string assemblerFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.s"));
                aotArgs.Add($"outfile={assemblerFile}");
                aotAssembly.SetMetadata("AssemblerFile", assemblerFile);
            }
            else
            {
                string objectFile = Path.Combine(OutputDir, Path.ChangeExtension(assemblyFilename, ".dll.o"));
                aotArgs.Add($"outfile={objectFile}");
                aotAssembly.SetMetadata("ObjectFile", objectFile);
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
            processArgs.Add(assemblyFilename);
        }

        var envVariables = new Dictionary<string, string>
        {
            {"MONO_PATH", paths},
            {"MONO_ENV_OPTIONS", string.Empty} // we do not want options to be provided out of band to the cross compilers
        };

        try
        {
            // run the AOT compiler
            Utils.RunProcess(CompilerBinaryPath, string.Join(" ", processArgs), envVariables, assemblyDir, silent: false,
                    outputMessageImportance: MessageImportance.Low, debugMessageImportance: MessageImportance.Low);
        }
        catch (Exception ex)
        {
            Log.LogMessage(MessageImportance.Low, ex.ToString());
            Log.LogError($"Precompiling failed for {assembly}: {ex.Message}");
            return false;
        }

        compiledAssemblies.Add(aotAssembly);
        return true;
    }

    private void GenerateAotModulesTable(ITaskItem[] assemblies, string[]? profilers)
    {
        var symbols = new List<string>();
        foreach (var asm in assemblies)
        {
            var name = Path.GetFileNameWithoutExtension(asm.ItemSpec).Replace ('.', '_').Replace ('-', '_');
            symbols.Add($"mono_aot_module_{name}_info");
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

                if (parsedAotMode == MonoAotMode.AotInterp)
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
    }
}

public enum MonoAotMode
{
    Normal,
    Full,
    LLVMOnly,
    AotInterp
}

public enum MonoAotOutputType
{
    Normal,
    AsmOnly,
}

public enum MonoAotModulesTableLanguage
{
    C,
    ObjC
}
