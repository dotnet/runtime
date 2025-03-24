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

public class AppBuilderTask : Task
{
    /// <summary>
    /// List of paths to assemblies.
    /// For AOT builds, the following metadata could be set:
    ///   - AssemblerFile (when using OutputType=AsmOnly)
    ///   - ObjectFile (when using OutputType=Normal)
    ///   - LibraryFile (when using OutputType=Library)
    ///   - AotDataFile (when using UseAotDataFile=true)
    ///   - LlvmObjectFile (if using LLVM)
    ///   - LlvmBitcodeFile (if using LLVM-only)
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Path to Mono public headers (*.h)
    /// </summary>
    [Required]
    public string[] MonoRuntimeHeaders { get; set; } = [];

    /// <summary>
    /// Path to store build artifacts
    /// </summary>
    [Required]
    public string OutputDirectory { get; set; } = ""!;

    /// <summary>
    /// OS + architecture runtime identifier
    /// </summary>
    [Required]
    public string RuntimeIdentifier { get; set; } = ""!;

    /// <summary>
    /// List of libraries to link against
    /// </summary>
    [Required]
    public ITaskItem[] RuntimeLibraries { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Files to be ignored in AppDir
    /// </summary>
    public ITaskItem[]? ExcludeFromAppDir { get; set; }

    /// <summary>
    /// Diagnostic ports configuration string
    /// </summary>
    public string DiagnosticPorts { get; set; } = ""!;

    protected List<CompiledAssembly> CompiledAssemblies { get; set; }

    public AppBuilderTask()
    {
        CompiledAssemblies = new List<CompiledAssembly>(Assemblies.Length);
    }

    public override bool Execute()
    {
        GatherCompiledAssemblies();

        return true;
    }

    private void GatherCompiledAssemblies()
    {
        Directory.CreateDirectory(OutputDirectory);

        foreach (ITaskItem file in Assemblies)
        {
            CompiledAssembly compiledAssembly = new CompiledAssembly();
            compiledAssembly.Path = file.ItemSpec;

            compiledAssembly.AssemblerFile = file.GetMetadata("AssemblerFile");
            compiledAssembly.ObjectFile = file.GetMetadata("ObjectFile");
            compiledAssembly.LibraryFile = file.GetMetadata("LibraryFile");
            compiledAssembly.DataFile = file.GetMetadata("AotDataFile");
            compiledAssembly.LlvmObjectFile = file.GetMetadata("LlvmObjectFile");
            compiledAssembly.LlvmBitCodeFile = file.GetMetadata("LlvmBitcodeFile");
            compiledAssembly.ExportsFile = file.GetMetadata("ExportSymbolsFile");

            CompiledAssemblies.Add(compiledAssembly);
        }
    }
}
