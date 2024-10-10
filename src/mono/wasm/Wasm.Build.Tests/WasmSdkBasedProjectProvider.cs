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
        IsFingerprintingSupported = true;
    }
    protected override string BundleDirName { get { return "wwwroot"; } }

    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(AssertBundleOptionsBase assertOptions)
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

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(AssertBundleOptionsBase assertOptions)
    {
        SortedSet<string> res = new()
        {
           "dotnet.js",
           "dotnet.native.wasm",
           "dotnet.native.js",
           "dotnet.runtime.js",
        };
        if (assertOptions.RuntimeType is RuntimeVariant.MultiThreaded)
        {
            res.Add("dotnet.native.worker.mjs");
        }
        if (assertOptions.GlobalizationMode is GlobalizationMode.Hybrid)
        {
            res.Add("dotnet.globalization.js");
        }

        if (!assertOptions.IsPublish)
        {
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        if (assertOptions.AssertSymbolsFile && assertOptions.ExpectSymbolsFile)
            res.Add("dotnet.native.js.symbols");

        return res;
    }


    protected void AssertBundle(BuildArgs buildArgs, BuildProjectOptions buildProjectOptions)
    {
        string frameworkDir = buildProjectOptions.BinFrameworkDir ??
            FindBinFrameworkDir(buildArgs.Config, buildProjectOptions.Publish, buildProjectOptions.TargetFramework);
        AssertBundle(new(
            Config: buildArgs.Config,
            IsPublish: buildProjectOptions.Publish,
            TargetFramework: buildProjectOptions.TargetFramework,
            BinFrameworkDir: frameworkDir,
            CustomIcuFile: buildProjectOptions.CustomIcuFile,
            GlobalizationMode: buildProjectOptions.GlobalizationMode,
            AssertSymbolsFile: false,
            ExpectedFileType: buildProjectOptions.Publish && buildArgs.Config == "Release" ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack
        ));
    }

    protected void AssertBundle(AssertWasmSdkBundleOptions assertOptions)
    {
        IReadOnlyDictionary<string, DotNetFileName> actualDotnetFiles = AssertBasicBundle(assertOptions);

        if (assertOptions.IsPublish)
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
        string objBuildDir = Path.Combine(ProjectDir!, "obj", assertOptions.Config, assertOptions.TargetFramework, "wasm", assertOptions.IsPublish ? "for-publish" : "for-build");

        string runtimeNativeDir = BuildTestBase.s_buildEnv.GetRuntimeNativeDir(assertOptions.TargetFramework, assertOptions.RuntimeType);

        string srcDirForNativeFileToCompareAgainst = assertOptions.ExpectedFileType switch
        {
            NativeFilesType.FromRuntimePack => runtimeNativeDir,
            NativeFilesType.Relinked => objBuildDir,
            NativeFilesType.AOT => objBuildDir,
            _ => throw new ArgumentOutOfRangeException(nameof(assertOptions.ExpectedFileType))
        };
        string buildType = assertOptions.IsPublish ? "publish" : "build";
        var nativeFilesToCheck = new List<string>() { "dotnet.native.wasm", "dotnet.native.js" };
        if (assertOptions.RuntimeType == RuntimeVariant.MultiThreaded)
        {
            nativeFilesToCheck.Add("dotnet.native.worker.mjs");
        }
        if (assertOptions.GlobalizationMode == GlobalizationMode.Hybrid)
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

            if (assertOptions.ExpectedFileType != NativeFilesType.FromRuntimePack)
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
    
    public void AssertTestMainJsBundle(BuildArgs buildArgs,
                              BuildProjectOptions buildProjectOptions,
                              string? buildOutput = null,
                              AssertTestMainJsAppBundleOptions? assertAppBundleOptions = null)
    {
        if (buildOutput is not null)
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildProjectOptions.TargetFramework ?? _defaultTargetFramework);

        if (assertAppBundleOptions is not null)
            AssertBundle(assertAppBundleOptions);
        else
            AssertBundle(buildArgs, buildProjectOptions);
    }

    public void AssertWasmSdkBundle(BuildArgs buildArgs,
                              BuildProjectOptions buildProjectOptions,
                              string? buildOutput = null,
                              AssertWasmSdkBundleOptions? assertAppBundleOptions = null)
    {
        if (buildOutput is not null)
            ProjectProviderBase.AssertRuntimePackPath(buildOutput, buildProjectOptions.TargetFramework ?? _defaultTargetFramework);

        if (assertAppBundleOptions is not null)
            AssertBundle(assertAppBundleOptions);
        else
            AssertBundle(buildArgs, buildProjectOptions);
    }
    
    public override string FindBinFrameworkDir(string config, bool forPublish, string framework, string? projectDir = null)
    {
        EnsureProjectDirIsSet();
        string basePath = Path.Combine(projectDir ?? ProjectDir!, "bin", config, framework);
        if (forPublish)
            basePath = FindSubDirIgnoringCase(basePath, "publish");

        return Path.Combine(basePath, BundleDirName, "_framework");
    }
}