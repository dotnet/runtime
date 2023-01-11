// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class LibraryBuilderTask : AppBuilderTask
{
    private string nativeLibraryType = "SHARED";

    /// <summary>
    /// The name of the library being generated
    /// </summary>
    [Required]
    [NotNull]
    public string? Name { get; set; }

    /// <summary>
    /// Extra native sources to be added to the library
    /// </summary>
    public ITaskItem[] ExtraSources { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Additional linker arguments that apply to the library being built
    /// </summary>
    public ITaskItem[] ExtraLinkerArguments { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// The type of native library being produced. Can be shared|static.
    /// </summary>
    public string NativeLibrary
    {
        get => nativeLibraryType;
        set => nativeLibraryType = value.ToUpperInvariant();
    }

    /// <summary>
    /// The location of the cmake file output
    /// </summary>
    [Output]
    public string OutputPath { get; set; } = ""!;

    public override bool Execute()
    {
        StringBuilder aotSources = new StringBuilder();
        StringBuilder aotObjects = new StringBuilder();
        StringBuilder extraSources = new StringBuilder();
        StringBuilder linkerArgs = new StringBuilder();

        if (!base.Execute())
        {
            // log something here
            return false;
        }

        GatherAotSourcesObjects(aotSources, aotObjects, extraSources, linkerArgs);
        GatherLinkerArgs(linkerArgs);

        WriteCMakeFileFromTemplate(aotSources.ToString(), aotObjects.ToString(), extraSources.ToString(), linkerArgs.ToString());

        return true;
    }

    private void GatherAotSourcesObjects(StringBuilder aotSources, StringBuilder aotObjects, StringBuilder extraSources, StringBuilder linkerArgs)
    {
        foreach (CompiledAssembly compiledAssembly in CompiledAssemblies)
        {
            aotSources.AppendLine(compiledAssembly.AssemblerFile);
            aotObjects.AppendLine($"    {compiledAssembly.LlvmObjectFile}");

            if (!string.IsNullOrEmpty(compiledAssembly.ExportsFile))
            {
                linkerArgs.AppendLine($"    \"-Wl,-exported_symbols_list {compiledAssembly.ExportsFile}\"");
            }
        }

        foreach (ITaskItem item in ExtraSources)
        {
            extraSources.AppendLine(item.ItemSpec);
        }
    }

    private void GatherLinkerArgs(StringBuilder linkerArgs)
    {
        foreach (ITaskItem item in RuntimeLibraries)
        {
            linkerArgs.AppendLine($"    \"-force_load {item.ItemSpec}\"");
        }

        foreach (ITaskItem item in ExtraLinkerArguments)
        {
            linkerArgs.AppendLine($"    \"{item.ItemSpec}\"");
        }
    }

    private void WriteCMakeFileFromTemplate(string aotSources, string aotObjects, string extraSources, string linkerArgs)
    {
        // BundleDir
        File.WriteAllText(Path.Combine(OutputDirectory, "dotnet_library.cmake"),
            Utils.GetEmbeddedResource("dotnet_library.cmake")
                .Replace("%LIBRARY_NAME%", Name)
                .Replace("%LIBRARY_TYPE%", NativeLibrary)
                .Replace("%AotSources%", aotSources)
                .Replace("%AotObjects%", aotObjects)
                .Replace("%ExtraSources%", extraSources)
                .Replace("%LIBRARY_LINKER_ARGS%", linkerArgs));
    }
}
