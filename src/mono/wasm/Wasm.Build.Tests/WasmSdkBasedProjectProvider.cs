// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Linq;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmSdkBasedProjectProvider : ProjectProviderBase
{
    private readonly string _defaultTargetFramework;
    public WasmSdkBasedProjectProvider(ITestOutputHelper _testOutput, string defaultTargetFramework, string? _projectDir = null)
            : base(_testOutput, _projectDir)
    {
        _defaultTargetFramework = defaultTargetFramework;
    }

    protected override string BundleDirName { get { return "wwwroot"; } }

    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(AssertBundleOptions assertOptions)
    {
        var result = new SortedDictionary<string, bool>()
        {
            { "dotnet.js", false },
            { "dotnet.js.map", false },
            { "dotnet.native.js", true },
            { "dotnet.native.js.symbols", false },
            { "dotnet.native.wasm", true },
            { "dotnet.native.worker.mjs", true },
            { "dotnet.runtime.js", true },
            { "dotnet.runtime.js.map", false },
            { "dotnet.diagnostics.js", true },
            { "dotnet.diagnostics.js.map", false },
        };

        if (assertOptions.BuildOptions.BootConfigFileName.EndsWith(".js"))
            result[assertOptions.BuildOptions.BootConfigFileName] = false;

        return result;
    }

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(AssertBundleOptions assertOptions)
    {
        SortedSet<string> res = new()
        {
           "dotnet.js",
           "dotnet.native.wasm",
           "dotnet.native.js",
           "dotnet.runtime.js",
        };
        if (assertOptions.BuildOptions.RuntimeType is RuntimeVariant.MultiThreaded)
        {
            res.Add("dotnet.native.worker.mjs");
        }

        if (!assertOptions.BuildOptions.IsPublish)
        {
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (assertOptions.AssertSymbolsFile && assertOptions.ExpectSymbolsFile)
            res.Add("dotnet.native.js.symbols");

        if (assertOptions.BuildOptions.WasmPerfTracing)
        {
            res.Add("dotnet.diagnostics.js");
            if (!assertOptions.BuildOptions.IsPublish)
                res.Add("dotnet.diagnostics.js.map");
        }

        if (assertOptions.BuildOptions.BootConfigFileName.EndsWith(".js"))
            res.Add(assertOptions.BuildOptions.BootConfigFileName);

        return res;
    }

    public NativeFilesType GetExpectedFileType(Configuration config, bool isAOT, bool isPublish, bool isUsingWorkloads, bool? isNativeBuild=null) =>
        isNativeBuild == true ? NativeFilesType.Relinked : // precedence over build/publish check: build with -p:WasmBuildNative=true should use relinked
        !isPublish ? NativeFilesType.FromRuntimePack : // precedence over AOT check: build with AOT should use runtime pack
        isAOT ? NativeFilesType.AOT : // precedence over -p:WasmBuildNative=false check: publish with AOT relinks regardless of WasmBuildNative value
        isNativeBuild == false ? NativeFilesType.FromRuntimePack :
        (config == Configuration.Release) ? NativeFilesType.Relinked :
        NativeFilesType.FromRuntimePack;

    public void AssertBundle(Configuration config, MSBuildOptions buildOptions, bool isUsingWorkloads, bool? isNativeBuild = null)
    {
        string frameworkDir = string.IsNullOrEmpty(buildOptions.NonDefaultFrameworkDir) ?
            GetBinFrameworkDir(config, buildOptions.IsPublish, _defaultTargetFramework) :
            buildOptions.NonDefaultFrameworkDir;

        AssertBundle(new AssertBundleOptions(
            config,
            BuildOptions: buildOptions,
            ExpectedFileType: GetExpectedFileType(config, buildOptions.AOT, buildOptions.IsPublish, isUsingWorkloads, isNativeBuild),
            BinFrameworkDir: frameworkDir,
            ExpectSymbolsFile: true,
            AssertIcuAssets: true,
            AssertSymbolsFile: false
        ));
    }

    private void AssertBundle(AssertBundleOptions assertOptions)
    {
        IReadOnlyDictionary<string, DotNetFileName> actualDotnetFiles = AssertBasicBundle(assertOptions);

        if (assertOptions.BuildOptions.IsPublish)
        {
            string publishPath = Path.GetFullPath(Path.Combine(assertOptions.BinFrameworkDir, "..", ".."));
            Assert.Equal("publish", Path.GetFileName(publishPath));

            var dlls = Directory.EnumerateFiles(publishPath, "*.dll");
            Assert.False(dlls.Any(), $"Did not expect to find any .dll in {publishPath} but found {string.Join(",", dlls)}");

            var wasmAssemblies = Directory.EnumerateFiles(publishPath, "*.wasm");
            Assert.False(wasmAssemblies.Any(), $"Did not expect to find any .wasm files in {publishPath} but found {string.Join(",", wasmAssemblies)}");
        }

        if (!BuildTestBase.IsUsingWorkloads)
            return;

        // Compare files with the runtime pack
        string objBuildDir = Path.Combine(ProjectDir!, "obj", assertOptions.Configuration.ToString(), assertOptions.BuildOptions.TargetFramework, "wasm", assertOptions.BuildOptions.IsPublish ? "for-publish" : "for-build");

        string runtimeNativeDir = BuildTestBase.s_buildEnv.GetRuntimeNativeDir(assertOptions.BuildOptions.TargetFramework, assertOptions.BuildOptions.RuntimeType);

        string srcDirForNativeFileToCompareAgainst = assertOptions.ExpectedFileType switch
        {
            NativeFilesType.FromRuntimePack => runtimeNativeDir,
            NativeFilesType.Relinked => objBuildDir,
            NativeFilesType.AOT => objBuildDir,
            _ => throw new ArgumentOutOfRangeException(nameof(assertOptions.BuildOptions.ExpectedFileType))
        };

        string buildType = assertOptions.BuildOptions.IsPublish ? "publish" : "build";
        var nativeFilesToCheck = new List<string>() { "dotnet.native.wasm", "dotnet.native.js" };
        if (assertOptions.BuildOptions.RuntimeType == RuntimeVariant.MultiThreaded)
        {
            nativeFilesToCheck.Add("dotnet.native.worker.mjs");
        }

        foreach (string nativeFilename in nativeFilesToCheck)
        {
            if (!actualDotnetFiles.TryGetValue(nativeFilename, out DotNetFileName? dotnetFile))
            {
                throw new XunitException($"Could not find {nativeFilename}. Actual files on disk: {string.Join($"{Environment.NewLine}  ", actualDotnetFiles.Values.Select(a => a.ActualPath).Order())}");
            }
            // For any *type*, check against the expected path
            TestUtils.AssertSameFile(Path.Combine(srcDirForNativeFileToCompareAgainst, nativeFilename),
                           actualDotnetFiles[nativeFilename].ActualPath,
                           buildType);

            if (assertOptions.BuildOptions.ExpectedFileType != NativeFilesType.FromRuntimePack)
            {
                if (nativeFilename == "dotnet.native.worker.mjs")
                {
                    Console.WriteLine($"Skipping the verification whether {nativeFilename} is from the runtime pack. The check wouldn't be meaningful as the runtime pack file has the same size as the relinked file");
                    continue;
                }
                // Confirm that it doesn't match the file from the runtime pack
                TestUtils.AssertNotSameFile(Path.Combine(runtimeNativeDir, nativeFilename),
                                   actualDotnetFiles[nativeFilename].ActualPath,
                                   buildType);
            }
        }
    }

    public void AssertWasmSdkBundle(Configuration config, MSBuildOptions buildOptions, bool isUsingWorkloads, bool? isNativeBuild = null, string? buildOutput = null)
    {
        if (isUsingWorkloads && buildOutput is not null)
        {
            // In no-workload case, the path would be from a restored nuget
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildOptions.TargetFramework ?? _defaultTargetFramework, buildOptions.RuntimeType);
        }
        AssertBundle(config, buildOptions, isUsingWorkloads, isNativeBuild);
    }

    public BuildPaths GetBuildPaths(Configuration configuration, bool forPublish)
    {
        Assert.NotNull(ProjectDir);
        string configStr = configuration.ToString();
        string objDir = Path.Combine(ProjectDir, "obj", configStr, _defaultTargetFramework);
        string binDir = Path.Combine(ProjectDir, "bin", configStr, _defaultTargetFramework);
        string binFrameworkDir = GetBinFrameworkDir(configuration, forPublish, _defaultTargetFramework);

        string objWasmDir = Path.Combine(objDir, "wasm", forPublish ? "for-publish" : "for-build");
        // for build: we should take from runtime pack?
        return new BuildPaths(objWasmDir, objDir, binDir, binFrameworkDir);
    }

    public override string GetBinFrameworkDir(Configuration config, bool forPublish, string framework, string? projectDir = null)
    {
        EnsureProjectDirIsSet();
        string basePath = Path.Combine(projectDir ?? ProjectDir!, "bin", config.ToString(), framework);
        if (forPublish)
            basePath = Path.Combine(basePath, "publish");

        return Path.Combine(basePath, BundleDirName, "_framework");
    }
}
