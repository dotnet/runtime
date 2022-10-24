// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidDexBuilderTask : Task
{
    public ITaskItem[] JavaFiles { get; set; } = Array.Empty<ITaskItem>();

    [Required]
    public string OutputDir { get; set; } = ""!;

    [Required]
    public string DexFileName { get; set; } = ""!;

    public string? AndroidSdk { get; set; }

    public string? BuildApiLevel { get; set; }

    public string? BuildToolsVersion { get; set; }

    [Output]
    public string DexFilePath { get; set; } = ""!;

    public override bool Execute()
    {
        var androidSdk = new AndroidSdkHelper(
            androidSdkPath: AndroidSdk,
            buildApiLevel: BuildApiLevel,
            buildToolsVersion: BuildToolsVersion);

        var compiler = new JavaCompiler(Log, androidSdk, workingDir: OutputDir);
        var dexBuilder = new DexBuilder(Log, androidSdk, workingDir: OutputDir);

        var objDir = "obj";
        var objPath = Path.Combine(OutputDir, objDir);
        Directory.CreateDirectory(objPath);

        try
        {
            foreach (var file in JavaFiles)
            {
                compiler.Compile(file.ItemSpec, outputDir: objDir);
            }

            dexBuilder.Build(inputDir: objDir, outputFileName: DexFileName);

            DexFilePath = Path.Combine(OutputDir, DexFileName);
            return true;
        }
        finally
        {
            Directory.Delete(objPath, recursive: true);
        }
    }
}
