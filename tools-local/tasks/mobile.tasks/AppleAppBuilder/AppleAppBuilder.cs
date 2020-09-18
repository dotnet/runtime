// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AppleAppBuilderTask : Task
{
    /// <summary>
    /// ProjectName is used as an app name, bundleId and xcode project name
    /// </summary>
    [Required]
    public string ProjectName { get; set; } = ""!;

    /// <summary>
    /// Target directory with *dll and other content to be AOT'd and/or bundled
    /// </summary>
    [Required]
    public string AppDir { get; set; } = ""!;

    /// <summary>
    /// Path to Mono public headers (*.h)
    /// </summary>
    [Required]
    public string MonoRuntimeHeaders { get; set; } = ""!;

    /// <summary>
    /// This library will be used as an entry-point (e.g. TestRunner.dll)
    /// </summary>
    [Required]
    public string MainLibraryFileName { get; set; } = ""!;

    /// <summary>
    /// List of paths to assemblies to be included in the app. For AOT builds the 'ObjectFile' metadata key needs to point to the object file.
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Path to store build artifacts
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Produce optimized binaries and use 'Release' config in xcode
    /// </summary>
    public bool Optimized { get; set; }

    /// <summary>
    /// Target arch, can be "arm64" (device) or "x64" (simulator) at the moment
    /// </summary>
    [Required]
    public string Arch { get; set; } = ""!;

    /// <summary>
    /// DEVELOPER_TEAM provisioning, needed for arm64 builds.
    /// </summary>
    public string? DevTeamProvisioning { get; set; }

    /// <summary>
    /// Build *.app bundle (using XCode for now)
    /// </summary>
    public bool BuildAppBundle { get; set; }

    /// <summary>
    /// Generate xcode project
    /// </summary>
    public bool GenerateXcodeProject { get; set; }

    /// <summary>
    /// Files to be ignored in AppDir
    /// </summary>
    public ITaskItem[]? ExcludeFromAppDir { get; set; }

    /// <summary>
    /// Path to a custom main.m with custom UI
    /// A default one is used if it's not set
    /// </summary>
    public string? NativeMainSource { get; set; }

    /// <summary>
    /// Use Console-style native UI template
    /// (use NativeMainSource to override)
    /// </summary>
    public bool UseConsoleUITemplate { get; set; }

    /// <summary>
    /// Path to *.app bundle
    /// </summary>
    [Output]
    public string AppBundlePath { get; set; } = ""!;

    /// <summary>
    /// Prefer FullAOT mode for Simulator over JIT
    /// </summary>
    public bool UseAotForSimulator { get; set; }

    /// <summary>
    /// Path to xcode project
    /// </summary>
    [Output]
    public string XcodeProjectPath { get; set; } = ""!;

    public override bool Execute()
    {
        Utils.Logger = Log;
        bool isDevice = Arch.Equals("arm64", StringComparison.InvariantCultureIgnoreCase);

        if (!File.Exists(Path.Combine(AppDir, MainLibraryFileName)))
        {
            throw new ArgumentException($"MainLibraryFileName='{MainLibraryFileName}' was not found in AppDir='{AppDir}'");
        }

        if (ProjectName.Contains(" "))
        {
            throw new ArgumentException($"ProjectName='{ProjectName}' should not contain spaces");
        }

        string[] excludes = Array.Empty<string>();
        if (ExcludeFromAppDir != null)
        {
            excludes = ExcludeFromAppDir
                .Where(i => !string.IsNullOrEmpty(i.ItemSpec))
                .Select(i => i.ItemSpec)
                .ToArray();
        }

        string binDir = Path.Combine(AppDir, $"bin-{ProjectName}-{Arch}");
        if (!string.IsNullOrEmpty(OutputDirectory))
        {
            binDir = OutputDirectory;
        }
        Directory.CreateDirectory(binDir);

        var assemblerFiles = new List<string>();
        foreach (ITaskItem file in Assemblies)
        {
            // use AOT files if available
            var obj = file.GetMetadata("AssemblerFile");
            if (!string.IsNullOrEmpty(obj))
            {
                assemblerFiles.Add(obj);
            }
        }

        if ((isDevice || UseAotForSimulator) && !assemblerFiles.Any())
        {
            throw new InvalidOperationException("Need list of AOT files for device builds.");
        }

        // generate modules.m
        GenerateLinkAllFile(
            assemblerFiles,
            Path.Combine(binDir, "modules.m"));

        if (GenerateXcodeProject)
        {
            XcodeProjectPath = Xcode.GenerateXCode(ProjectName, MainLibraryFileName, assemblerFiles,
                AppDir, binDir, MonoRuntimeHeaders, !isDevice, UseConsoleUITemplate, UseAotForSimulator, Optimized, NativeMainSource);

            if (BuildAppBundle)
            {
                if (isDevice && string.IsNullOrEmpty(DevTeamProvisioning))
                {
                    // DevTeamProvisioning shouldn't be empty for arm64 builds
                    Utils.LogInfo("DevTeamProvisioning is not set, BuildAppBundle step is skipped.");
                }
                else
                {
                    AppBundlePath = Xcode.BuildAppBundle(
                        Path.Combine(binDir, ProjectName, ProjectName + ".xcodeproj"),
                        Arch, Optimized, DevTeamProvisioning);
                }
            }
        }

        return true;
    }

    private static void GenerateLinkAllFile(IEnumerable<string> asmFiles, string outputFile)
    {
        //  Generates 'modules.m' in order to register all managed libraries
        //
        //
        // extern void *mono_aot_module_Lib1_info;
        // extern void *mono_aot_module_Lib2_info;
        // ...
        //
        // void mono_ios_register_modules (void)
        // {
        //     mono_aot_register_module (mono_aot_module_Lib1_info);
        //     mono_aot_register_module (mono_aot_module_Lib2_info);
        //     ...
        // }

        Utils.LogInfo("Generating 'modules.m'...");

        var lsDecl = new StringBuilder();
        lsDecl
            .AppendLine("#include <mono/jit/jit.h>")
            .AppendLine("#include <TargetConditionals.h>")
            .AppendLine()
            .AppendLine("#if TARGET_OS_IPHONE && (!TARGET_IPHONE_SIMULATOR || USE_AOT_FOR_SIMULATOR)")
            .AppendLine();

        var lsUsage = new StringBuilder();
        lsUsage
            .AppendLine("void mono_ios_register_modules (void)")
            .AppendLine("{");
        foreach (string asmFile in asmFiles)
        {
            string symbol = "mono_aot_module_" +
                            Path.GetFileName(asmFile)
                                .Replace(".dll.s", "")
                                .Replace(".", "_")
                                .Replace("-", "_") + "_info";

            lsDecl.Append("extern void *").Append(symbol).Append(';').AppendLine();
            lsUsage.Append("\tmono_aot_register_module (").Append(symbol).Append(");").AppendLine();
        }
        lsDecl
            .AppendLine()
            .Append(lsUsage)
            .AppendLine("}")
            .AppendLine()
            .AppendLine("#endif")
            .AppendLine();

        File.WriteAllText(outputFile, lsDecl.ToString());
        Utils.LogInfo($"Saved to {outputFile}.");
    }
}
