// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidApkFileReplacerTask : Task
{
    [Required]
    public string FilePath { get; set; } = ""!;

    [Required]
    public string OutputDir { get; set; } = ""!;

    public string? AndroidSdk { get; set; }

    public string? MinApiLevel { get; set; }

    public string? BuildToolsVersion { get; set; }

    public string? KeyStorePath { get; set; }


    public override bool Execute()
    {
        var apkBuilder = new ApkBuilder(Log);
        apkBuilder.OutputDir = OutputDir;
        apkBuilder.AndroidSdk = AndroidSdk;
        apkBuilder.MinApiLevel = MinApiLevel;
        apkBuilder.BuildToolsVersion = BuildToolsVersion;
        apkBuilder.KeyStorePath = KeyStorePath;
        apkBuilder.ReplaceFileInApk(FilePath);
        return true;
    }
}
