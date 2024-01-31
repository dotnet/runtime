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
    /// The Apple OS we are targeting (ios, tvos, iossimulator, tvossimulator)
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
    public string MonoRuntimeHeaders { get; set; } = ""!;

    /// <summary>
    /// This library will be used as an entry point (e.g. TestRunner.dll). Can
    /// be empty. If empty, the entry point of the app must be specified in an
    /// environment variable named "MONO_APPLE_APP_ENTRY_POINT_LIB_NAME" when
    /// running the resulting app.
    /// </summary>
    public string MainLibraryFileName { get; set; } = ""!;

    /// <summary>
    /// List of paths to assemblies to be included in the app. For AOT builds the 'ObjectFile' metadata key needs to point to the object file.
    /// </summary>
    [Required]
    public ITaskItem[] Assemblies { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Additional linker arguments that apply to the app being built
    /// </summary>
    public ITaskItem[] ExtraLinkerArguments { get; set; } = Array.Empty<ITaskItem>();

    /// <summary>
    /// Target arch, can be "arm64", "arm" or "x64" at the moment
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
    /// Generate CMake project
    /// </summary>
    public bool GenerateCMakeProject { get; set; }

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
    public string[] RuntimeComponents { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Diagnostic ports configuration string
    /// </summary>
    public string? DiagnosticPorts { get; set; } = ""!;

    /// <summary>
    /// Forces the runtime to use the invariant mode
    /// </summary>
    public bool InvariantGlobalization { get; set; }

    /// <summary>
    /// Forces the runtime to use hybrid(icu files + native functions) mode
    /// </summary>
    public bool HybridGlobalization { get; set; } = true;

    /// <summary>
    /// Forces the runtime to use the interpreter
    /// </summary>
    public bool ForceInterpreter { get; set; }

    /// <summary>
    /// Enables detailed runtime logging
    /// </summary>
    public bool EnableRuntimeLogging { get; set; }

    /// <summary>
    /// Enables App Sandbox for Mac Catalyst apps
    /// </summary>
    public bool EnableAppSandbox { get; set; }

    /// Strip local symbols and debug information, and extract it in XcodeProjectPath directory
    /// </summary>
    public bool StripSymbolTable { get; set; }

    /// <summary>
    /// Bundles the application for NativeAOT runtime. Default runtime is Mono.
    /// </summary>
    public bool UseNativeAOTRuntime { get; set; }

    /// <summary>
    /// Extra native dependencies to link into the app
    /// </summary>
    public string[] NativeDependencies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Mode to control whether runtime is a self-contained library or not
    /// </summary>
    public bool IsLibraryMode { get; set; }

    public void ValidateRuntimeSelection()
    {
        if (UseNativeAOTRuntime)
        {
            if (!string.IsNullOrEmpty(MonoRuntimeHeaders))
                throw new ArgumentException($"Property \"{nameof(MonoRuntimeHeaders)}\" is not supported with NativeAOT runtime and will be ignored.");

            if (!string.IsNullOrEmpty(MainLibraryFileName))
                throw new ArgumentException($"Property \"{nameof(MainLibraryFileName)}\" is not supported with NativeAOT runtime and will be ignored.");

            if (ForceInterpreter)
                throw new ArgumentException($"Property \"{nameof(ForceInterpreter)}\" is not supported with NativeAOT runtime and will be ignored.");

            if (ForceAOT)
                throw new ArgumentException($"Property \"{nameof(ForceAOT)}\" is not supported with NativeAOT runtime and will be ignored.");

            if (RuntimeComponents.Length > 0)
                throw new ArgumentException($"Item \"{nameof(RuntimeComponents)}\" is not supported with NativeAOT runtime and will be ignored.");

            if (!string.IsNullOrEmpty(DiagnosticPorts))
                throw new ArgumentException($"Property \"{nameof(DiagnosticPorts)}\" is not supported with NativeAOT runtime and will be ignored.");

            if (EnableRuntimeLogging)
                throw new ArgumentException($"Property \"{nameof(EnableRuntimeLogging)}\" is not supported with NativeAOT runtime and will be ignored.");
        }
        else
        {
            if (string.IsNullOrEmpty(MonoRuntimeHeaders))
                throw new ArgumentException($"The \"{nameof(AppleAppBuilderTask)}\" task was not given a value for the required parameter \"{nameof(MonoRuntimeHeaders)}\" when using Mono runtime.");
        }
    }

    public override bool Execute()
    {
        bool shouldStaticLink = !EnableAppSandbox;
        bool isDevice = (TargetOS == TargetNames.iOS || TargetOS == TargetNames.tvOS);

        ValidateRuntimeSelection();

        if (!string.IsNullOrEmpty(MainLibraryFileName))
        {
            if (!File.Exists(Path.Combine(AppDir, MainLibraryFileName)))
            {
                throw new ArgumentException($"MainLibraryFileName='{MainLibraryFileName}' was not found in AppDir='{AppDir}'");
            }
        }

        if (ProjectName.Contains(' '))
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

        List<string> assemblerFiles = new List<string>();
        List<string> assemblerDataFiles = new List<string>();
        List<string> assemblerFilesToLink = new List<string>();

        if (!IsLibraryMode)
        {
            foreach (ITaskItem file in Assemblies)
            {
                // use AOT files if available
                string obj = file.GetMetadata("AssemblerFile");
                string llvmObj = file.GetMetadata("LlvmObjectFile");
                string dataFile = file.GetMetadata("AotDataFile");

                if (!string.IsNullOrEmpty(obj))
                {
                    assemblerFiles.Add(obj);
                }

                if (!string.IsNullOrEmpty(dataFile))
                {
                    assemblerDataFiles.Add(dataFile);
                }

                if (!string.IsNullOrEmpty(llvmObj))
                {
                    assemblerFilesToLink.Add(llvmObj);
                }
            }

            if (!ForceInterpreter && (shouldStaticLink || ForceAOT) && (assemblerFiles.Count == 0 && !UseNativeAOTRuntime))
            {
                throw new InvalidOperationException("Need list of AOT files for static linked builds.");
            }
        }

        if (!string.IsNullOrEmpty(DiagnosticPorts) && !Array.Exists(RuntimeComponents, runtimeComponent => string.Equals(runtimeComponent, "diagnostics_tracing", StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Using DiagnosticPorts requires diagnostics_tracing runtime component, which was not included in 'RuntimeComponents' item group. @RuntimeComponents: '{string.Join(", ", RuntimeComponents)}'");
        }

        if (EnableAppSandbox && (string.IsNullOrEmpty(DevTeamProvisioning) || DevTeamProvisioning == "-"))
        {
            throw new ArgumentException("DevTeamProvisioning must be set to a valid value when App Sandbox is enabled, using '-' is not supported.");
        }

        foreach (var nativeDependency in NativeDependencies)
        {
            assemblerFilesToLink.Add(nativeDependency);
        }

        List<string> extraLinkerArgs = new List<string>();
        foreach(ITaskItem item in ExtraLinkerArguments)
        {
            extraLinkerArgs.Add(item.ItemSpec);
        }

        var generator = new Xcode(Log, TargetOS, Arch);

        if (GenerateXcodeProject)
        {
            XcodeProjectPath = generator.GenerateXCode(ProjectName, MainLibraryFileName, assemblerFiles, assemblerDataFiles, assemblerFilesToLink, extraLinkerArgs, excludes,
                AppDir, binDir, MonoRuntimeHeaders, !shouldStaticLink, UseConsoleUITemplate, ForceAOT, ForceInterpreter, InvariantGlobalization, HybridGlobalization, Optimized, EnableRuntimeLogging, EnableAppSandbox, DiagnosticPorts, RuntimeComponents, NativeMainSource, UseNativeAOTRuntime, IsLibraryMode);

            if (BuildAppBundle)
            {
                if (isDevice && string.IsNullOrEmpty(DevTeamProvisioning))
                {
                    // DevTeamProvisioning shouldn't be empty for arm64 builds
                    Log.LogMessage(MessageImportance.High, "DevTeamProvisioning is not set, BuildAppBundle step is skipped.");
                }
                else
                {
                    string appDir = generator.BuildAppBundle(XcodeProjectPath, Optimized, DevTeamProvisioning);
                    AppBundlePath = Xcode.GetAppPath(appDir, XcodeProjectPath);

                    if (StripSymbolTable)
                    {
                        generator.StripApp(XcodeProjectPath, AppBundlePath);
                    }

                    generator.LogAppSize(AppBundlePath);
                }
            }
        }
        else if (GenerateCMakeProject)
        {
             generator.GenerateCMake(ProjectName, MainLibraryFileName, assemblerFiles, assemblerDataFiles, assemblerFilesToLink, extraLinkerArgs, excludes,
                AppDir, binDir, MonoRuntimeHeaders, !shouldStaticLink, UseConsoleUITemplate, ForceAOT, ForceInterpreter, InvariantGlobalization, HybridGlobalization, Optimized, EnableRuntimeLogging, EnableAppSandbox, DiagnosticPorts, RuntimeComponents, NativeMainSource, UseNativeAOTRuntime, IsLibraryMode);
        }

        return true;
    }
}
