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
    private string targetOS = TargetNames.iOS;

    /// <summary>
    /// The Apple OS we are targeting (iOS or tvOS)
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
            targetOS = value.ToLower();
        }
    }

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
    /// Target arch, can be "arm64" (device) or "x64" (simulator) at the moment
    /// </summary>
    [Required]
    public string Arch { get; set; } = ""!;

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

    /// <summary>
    /// Path to store build artifacts
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Produce optimized binaries and use 'Release' config in xcode
    /// </summary>
    public bool Optimized { get; set; }

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
    /// Prefer FullAOT mode for Simulator over JIT
    /// </summary>
    public bool ForceAOT { get; set; }

    /// <summary>
    /// List of enabled runtime components
    /// </summary>
    public string? RuntimeComponents { get; set; } = ""!;

    /// <summary>
    /// Forces the runtime to use the invariant mode
    /// </summary>
    public bool InvariantGlobalization { get; set; }

    /// <summary>
    /// Forces the runtime to use the interpreter
    /// </summary>
    public bool ForceInterpreter { get; set; }

    /// <summary>
    /// Enables detailed runtime logging
    /// </summary>
    public bool EnableRuntimeLogging { get; set; }

    public override bool Execute()
    {
        Utils.Logger = Log;
        bool isDevice = (TargetOS == TargetNames.iOS || TargetOS == TargetNames.tvOS);

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

        if (((!ForceInterpreter && (isDevice || ForceAOT)) && !assemblerFiles.Any()))
        {
            throw new InvalidOperationException("Need list of AOT files for device builds.");
        }

        if (ForceInterpreter && ForceAOT)
        {
            throw new InvalidOperationException("Interpreter and AOT cannot be enabled at the same time");
        }

        if (GenerateXcodeProject)
        {
            Xcode generator = new Xcode(TargetOS, Arch);
            generator.EnableRuntimeLogging = EnableRuntimeLogging;

            XcodeProjectPath = generator.GenerateXCode(ProjectName, MainLibraryFileName, assemblerFiles,
                AppDir, binDir, MonoRuntimeHeaders, !isDevice, UseConsoleUITemplate, ForceAOT, ForceInterpreter, InvariantGlobalization, Optimized, RuntimeComponents, NativeMainSource);

            if (BuildAppBundle)
            {
                if (isDevice && string.IsNullOrEmpty(DevTeamProvisioning))
                {
                    // DevTeamProvisioning shouldn't be empty for arm64 builds
                    Utils.LogInfo("DevTeamProvisioning is not set, BuildAppBundle step is skipped.");
                }
                else
                {
                    AppBundlePath = generator.BuildAppBundle(XcodeProjectPath, Arch, Optimized, DevTeamProvisioning);
                }
            }
        }

        return true;
    }
}
