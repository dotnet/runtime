// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidLibBuilderTask : Task
{
    [Required]
    public string JavaSourceDirectory { get; set; } = ""!;

    [Required]
    public string OutputDir { get; set; } = ""!;

    [Required]
    public string DexFileName { get; set; } = ""!;

    [Required]
    public string JarFileName { get; set; } = ""!;

    public string? AndroidSdk { get; set; }

    public string? BuildApiLevel { get; set; }

    public string? BuildToolsVersion { get; set; }

    public override bool Execute()
    {
        var androidSdk = new AndroidSdkHelper(
            androidSdkPath: AndroidSdk,
            buildApiLevel: BuildApiLevel,
            buildToolsVersion: BuildToolsVersion);

        var objDir = Path.Combine(OutputDir, "obj");
        Directory.CreateDirectory(objDir);

        try
        {
            CompileJava(objDir, androidSdk);
            BuildJar(objDir);
            BuildDex(objDir, androidSdk);

            return true;
        }
        finally
        {
            Directory.Delete(objDir, recursive: true);
        }
    }

    private void CompileJava(string objDir, AndroidSdkHelper androidSdk)
    {
        var compiler = new JavaCompiler(Log, androidSdk, workingDir: JavaSourceDirectory);
        string[] javaFiles = Directory.GetFiles(JavaSourceDirectory, "*.java", SearchOption.AllDirectories);
        foreach (var file in javaFiles)
        {
            compiler.Compile(file, outputDir: objDir);
        }
    }

    private void BuildJar(string objDir)
    {
        var jarBuilder = new JarBuilder(Log);
        var jarFilePath = Path.Combine(OutputDir, JarFileName);
        jarBuilder.Build(inputDir: objDir, outputFileName: jarFilePath);

        Log.LogMessage(MessageImportance.High, $"Built {jarFilePath}");
    }

    private void BuildDex(string objDir, AndroidSdkHelper androidSdk)
    {
        var dexBuilder = new DexBuilder(Log, androidSdk, workingDir: OutputDir);
        var dexFilePath = Path.Combine(OutputDir, DexFileName);
        dexBuilder.Build(inputDir: objDir, outputFileName: dexFilePath);

        Log.LogMessage(MessageImportance.High, $"Built {dexFilePath}");
    }
}
