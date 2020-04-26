// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidAppBuilderTask : Task
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
    /// Path to store build artifacts
    /// </summary>
    public string? OutputDirectory { get; set; }

    /// <summary>
    /// Target arch, can be 'x86', 'x86_64', 'armeabi', 'armeabi-v7a' or 'arm64-v8a'
    /// </summary>
    [Required]
    public string Abi { get; set; } = ""!;

    /// <summary>
    /// Path to Android SDK
    /// </summary>
    [Required]
    public string AndroidSdk { get; set; } = ""!;
    
    /// <summary>
    /// Path to Android NDK
    /// </summary>
    [Required]
    public string AndroidNdk { get; set; } = ""!;
    
    /// <summary>
    /// Minimal Android API (21 by default)
    /// </summary>
    public string MinApiLevel { get; set; } = "21"!;
    
    /// <summary>
    /// Android API to build against (uses the latest available if not set)
    /// </summary>
    public string? BuildApiLevel { get; set; }
    
    /// <summary>
    /// Build-tools version (uses the latest available if not set)
    /// </summary>
    public string? BuildToolsVersion { get; set; }

    /// <summary>
    /// Path to *.apk bundle
    /// </summary>
    [Output]
    public string ApkBundlePath { get; set; } = ""!;
    
    /// <summary>
    /// Package unique id
    /// </summary>
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
