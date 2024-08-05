// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Android.Build;
using Microsoft.Apple.Build;
using Microsoft.Mobile.Build.Clang;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class LibraryBuilderTask : AppBuilderTask
{
    private bool isSharedLibrary = true;
    private string nativeLibraryType = "SHARED";

    private string targetOS = "";
    private bool usesAOTDataFile;
    private List<string> exportedAssemblies = new List<string>();

    /// <summary>
    /// The name of the library being generated
    /// </summary>
    [Required]
    public string? Name { get; set; } = ""!;

    /// <summary>
    /// The name of the OS being targeted
    /// </summary>
    [Required]
    public string TargetOS
    {
        get
        {
            return targetOS;
        }
        set
        {
            targetOS = value.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Extra native sources to be added to the library
    /// </summary>
    public ITaskItem[] ExtraSources { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Additional linker arguments that apply to the library being built
    /// </summary>
    public ITaskItem[] ExtraLinkerArguments { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Determines if the library is static or shared
    /// </summary>
    public bool IsSharedLibrary
    {
        get => isSharedLibrary;
        set
        {
            isSharedLibrary = value;
            nativeLibraryType = (isSharedLibrary) ? "SHARED" : "STATIC";
        }
    }

    /// <summary>
    /// Determines whether or not the mono runtime auto initialization
    /// template, autoinit.c, is used.
    /// </summary>
    public bool UsesCustomRuntimeInitCallback { get; set; }

    /// <summary>
    /// Determines if there is a mono runtime init callback
    /// </summary>
    public bool UsesRuntimeInitCallback { get; set; }

    /// <summary>
    /// The environment variable name that will point to where assemblies
    /// are located on the app host device.
    /// </summary>
    public string? AssembliesLocation { get; set; }

    /// <summary>
    /// Determines whether or not assemblies are bundled into the library
    /// </summary>
    public bool BundlesResources { get; set; }

    /// <summary>
    /// An Item containing the bundled runtimeconfig.bin metadata detailing
    /// DataSymbol - Symbol corresponding to the runtimeconfig.bin byte array data
    /// DataLenSymbol - Symbol corresponding to the runtimeconfig.bin byte array size
    /// DataLenSymbolValue - Literal size of the runtimeconfig.bin byte array data
    /// </summary>
    public ITaskItem? BundledRuntimeConfig { get; set; }

    public bool StripDebugSymbols { get; set; }

    /// <summary>
    /// The location of the cmake file output
    /// </summary>
    [Output]
    public string OutputPath { get; set; } = ""!;

    /// <summary>
    /// The set of exported symbols identified by the aot compiler.
    /// </summary>
    [Output]
    public string[] ExportedSymbols { get; set; } = Array.Empty<string>();

    private string MobileSymbolFileName
    {
        get => Path.Combine(OutputDirectory, "mobile_symbols.txt");
    }

    public override bool Execute()
    {
        List<string> sources = new List<string>();
        List<string> libs = new List<string>();
        List<string> linkerArgs = new List<string>();

        if (!ValidateValidTargetOS())
        {
            throw new ArgumentException($"{TargetOS} is not yet supported by the librarybuilder task.");
        }

        if (!base.Execute())
        {
            // log something here
            return false;
        }

        GatherSourcesAndLibs(sources, libs, linkerArgs);

        File.WriteAllText(Path.Combine(OutputDirectory, "library-builder.h"),
            Utils.GetEmbeddedResource("library-builder.h"));

        GenerateAssembliesLoader();

        if (UsesRuntimeInitCallback && !UsesCustomRuntimeInitCallback)
        {
            WriteAutoInitializationFromTemplate();
        }

        if (TargetOS == "android")
        {
            OutputPath = BuildAndroidLibrary(sources, libs, linkerArgs);
        }
        else
        {
            OutputPath = BuildAppleLibrary(sources, libs, linkerArgs);
        }

        return true;
    }

    // Intended for native toolchain specific builds
    private void GatherSourcesAndLibs(List<string> sources, List<string> libs, List<string> linkerArgs)
    {
        List<string> exportedSymbols = new List<string>();

        foreach (CompiledAssembly compiledAssembly in CompiledAssemblies)
        {
            if (!usesAOTDataFile && !string.IsNullOrEmpty(compiledAssembly.DataFile))
            {
                usesAOTDataFile = true;
            }

            if (!string.IsNullOrEmpty(compiledAssembly.AssemblerFile))
            {
                sources.Add(compiledAssembly.AssemblerFile);
            }

            if (!string.IsNullOrEmpty(compiledAssembly.LlvmObjectFile))
            {
                sources.Add(compiledAssembly.LlvmObjectFile);
            }

            if (!string.IsNullOrEmpty(compiledAssembly.ObjectFile))
            {
                sources.Add(compiledAssembly.ObjectFile);
            }

            if (!string.IsNullOrEmpty(compiledAssembly.ExportsFile))
            {
                int symbolsAdded = GatherExportedSymbols(compiledAssembly.ExportsFile, exportedSymbols);

                if (symbolsAdded > 0)
                {
                    exportedAssemblies.Add(Path.GetFileName(compiledAssembly.Path));
                }
            }
        }

        foreach (ITaskItem lib in RuntimeLibraries)
        {
            string ext = Path.GetExtension(lib.ItemSpec);

            if (ext == ".so" || ext == ".dylib")
            {
                libs.Add(lib.ItemSpec);
            }
            else
            {
                sources.Add(lib.ItemSpec);
            }
        }

        foreach (ITaskItem item in ExtraLinkerArguments)
        {
            linkerArgs.Add(item.ItemSpec);
        }

        if (exportedAssemblies.Count == 0)
        {
            throw new LogAsErrorException($"None of the compiled assemblies contain exported symbols. The library must export only symbols resulting from [UnmanageCallersOnly(Entrypoint = )]Resulting shared library would be unusable.");
        }

        if (IsSharedLibrary)
        {
            // for android, all symbols to keep go in one linker script
            //
            // for ios, multiple files can be specified
            if (TargetOS == "android")
            {
                WriteLinkerScriptFile(MobileSymbolFileName, exportedSymbols);
                linkerArgs.Add($"\"--version-script={MobileSymbolFileName}\"");
            }
            else
            {
                File.WriteAllText(
                    MobileSymbolFileName,
                    string.Join("\n", exportedSymbols.Select(symbol => symbol))
                );
                linkerArgs.Add($"-exported_symbols_list {MobileSymbolFileName}");
            }
        }

        foreach (ITaskItem item in ExtraSources)
        {
            sources.Add(item.ItemSpec);
        }

        ExportedSymbols = exportedSymbols.ToArray();
    }

    private static int GatherExportedSymbols(string exportsFile, List<string> exportedSymbols)
    {
        int count = 0;

        foreach (string symbol in File.ReadLines(exportsFile))
        {
            exportedSymbols.Add(symbol);
            count++;
        }

        return count;
    }

    private static void WriteLinkerScriptFile(string exportsFile, List<string> exportedSymbols)
    {
        string globalExports = string.Join(";\n", exportedSymbols.Select(symbol => symbol));
        File.WriteAllText(exportsFile,
            Utils.GetEmbeddedResource("linker-script.txt")
                .Replace("%GLOBAL_SYMBOLS%", globalExports));
    }

    private void WriteAutoInitializationFromTemplate()
    {
        string autoInitialization = Utils.GetEmbeddedResource("autoinit.c")
                .Replace("%ASSEMBLIES_LOCATION%", !string.IsNullOrEmpty(AssembliesLocation) ? AssembliesLocation : "DOTNET_LIBRARY_ASSEMBLY_PATH")
                .Replace("%RUNTIME_IDENTIFIER%", RuntimeIdentifier);

        if (BundlesResources)
        {
            string dataSymbol = "NULL";
            string dataLenSymbol = "0";
            StringBuilder externBundledResourcesSymbols = new ("#if defined(BUNDLED_RESOURCES)\nextern void mono_register_resources_bundle (void);");
            if (BundledRuntimeConfig?.ItemSpec != null)
            {
                dataSymbol = BundledRuntimeConfig.GetMetadata("DataSymbol");
                if (string.IsNullOrEmpty(dataSymbol))
                {
                    throw new LogAsErrorException($"'{nameof(BundledRuntimeConfig)}' does not contain 'DataSymbol' metadata.");
                }
                dataLenSymbol = BundledRuntimeConfig.GetMetadata("DataLenSymbol");
                if (string.IsNullOrEmpty(dataLenSymbol))
                {
                    throw new LogAsErrorException($"'{nameof(BundledRuntimeConfig)}' does not contain 'DataLenSymbol' metadata.");
                }
                externBundledResourcesSymbols.AppendLine();
                externBundledResourcesSymbols.AppendLine($"extern uint8_t {dataSymbol}[];");
                externBundledResourcesSymbols.AppendLine($"extern const uint32_t {dataLenSymbol};");
            }

            externBundledResourcesSymbols.AppendLine("#endif");

            autoInitialization = autoInitialization
                .Replace("%EXTERN_BUNDLED_RESOURCES_SYMBOLS%", externBundledResourcesSymbols.ToString())
                .Replace("%RUNTIME_CONFIG_DATA%", dataSymbol)
                .Replace("%RUNTIME_CONFIG_DATA_LEN%", dataLenSymbol);
        }
        else
        {
            autoInitialization = autoInitialization.Replace("%EXTERN_BUNDLED_RESOURCES_SYMBOLS%", string.Empty);
        }

        File.WriteAllText(Path.Combine(OutputDirectory, "autoinit.c"), autoInitialization);
    }

    private void GenerateAssembliesLoader()
    {
        var assemblyPreloaders = new List<string>();
        foreach (string exportedAssembly in exportedAssemblies)
        {
            assemblyPreloaders.Add($"preload_assembly(\"{exportedAssembly}\");");
        }

        File.WriteAllText(Path.Combine(OutputDirectory, "preloaded-assemblies.c"),
            Utils.GetEmbeddedResource("preloaded-assemblies.c")
                .Replace("%ASSEMBLIES_PRELOADER%", string.Join("\n    ", assemblyPreloaders)));
    }

    private string BuildAndroidLibrary(List<string> sources, List<string> libs, List<string> linkerArgs)
    {
        string libraryName = GetLibraryName();

        ClangBuildOptions buildOptions = new ClangBuildOptions();
        buildOptions.CompilerArguments.Add("-D ANDROID=1");
        buildOptions.CompilerArguments.Add("-D HOST_ANDROID=1");
        buildOptions.CompilerArguments.Add("-fPIC");
        buildOptions.CompilerArguments.Add(IsSharedLibrary ? $"-shared -o {libraryName}" : $"-o {libraryName}");
        buildOptions.IncludePaths.Add(MonoRuntimeHeaders);
        buildOptions.LinkerArguments.Add($"--soname={libraryName}");

        // Google requires all the native libraries to be aligned to 16 bytes (for 16k memory page size)
        // This is required only for 64-bit binaries.
        if (string.CompareOrdinal ("android-arm64", RuntimeIdentifier) == 0 || string.CompareOrdinal ("android-x64", RuntimeIdentifier) == 0) {
            buildOptions.LinkerArguments.Add($"-z,max-page-size=16384");
        }
        buildOptions.LinkerArguments.AddRange(linkerArgs);
        buildOptions.NativeLibraryPaths.AddRange(libs);
        buildOptions.Sources.AddRange(sources);
        buildOptions.Sources.Add("preloaded-assemblies.c");

        if (UsesRuntimeInitCallback && !UsesCustomRuntimeInitCallback)
        {
            buildOptions.Sources.Add("autoinit.c");
        }

        if (BundlesResources)
        {
            buildOptions.CompilerArguments.Add("-D BUNDLED_RESOURCES=1");
        }

        if (usesAOTDataFile)
        {
            buildOptions.CompilerArguments.Add("-D USES_AOT_DATA=1");
        }

        AndroidProject project = new AndroidProject("netlibrary", RuntimeIdentifier, Log);
        project.Build(OutputDirectory, buildOptions, StripDebugSymbols);

        return Path.Combine(OutputDirectory, libraryName);
    }

    private string BuildAppleLibrary(List<string> sources, List<string> libs, List<string> linkerArgs)
    {
        string libraryName = GetLibraryName();

        ClangBuildOptions buildOptions = new ClangBuildOptions();
        buildOptions.CompilerArguments.Add(IsSharedLibrary ? $"-dynamiclib -o {libraryName}" : $"-o {libraryName}");
        buildOptions.CompilerArguments.Add("-D HOST_APPLE_MOBILE=1");
        buildOptions.CompilerArguments.Add("-D FORCE_AOT=1");
        buildOptions.IncludePaths.Add(MonoRuntimeHeaders);
        buildOptions.NativeLibraryPaths.AddRange(libs);
        buildOptions.Sources.AddRange(sources);
        buildOptions.Sources.Add("preloaded-assemblies.c");

        if (IsSharedLibrary)
        {
            linkerArgs.Add("-Wl,-headerpad_max_install_names");
        }

        buildOptions.LinkerArguments.AddRange(linkerArgs);

        if (UsesRuntimeInitCallback && !UsesCustomRuntimeInitCallback)
        {
            buildOptions.Sources.Add("autoinit.c");
        }

        if (BundlesResources)
        {
            buildOptions.CompilerArguments.Add("-D BUNDLED_RESOURCES=1");
        }

        if (usesAOTDataFile)
        {
            buildOptions.CompilerArguments.Add("-D USES_AOT_DATA=1");
        }

        AppleProject project = new AppleProject("netlibrary", RuntimeIdentifier, Log);
        project.Build(OutputDirectory, buildOptions, StripDebugSymbols);

        if (IsSharedLibrary)
        {
            string installToolArgs = $"install_name_tool -id @rpath/{libraryName} {libraryName}";
            Utils.RunProcess(Log, "xcrun", workingDir: OutputDirectory, args: installToolArgs);
        }

        return Path.Combine(OutputDirectory, libraryName);
    }

    private string GetLibraryName()
    {
        string libPrefix, libExtension;

        if (TargetOS == "android")
        {
            libPrefix = "lib";
            libExtension = (isSharedLibrary) ? ".so" : ".a";
        }
        else
        {
            libPrefix = "lib";
            libExtension = (isSharedLibrary) ? ".dylib" : ".a";
        }

        return $"{libPrefix}{Name}{libExtension}";
    }

    private bool ValidateValidTargetOS() =>
        TargetOS switch
        {
            "android" or "ios" or "iossimulator" or "tvos" or "tvossimulator" or "maccatalyst" => true,
            _ => false
        };
}
