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
    /// Additional symbols for the library to retain outside of what was specified via
    /// [UnmanagedCallersOnly]
    /// </summary>
    public string[] ExtraSymbolsToExport { get; set; } = Array.Empty<string>();

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

    public bool StripDebugSymbols { get; set; }

    /// <summary>
    /// The location of the cmake file output
    /// </summary>
    [Output]
    public string OutputPath { get; set; } = ""!;

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

        WriteCMakeFileFromTemplate(aotSources.ToString(), aotObjects.ToString(), extraSources.ToString(), linkerArgs.ToString());
        OutputPath = BuildLibrary();

        return true;
    }

    private void GatherAotSourcesObjects(StringBuilder aotSources, StringBuilder aotObjects, StringBuilder extraSources, StringBuilder linkerArgs)
    {
        List<string> exportedSymbols = new List<string>(ExtraSymbolsToExport);
        bool hasExports = (exportedSymbols.Count > 0);

        foreach (CompiledAssembly compiledAssembly in CompiledAssemblies)
        {
            if (!string.IsNullOrEmpty(compiledAssembly.AssemblerFile))
            {
                aotSources.AppendLine(compiledAssembly.AssemblerFile);
            }

            if (!string.IsNullOrEmpty(compiledAssembly.LlvmObjectFile))
            {
                aotObjects.AppendLine($"    {compiledAssembly.LlvmObjectFile}");
            }

            if (!string.IsNullOrEmpty(compiledAssembly.ExportsFile))
            {
                hasExports = true;

                if (TargetOS == "android")
                {
                    GatherExportedSymbols(compiledAssembly.ExportsFile, exportedSymbols);
                }
                else
                {
                    WriteExportedSymbolsArg(compiledAssembly.ExportsFile, linkerArgs);
                }
            }
        }

        // for android, all symbols to keep go in one linker script
        //
        // for ios, multiple files can be specified
        if (hasExports && TargetOS == "android")
        {
            WriteLinkerScriptFile(MobileSymbolFileName, exportedSymbols);
            WriteLinkerScriptArg(MobileSymbolFileName, linkerArgs);
        }
        else if (hasExports && ExtraSymbolsToExport.Length > 0)
        {
            File.WriteAllText(
                MobileSymbolFileName,
                string.Join("\n", exportedSymbols.Select(symbol => symbol))
            );
            WriteExportedSymbolsArg(MobileSymbolFileName, linkerArgs);
        }

        foreach (ITaskItem item in ExtraSources)
        {
            extraSources.AppendLine(item.ItemSpec);
        }
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

    private static void GatherExportedSymbols(string exportsFile, List<string> exportedSymbols)
    {
        foreach (string symbol in File.ReadLines(exportsFile))
        {
            exportedSymbols.Add(symbol);
        }
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

    private void WriteCMakeFileFromTemplate(string aotSources, string aotObjects, string extraSources, string linkerArgs)
    {
        // BundleDir
        File.WriteAllText(Path.Combine(OutputDirectory, "CMakeLists.txt"),
            Utils.GetEmbeddedResource("CMakeLists.txt.template")
                .Replace("%LIBRARY_NAME%", Name)
                .Replace("%LIBRARY_TYPE%", nativeLibraryType)
                .Replace("%CMAKE_LANGS%", CMakeProjectLanguages)
                .Replace("%MonoInclude%", MonoRuntimeHeaders)
                .Replace("%AotSources%", aotSources)
                .Replace("%AotObjects%", aotObjects)
                .Replace("%ExtraSources%", extraSources)
                .Replace("%LIBRARY_LINKER_ARGS%", linkerArgs));

        File.WriteAllText(Path.Combine(OutputDirectory, "autoinit.c"),
            Utils.GetEmbeddedResource("autoinit.c"));
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
