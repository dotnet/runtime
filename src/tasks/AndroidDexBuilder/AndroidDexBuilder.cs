// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

public class AndroidDexBuilderTask : Task
{
    /// <summary>
    /// List of paths to java files to be compiled and packaged into the .dex file.
    /// </summary>
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
        DexFilePath = CompileJava();
        return true;
    }

    private string CompileJava()
    {
        // ---- init

        if (string.IsNullOrEmpty(AndroidSdk))
            AndroidSdk = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");

        if (string.IsNullOrEmpty(AndroidSdk))
            throw new ArgumentException($"Android SDK='{AndroidSdk}' was not found or empty (can be set via ANDROID_SDK_ROOT envvar).");

        // Try to get the latest build-tools version if not specified
        if (string.IsNullOrEmpty(BuildToolsVersion))
            BuildToolsVersion = GetLatestBuildTools(AndroidSdk);

        string buildToolsFolder = Path.Combine(AndroidSdk, "build-tools", BuildToolsVersion);
        if (!Directory.Exists(buildToolsFolder))
            throw new ArgumentException($"Build tools folder '{buildToolsFolder}' was not found.");

        // Try to get the latest API level if not specified
        if (string.IsNullOrEmpty(BuildApiLevel))
            BuildApiLevel = GetLatestApiLevel(AndroidSdk);
        string androidJar = Path.Combine(AndroidSdk, "platforms", "android-" + BuildApiLevel, "android.jar");

        // ---- compile java

        var objDir = Path.Combine(OutputDir, "obj");
        Directory.CreateDirectory(objDir);

        string javaCompilerArgs = $"-d obj -classpath src -bootclasspath {androidJar} -source 1.8 -target 1.8 ";
        foreach (var file in JavaFiles)
            Utils.RunProcess(Log, "javac", javaCompilerArgs + file.ItemSpec, workingDir: OutputDir);

        // ---- pack classes in dex

        string d8 = Path.Combine(buildToolsFolder, "d8");
        if (File.Exists(d8))
        {
            string[] classFiles = Directory.GetFiles(objDir, "*.class", SearchOption.AllDirectories);

            if (!classFiles.Any())
                throw new InvalidOperationException("Didn't find any .class files");

            Utils.RunProcess(Log, d8, $"--no-desugaring {string.Join(" ", classFiles)}", workingDir: OutputDir);
        }
        else
        {
            string dx = Path.Combine(buildToolsFolder, "dx");
            Utils.RunProcess(Log, dx, "--dex --output=classes.dex obj", workingDir: OutputDir);
        }

        Directory.Delete(objDir, recursive: true);

        var dexPath = Path.Combine(OutputDir, DexFileName);
        File.Move(Path.Combine(OutputDir, "classes.dex"), dexPath, overwrite: true);

        return dexPath;
    }

    /// <summary>
    /// Scan android SDK for build tools (ignore preview versions)
    /// </summary>
    private static string GetLatestBuildTools(string androidSdkDir)
    {
        string? buildTools = Directory.GetDirectories(Path.Combine(androidSdkDir, "build-tools"))
            .Select(Path.GetFileName)
            .Where(file => !file!.Contains('-'))
            .Select(file => { Version.TryParse(Path.GetFileName(file), out Version? version); return version; })
            .OrderByDescending(v => v)
            .FirstOrDefault()?.ToString();

        if (string.IsNullOrEmpty(buildTools))
            throw new ArgumentException($"Android SDK ({androidSdkDir}) doesn't contain build-tools.");

        return buildTools;
    }

    /// <summary>
    /// Scan android SDK for api levels (ignore preview versions)
    /// </summary>
    private static string GetLatestApiLevel(string androidSdkDir)
    {
        return Directory.GetDirectories(Path.Combine(androidSdkDir, "platforms"))
            .Select(file => int.TryParse(Path.GetFileName(file).Replace("android-", ""), out int apiLevel) ? apiLevel : -1)
            .OrderByDescending(v => v)
            .FirstOrDefault()
            .ToString();
    }
}
