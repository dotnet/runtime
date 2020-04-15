// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AppleAppBuilderTask : Task
{
    /// <summary>
    /// Path to arm64 AOT cross-compiler (mono-aot-cross)
    /// It's not used for x64 (Simulator)
    /// </summary>
    public string? CrossCompiler { get; set; }

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
    /// Path to store build artifacts
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Produce optimized binaries (e.g. use -O2 in AOT)
    /// and use 'Release' config in xcode
    /// </summary>
    public bool Optimized { get; set; }

    /// <summary>
    /// Disable parallel AOT compilation
    /// </summary>
    public bool DisableParallelAot { get; set; }

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
    /// Path to xcode project
    /// </summary>
    [Output]
    public string XcodeProjectPath { get; set; } = ""!;

    public override bool Execute()
    {
        Utils.Logger = Log;
        bool isDevice = Arch.Equals("arm64", StringComparison.InvariantCultureIgnoreCase);
        if (isDevice && string.IsNullOrEmpty(CrossCompiler))
        {
            throw new ArgumentException("arm64 arch requires CrossCompiler");
        }

        if (!File.Exists(Path.Combine(AppDir, MainLibraryFileName)))
        {
            throw new ArgumentException($"MainLibraryFileName='{MainLibraryFileName}' was not found in AppDir='{AppDir}'");
        }

        if (ProjectName.Contains(" "))
        {
            throw new ArgumentException($"ProjectName='{ProjectName}' should not contain spaces");
        }

        string[] excludes = new string[0];
        if (ExcludeFromAppDir != null)
        {
            excludes = ExcludeFromAppDir
                .Where(i => !string.IsNullOrEmpty(i.ItemSpec))
                .Select(i => i.ItemSpec)
                .ToArray();
        }
        string[] libsToAot = Directory.GetFiles(AppDir, "*.dll")
            .Where(f => !excludes.Contains(Path.GetFileName(f)))
            .ToArray();

        string binDir = Path.Combine(AppDir, $"bin-{ProjectName}-{Arch}");
        if (!string.IsNullOrEmpty(OutputDirectory))
        {
            binDir = OutputDirectory;
        }
        Directory.CreateDirectory(binDir);

        // run AOT compilation only for devices
        if (isDevice)
        {
            if (string.IsNullOrEmpty(CrossCompiler))
                throw new InvalidOperationException("cross-compiler is not set");

            AotCompiler.PrecompileLibraries(CrossCompiler, Arch, !DisableParallelAot, binDir, libsToAot,
                new Dictionary<string, string> { {"MONO_PATH", AppDir} },
                Optimized);
        }

        // generate modules.m
        AotCompiler.GenerateLinkAllFile(
            Directory.GetFiles(binDir, "*.dll.o"),
            Path.Combine(binDir, "modules.m"));

        if (GenerateXcodeProject)
        {
            XcodeProjectPath = Xcode.GenerateXCode(ProjectName, MainLibraryFileName, 
                AppDir, binDir, MonoRuntimeHeaders, !isDevice, UseConsoleUITemplate, NativeMainSource);

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
}
