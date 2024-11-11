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
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", true },
               { "dotnet.native.js.symbols", false },
               { "dotnet.globalization.js", true },
               { "dotnet.native.wasm", true },
               { "dotnet.native.worker.mjs", true },
               { "dotnet.runtime.js", true },
               { "dotnet.runtime.js.map", false },
            };

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
        if (assertOptions.BuildOptions.GlobalizationMode is GlobalizationMode.Hybrid)
        {
            res.Add("dotnet.globalization.js");
        }

        if (!assertOptions.BuildOptions.IsPublish)
        {
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (assertOptions.AssertSymbolsFile && assertOptions.ExpectSymbolsFile)
            res.Add("dotnet.native.js.symbols");

        return res;
    }


    public void AssertBundle(BuildProjectOptions buildOptions)
    {
        AssertBundle(new AssertBundleOptions(
            BuildOptions: buildOptions,
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
            string publishPath = Path.GetFullPath(Path.Combine(assertOptions.BuildOptions.BinFrameworkDir, "..", ".."));
            Assert.Equal("publish", Path.GetFileName(publishPath));

            var dlls = Directory.EnumerateFiles(publishPath, "*.dll");
            Assert.False(dlls.Any(), $"Did not expect to find any .dll in {publishPath} but found {string.Join(",", dlls)}");

            var wasmAssemblies = Directory.EnumerateFiles(publishPath, "*.wasm");
            Assert.False(wasmAssemblies.Any(), $"Did not expect to find any .wasm files in {publishPath} but found {string.Join(",", wasmAssemblies)}");
        }

        if (!BuildTestBase.IsUsingWorkloads)
            return;

        // Compare files with the runtime pack
        string objBuildDir = Path.Combine(ProjectDir!, "obj", assertOptions.BuildOptions.Configuration, assertOptions.BuildOptions.TargetFramework, "wasm", assertOptions.BuildOptions.IsPublish ? "for-publish" : "for-build");

        string runtimeNativeDir = BuildTestBase.s_buildEnv.GetRuntimeNativeDir(assertOptions.BuildOptions.TargetFramework, assertOptions.BuildOptions.RuntimeType);

        string srcDirForNativeFileToCompareAgainst = assertOptions.BuildOptions.ExpectedFileType switch
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
        if (assertOptions.BuildOptions.GlobalizationMode == GlobalizationMode.Hybrid)
        {
            nativeFilesToCheck.Add("dotnet.globalization.js");
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

    public void AssertWasmSdkBundle(BuildProjectOptions buildOptions, string? buildOutput = null)
    {
        if (buildOutput is not null)
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildOptions.TargetFramework ?? _defaultTargetFramework);
        AssertBundle(buildOptions);
    }
    
    public BuildPaths GetBuildPaths(ProjectInfo info, bool forPublish)
    {
        Assert.NotNull(ProjectDir);
        string objDir = Path.Combine(ProjectDir, "obj", info.Configuration, _defaultTargetFramework);
        string binDir = Path.Combine(ProjectDir, "bin", info.Configuration, _defaultTargetFramework);
        string binFrameworkDir = GetBinFrameworkDir(info.Configuration, forPublish, _defaultTargetFramework);
        
        string objWasmDir = Path.Combine(objDir, "wasm", forPublish ? "for-publish" : "for-build");
        // for build: we should take from runtime pack?
        return new BuildPaths(objWasmDir, objDir, binDir, binFrameworkDir);
    }
    
    public override string GetBinFrameworkDir(string config, bool forPublish, string framework, string? projectDir = null)
    {
        EnsureProjectDirIsSet();
        string basePath = Path.Combine(projectDir ?? ProjectDir!, "bin", config, framework);
        if (forPublish)
            basePath = Path.Combine(basePath, "publish");

        return Path.Combine(basePath, BundleDirName, "_framework");
    }
}