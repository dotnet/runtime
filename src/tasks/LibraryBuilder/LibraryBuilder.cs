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
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class LibraryBuilderTask : AppBuilderTask
{
    private bool isSharedLibrary = true;
    private string nativeLibraryType = "SHARED";

    private string cmakeProjectLanguages = "";
    private string targetOS = "";
    private bool usesAOTDataFile;
    private List<string> exportedAssemblies = new List<string>();

    /// <summary>
    /// The name of the library being generated
    /// </summary>
    [Required]
    [NotNull]
    public string? Name { get; set; }

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
    /// The symbols of preallocated structs being bundled
    /// </summary>
    public ITaskItem[] BundledPreallocatedSymbols { get; set; } = Array.Empty<ITaskItem>();

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

    private string CMakeProjectLanguages
    {
        get
        {
            if (string.IsNullOrEmpty(cmakeProjectLanguages))
            {
                cmakeProjectLanguages = (TargetOS == "android") ? "C ASM" : "OBJC ASM";
            }

            return cmakeProjectLanguages;
        }
    }

    public override bool Execute()
    {
        StringBuilder aotSources = new StringBuilder();
        StringBuilder aotObjects = new StringBuilder();
        StringBuilder extraSources = new StringBuilder();
        StringBuilder linkerArgs = new StringBuilder();

        if (!ValidateValidTargetOS())
        {
            throw new ArgumentException($"{TargetOS} is not yet supported by the librarybuilder task.");
        }

        if (!base.Execute())
        {
            // log something here
            return false;
        }

        GatherAotSourcesObjects(aotSources, aotObjects, extraSources, linkerArgs);
        GatherLinkerArgs(linkerArgs);

        File.WriteAllText(Path.Combine(OutputDirectory, "library-builder.h"),
            Utils.GetEmbeddedResource("library-builder.h"));

        GenerateAssembliesLoader();

        if (UsesRuntimeInitCallback && !UsesCustomRuntimeInitCallback)
        {
            WriteAutoInitializationFromTemplate();
            extraSources.AppendLine("    autoinit.c");
        }

        if (BundlesResources)
        {
            GenerateBundledResourcesLoader();
            extraSources.AppendLine("    preallocated-resources.c");
        }

        WriteCMakeFileFromTemplate(aotSources.ToString(), aotObjects.ToString(), extraSources.ToString(), linkerArgs.ToString());
        OutputPath = BuildLibrary();

        return true;
    }

    private void GatherAotSourcesObjects(StringBuilder aotSources, StringBuilder aotObjects, StringBuilder extraSources, StringBuilder linkerArgs)
    {
        List<string> exportedSymbols = new List<string>();

        foreach (CompiledAssembly compiledAssembly in CompiledAssemblies)
        {
            if (!string.IsNullOrEmpty(compiledAssembly.AssemblerFile))
            {
                aotSources.AppendLine($"    {compiledAssembly.AssemblerFile}");
            }

            if (!usesAOTDataFile && !string.IsNullOrEmpty(compiledAssembly.DataFile))
            {
                usesAOTDataFile = true;
            }

            if (!string.IsNullOrEmpty(compiledAssembly.LlvmObjectFile))
            {
                aotObjects.AppendLine($"    {compiledAssembly.LlvmObjectFile}");
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
                WriteLinkerScriptArg(MobileSymbolFileName, linkerArgs);
            }
            else
            {
                File.WriteAllText(
                    MobileSymbolFileName,
                    string.Join("\n", exportedSymbols.Select(symbol => symbol))
                );
                WriteExportedSymbolsArg(MobileSymbolFileName, linkerArgs);
            }
        }

        foreach (ITaskItem item in ExtraSources)
        {
            extraSources.AppendLine($"    {item.ItemSpec}");
        }

        ExportedSymbols = exportedSymbols.ToArray();
    }

    private void GatherLinkerArgs(StringBuilder linkerArgs)
    {
        string libForceLoad = "";

        if (TargetOS != "android")
        {
            libForceLoad = "-force_load ";
        }

        foreach (ITaskItem item in RuntimeLibraries)
        {
            linkerArgs.AppendLine($"    \"{libForceLoad}{item.ItemSpec}\"");
        }

        foreach (ITaskItem item in ExtraLinkerArguments)
        {
            linkerArgs.AppendLine($"    \"{item.ItemSpec}\"");
        }
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

    private static void WriteExportedSymbolsArg(string exportsFile, StringBuilder linkerArgs)
    {
        linkerArgs.AppendLine($"    \"-Wl,-exported_symbols_list {exportsFile}\"");
    }

    private static void WriteLinkerScriptArg(string exportsFile, StringBuilder linkerArgs)
    {
        linkerArgs.AppendLine($"    \"-Wl,--version-script={exportsFile}\"");
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
        File.WriteAllText(Path.Combine(OutputDirectory, "autoinit.c"),
            Utils.GetEmbeddedResource("autoinit.c")
                .Replace("%ASSEMBLIES_LOCATION%", !string.IsNullOrEmpty(AssembliesLocation) ? AssembliesLocation : "DOTNET_LIBRARY_ASSEMBLY_PATH")
                .Replace("%RUNTIME_IDENTIFIER%", RuntimeIdentifier));
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

    private void GenerateBundledResourcesLoader()
    {
        var preallocatedResources = new StringBuilder();
        var preallocatedAssemblies = new StringBuilder("MonoBundledResource *bundledAssemblyResources[] = { ");
        var preallocatedSatelliteAssemblies = new StringBuilder("MonoBundledResource *bundledSatelliteAssemblyResources[] = { ");
        var preallocatedData = new StringBuilder("MonoBundledResource *bundledDataResources[] = { ");
        int assembliesCount = 0;
        int satelliteAssembliesCount = 0;
        int dataCount = 0;
        foreach (ITaskItem bundledPreallocatedSymbol in BundledPreallocatedSymbols)
        {
            preallocatedResources.AppendLine($"extern const {bundledPreallocatedSymbol.GetMetadata("ResourceType")} {bundledPreallocatedSymbol.ItemSpec};");

            switch (bundledPreallocatedSymbol.GetMetadata("ResourceType")) {
            case "MonoBundledAssemblyResource": {
                preallocatedAssemblies.Append($"(MonoBundledResource *)&{bundledPreallocatedSymbol.ItemSpec}, ");
                assembliesCount += 1;
                break;
            }
            case "MonoBundledSatelliteAssemblyResource": {
                preallocatedSatelliteAssemblies.Append($"(MonoBundledResource *)&{bundledPreallocatedSymbol.ItemSpec}, ");
                satelliteAssembliesCount += 1;
                break;
            }
            case "MonoBundledDataResource":
            default: {
                preallocatedData.Append($"(MonoBundledResource *)&{bundledPreallocatedSymbol.ItemSpec}, ");
                dataCount += 1;
                break;
            }
            }
        }

        var addPreallocatedResources = new StringBuilder();
        if (assembliesCount != 0) {
            preallocatedAssemblies.AppendLine("};");
            preallocatedResources.AppendLine(preallocatedAssemblies.ToString());
            addPreallocatedResources.AppendLine($"    mono_add_bundled_resource (bundledAssemblyResources, {assembliesCount});");
        }
        if (satelliteAssembliesCount != 0) {
            preallocatedSatelliteAssemblies.AppendLine("};");
            preallocatedResources.AppendLine(preallocatedSatelliteAssemblies.ToString());
            addPreallocatedResources.AppendLine($"    mono_add_bundled_resource (bundledSatelliteAssemblyResources, {satelliteAssembliesCount});");
        }
        if (dataCount != 0) {
            preallocatedData.AppendLine("};");
            preallocatedResources.AppendLine(preallocatedData.ToString());
            addPreallocatedResources.AppendLine($"    mono_add_bundled_resource (bundledDataResources, {dataCount});");
        }

        File.WriteAllText(Path.Combine(OutputDirectory, "preallocated-resources.c"),
            Utils.GetEmbeddedResource("preallocated-resources.c")
                .Replace("%PreallocatedResources%", preallocatedResources.ToString())
                .Replace("%AddPreallocatedResources%", addPreallocatedResources.ToString()));
    }

    private void WriteCMakeFileFromTemplate(string aotSources, string aotObjects, string extraSources, string linkerArgs)
    {
        string extraDefinitions = GenerateExtraDefinitions();
        // BundleDir
        File.WriteAllText(Path.Combine(OutputDirectory, "CMakeLists.txt"),
            Utils.GetEmbeddedResource("CMakeLists.txt.template")
                .Replace("%LIBRARY_NAME%", Name)
                .Replace("%LIBRARY_TYPE%", nativeLibraryType)
                .Replace("%CMAKE_LANGS%", CMakeProjectLanguages)
                .Replace("%MonoInclude%", MonoRuntimeHeaders)
                .Replace("%AotSources%", aotSources)
                .Replace("%AotObjects%", aotObjects)
                .Replace("%ExtraDefinitions%", extraDefinitions)
                .Replace("%ExtraSources%", extraSources)
                .Replace("%LIBRARY_LINKER_ARGS%", linkerArgs));
    }

    private string GenerateExtraDefinitions()
    {
        var extraDefinitions = new StringBuilder();

        if (usesAOTDataFile)
        {
            extraDefinitions.AppendLine("add_definitions(-DUSES_AOT_DATA=1)");
        }

        if (BundlesResources)
        {
            extraDefinitions.AppendLine("add_definitions(-DBUNDLED_RESOURCES=1)");
        }

        return extraDefinitions.ToString();
    }

    private string BuildLibrary()
    {
        string libraryOutputPath;

        if (TargetOS == "android")
        {
            AndroidProject project = new AndroidProject("netlibrary", RuntimeIdentifier, Log);
            project.GenerateCMake(OutputDirectory, StripDebugSymbols);
            libraryOutputPath = project.BuildCMake(OutputDirectory, StripDebugSymbols);
        }
        else
        {
            Xcode project = new Xcode(Log, RuntimeIdentifier);
            project.CreateXcodeProject("netlibrary", OutputDirectory);

            string xcodeProjectPath = Path.Combine(OutputDirectory, "netlibrary", $"{Name}.xcodeproj");
            libraryOutputPath = project.BuildAppBundle(xcodeProjectPath, StripDebugSymbols, "-");
        }

        return Path.Combine(libraryOutputPath, GetLibraryName());
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
            "android" or "ios" or "tvos" or "maccatalyst" => true,
            _ => false
        };
}
