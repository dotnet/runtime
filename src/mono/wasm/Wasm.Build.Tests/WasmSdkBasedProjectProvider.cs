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
    public WasmSdkBasedProjectProvider(ITestOutputHelper _testOutput, string? _projectDir = null)
            : base(_testOutput, _projectDir)
    {}

    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(AssertBundleOptionsBase assertOptions)
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", true },
               { "dotnet.native.js.symbols", false },
               { "dotnet.native.wasm", false },
               { "dotnet.native.worker.js", true },
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
            res.Add("dotnet.native.worker.js");
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


    public void AssertBundle(BuildArgs buildArgs, BuildProjectOptions buildProjectOptions)
    {
        AssertBundle(new(
            Config: buildArgs.Config,
            IsPublish: buildProjectOptions.Publish,
            TargetFramework: buildProjectOptions.TargetFramework,
            BinFrameworkDir: buildProjectOptions.BinFrameworkDir ?? FindBinFrameworkDir(buildArgs.Config, buildProjectOptions.Publish, buildProjectOptions.TargetFramework),
            PredefinedIcudt: buildProjectOptions.PredefinedIcudt,
            GlobalizationMode: buildProjectOptions.GlobalizationMode,
            AssertSymbolsFile: false,
            ExpectedFileType: buildProjectOptions.Publish && buildArgs.Config == "Release" ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack
        ));
    }

    public void AssertBundle(AssertWasmSdkBundleOptions assertOptions)
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
        var nativeFilesToCheck = new List<string>() { "dotnet.native.wasm", "dotnet.native.js" }
        if (assertOptions.RuntimeType == RuntimeVariant.MultiThreaded)
            nativeFilesToCheck.Add("dotnet.native.worker.js");
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
                // Confirm that it doesn't match the file from the runtime pack
                TestUtils.AssertNotSameFile(Path.Combine(runtimeNativeDir, nativeFilename),
                                   actualDotnetFiles[nativeFilename].ActualPath,
                                   buildType);
            }
        }
    }
}
