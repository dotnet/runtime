// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidAppBuilderTask : Task
{
    [Required]
    public string ProjectName { get; set; } = ""!;

    [Required]
    public string AppDir { get; set; } = ""!;

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
    /// Target arch, can be 'x86', 'x86_64', 'armeabi', 'armeabi-v7a' or 'arm64-v8a'
    /// </summary>
    [Required]
    public string Abi { get; set; } = ""!;

    [Required]
    public string AndroidSdk { get; set; } = ""!;
    
    [Required]
    public string AndroidNdk { get; set; } = ""!;
    
    /// <summary>
    /// Minimal Android API (21 by default)
    /// </summary>
    public string MinApiLevel { get; set; } = "21"!;
    
    public string? BuildApiLevel { get; set; }
    
    public string? BuildToolsVersion { get; set; }

    [Output]
    public string ApkBundlePath { get; set; } = ""!;
    
    [Output]
    public string ApkPackageId { get; set; } = ""!;

    public override bool Execute()
    {
        Utils.Logger = Log;

        if (!File.Exists(Path.Combine(AppDir, MainLibraryFileName)))
        {
            throw new ArgumentException($"MainLibraryFileName='{MainLibraryFileName}' was not found in AppDir='{AppDir}'");
        }

        string binDir = Path.Combine(AppDir, $"bin-{ProjectName}-{Abi}");
        if (!string.IsNullOrEmpty(OutputDirectory))
        {
            binDir = OutputDirectory;
        }
        Directory.CreateDirectory(binDir);

        (ApkBundlePath, ApkPackageId) = ApkBuilder.BuildApk(ProjectName, binDir, AppDir, MainLibraryFileName, MonoRuntimeHeaders,
            AndroidSdk, AndroidNdk, Abi, MinApiLevel, BuildApiLevel, BuildToolsVersion);
        
        return true;
    }
}
