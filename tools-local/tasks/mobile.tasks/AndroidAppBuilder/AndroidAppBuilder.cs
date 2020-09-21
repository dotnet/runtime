// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidAppBuilderTask : Task
{
    [Required]
    public string SourceDir { get; set; } = ""!;

    [Required]
    public string MonoRuntimeHeaders { get; set; } = ""!;

    /// <summary>
    /// This library will be used as an entry-point (e.g. TestRunner.dll)
    /// </summary>
    [Required]
    public string MainLibraryFileName { get; set; } = ""!;

    /// <summary>
    /// Target arch, can be 'x86', 'x86_64', 'armeabi-v7a' or 'arm64-v8a'
    /// </summary>
    [Required]
    public string Abi { get; set; } = ""!;

    public string? ProjectName { get; set; }

    public string? OutputDir { get; set; }

    public string? AndroidSdk { get; set; }

    public string? AndroidNdk { get; set; }

    public string? MinApiLevel { get; set; }

    public string? BuildApiLevel { get; set; }

    public string? BuildToolsVersion { get; set; }

    public bool StripDebugSymbols { get; set; }

    [Output]
    public string ApkBundlePath { get; set; } = ""!;

    [Output]
    public string ApkPackageId { get; set; } = ""!;

    public override bool Execute()
    {
        Utils.Logger = Log;

        var apkBuilder = new ApkBuilder();
        apkBuilder.ProjectName = ProjectName;
        apkBuilder.OutputDir = OutputDir;
        apkBuilder.AndroidSdk = AndroidSdk;
        apkBuilder.AndroidNdk = AndroidNdk;
        apkBuilder.MinApiLevel = MinApiLevel;
        apkBuilder.BuildApiLevel = BuildApiLevel;
        apkBuilder.BuildToolsVersion = BuildToolsVersion;
        apkBuilder.StripDebugSymbols = StripDebugSymbols;
        (ApkBundlePath, ApkPackageId) = apkBuilder.BuildApk(SourceDir, Abi, MainLibraryFileName, MonoRuntimeHeaders);

        return true;
    }
}
