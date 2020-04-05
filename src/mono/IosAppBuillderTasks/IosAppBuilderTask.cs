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

public class IosAppBuilderTask : Task
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
    public string MonoInclude { get; set; } = ""!;

    /// <summary>
    /// This library will be used as an entry-point (e.g. TestRunner.dll)
    /// </summary>
    [Required]
    public string EntryPointLib { get; set; } = ""!;

    /// <summary>
    /// Path to a custom main.m with custom UI
    /// A default one is used if it's not set
    /// </summary>
    public string? NativeMainSource { get; set; }

    /// <summary>
    /// Produce optimized binaries (e.g. use -O2 in AOT)
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
    /// Path to *.app bundle
    /// </summary>
    [Output]
    public string AppBundlePath { get; set; } = ""!;

    public override bool Execute()
    {
        Utils.Logger = Log;
        bool isDevice = Arch.Equals("arm64", StringComparison.InvariantCultureIgnoreCase);
        if (isDevice && string.IsNullOrEmpty(CrossCompiler))
        {
            throw new ArgumentException("arm64 arch requires CrossCompiler");
        }

        var stopWatch = Stopwatch.StartNew();

        // see https://github.com/dotnet/runtime/issues/34448
        // Can be exposed as an input argument if needed
        string[] excludes = {"System.Runtime.WindowsRuntime.dll"};
        string[] libsToAot = Directory.GetFiles(AppDir, "*.dll")
            .Where(f => !excludes.Contains(Path.GetFileName(f)))
            .ToArray();

        string binDir = Path.Combine(AppDir, $"bin-{ProjectName}-{Arch}");
        Directory.CreateDirectory(binDir);

        // run AOT compilation only for devices
        if (Arch == "arm64")
        {
            if (string.IsNullOrEmpty(CrossCompiler))
                throw new InvalidOperationException("cross-compiler is not set");

            AotCompiler.PrecompileLibraries(CrossCompiler, binDir, libsToAot,
                new Dictionary<string, string>
                {
                    {"DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1"},
                    {"MONO_PATH", AppDir},
                },
                Optimized);
        }

        // generate modules.m
        AotCompiler.GenerateLinkAllFile(
            Directory.GetFiles(binDir, "*.dll.o"),
            Path.Combine(binDir, "modules.m"));

        // generate xcode project
        Xcode.GenerateXCode(ProjectName, EntryPointLib, AppDir, binDir, MonoInclude, NativeMainSource);

        // build app
        string appBundle = Xcode.BuildAppBundle(
            Path.Combine(binDir, ProjectName, ProjectName + ".xcodeproj"),
            Arch, Optimized, DevTeamProvisioning);

        stopWatch.Stop();
        Debug.Assert(Directory.Exists(appBundle)); // it's actually a directory
        Log.LogMessage(MessageImportance.High, $"App: {appBundle} in {stopWatch.Elapsed.TotalSeconds:F1} s.");

        AppBundlePath = appBundle;
        return true;
    }
}
