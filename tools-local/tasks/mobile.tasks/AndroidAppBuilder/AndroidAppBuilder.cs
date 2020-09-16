// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System.Reflection;
using System.Collections.Generic;

public class AndroidAppBuilderTask : Task
{
    [Required]
    public string SourceDir { get; set; } = ""!;

    [Required]
    public string MonoRuntimeHeaders { get; set; } = ""!;

    /// <summary>
    /// This library will be used as an entry-point (e.g. TestRunner.dll)
    /// </summary>
    public string MainLibraryFileName { get; set; } = ""!;

    [Required]
    public string RuntimeIdentifier { get; set; } = ""!;

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
        if (LoadDependencies)
        {
            var paths = new List<string>();
            
            // Collect and load assemblies used by the app
            foreach (var v in AssemblySearchPaths!)
            {
                var dir = v.ItemSpec;
                if (!Directory.Exists(dir))
                    throw new ArgumentException($"Directory '{dir}' doesn't exist or not a directory.");
                paths.Add(dir);
            }
            _resolver = new Resolver(paths);
            var mlc = new MetadataLoadContext(_resolver, "System.Private.CoreLib");

            var mainAssembly = mlc.LoadFromAssemblyPath(MainAssembly);
            Add(mlc, mainAssembly);

            if (ExtraAssemblies != null)
            {
                foreach (var item in ExtraAssemblies)
                {
            try
                {
                        var refAssembly = mlc.LoadFromAssemblyPath(item.ItemSpec);
                        Add(mlc, refAssembly);
            }
            catch (System.IO.FileLoadException)
            {
                if (!SkipMissingAssemblies)
                {
                    throw;
                }
            }
                }
            }
        }

        Utils.Logger = Log;

        string abi = DetermineAbi();

        var apkBuilder = new ApkBuilder();
        apkBuilder.ProjectName = ProjectName;
        apkBuilder.OutputDir = OutputDir;
        apkBuilder.AndroidSdk = AndroidSdk;
        apkBuilder.AndroidNdk = AndroidNdk;
        apkBuilder.MinApiLevel = MinApiLevel;
        apkBuilder.BuildApiLevel = BuildApiLevel;
        apkBuilder.BuildToolsVersion = BuildToolsVersion;
        apkBuilder.StripDebugSymbols = StripDebugSymbols;
        (ApkBundlePath, ApkPackageId) = apkBuilder.BuildApk(SourceDir, abi, MainLibraryFileName, MonoRuntimeHeaders);

        return true;
    }

    private string DetermineAbi()
    {
        switch (RuntimeIdentifier)
        {
            case "android-x86":
                return "x86";
            case "android-x64":
                return "x86_64";
            case "android-arm":
                return "armeabi-v7a";
            case "android-arm64":
                return "arm64-v8a";
            default:
                throw new ArgumentException(RuntimeIdentifier + " is not supported for Android");
        }
    }
}
