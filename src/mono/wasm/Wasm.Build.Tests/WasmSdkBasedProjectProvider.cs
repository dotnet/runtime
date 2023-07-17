// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using Microsoft.NET.Sdk.WebAssembly;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class WasmSdkBasedProjectProvider : ProjectProviderBase
{
    public WasmSdkBasedProjectProvider(ITestOutputHelper _testOutput, string? _projectDir = null)
            : base(_testOutput, _projectDir)
    {}

    protected override IReadOnlyDictionary<string, bool> GetAllKnownDotnetFilesToFingerprintMap(RuntimeVariant runtimeType)
        => new SortedDictionary<string, bool>()
            {
               { "dotnet.js", false },
               { "dotnet.js.map", false },
               { "dotnet.native.js", true },
               { "dotnet.native.wasm", false },
               { "dotnet.native.worker.js", true },
               { "dotnet.runtime.js", true },
               { "dotnet.runtime.js.map", false }
            };

    protected override IReadOnlySet<string> GetDotNetFilesExpectedSet(RuntimeVariant runtimeType, bool isPublish)
    {
        SortedSet<string> res = new()
        {
           "dotnet.js",
           "dotnet.native.wasm",
           "dotnet.native.js",
           "dotnet.runtime.js",
        };
        if (runtimeType is RuntimeVariant.MultiThreaded)
        {
            res.Add("dotnet.native.worker.js");
        }

        if (!isPublish)
        {
            res.Add("dotnet.js.map");
            res.Add("dotnet.runtime.js.map");
        }

        return res;
    }

    public void AssertDotNetNativeFiles(
        NativeFilesType type,
        string config,
        bool forPublish,
        string targetFramework,
        bool expectFingerprintOnDotnetJs,
        RuntimeVariant runtimeType = RuntimeVariant.SingleThreaded)
    {
        EnsureProjectDirIsSet();
        string label = forPublish ? "publish" : "build";
        string objBuildDir = Path.Combine(ProjectDir, "obj", config, targetFramework, "wasm", forPublish ? "for-publish" : "for-build");
        string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish, framework: targetFramework);

        var dotnetFiles = FindAndAssertDotnetFiles(
                            dir: binFrameworkDir,
                            isPublish: forPublish,
                            expectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs,
                            runtimeType: runtimeType);

        string runtimeNativeDir = _buildEnv.GetRuntimeNativeDir(targetFramework, runtimeType);

        string srcDirForNativeFileToCompareAgainst = type switch
        {
            NativeFilesType.FromRuntimePack => runtimeNativeDir,
            NativeFilesType.Relinked => objBuildDir,
            NativeFilesType.AOT => objBuildDir,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
        foreach (string nativeFilename in new[] { "dotnet.native.wasm", "dotnet.native.js" })
        {
            // For any *type*, check against the expected path
            TestUtils.AssertSameFile(Path.Combine(srcDirForNativeFileToCompareAgainst, nativeFilename),
                           dotnetFiles[nativeFilename].ActualPath,
                           label);

            if (type != NativeFilesType.FromRuntimePack)
            {
                // Confirm that it doesn't match the file from the runtime pack
                TestUtils.AssertNotSameFile(Path.Combine(runtimeNativeDir, nativeFilename),
                                   dotnetFiles[nativeFilename].ActualPath,
                                   label);
            }
        }
    }
    public void AssertBootJson(
        string binFrameworkDir,
        bool expectFingerprintOnDotnetJs = false,
        bool isPublish = false,
        RuntimeVariant runtimeType = RuntimeVariant.SingleThreaded)
    {
        EnsureProjectDirIsSet();
        string bootJsonPath = Path.Combine(binFrameworkDir, "blazor.boot.json");
        Assert.True(File.Exists(bootJsonPath), $"Expected to find {bootJsonPath}");

        BootJsonData bootJson = ParseBootData(bootJsonPath);
        var bootJsonEntries = bootJson.resources.runtime.Keys.Where(k => k.StartsWith("dotnet.", StringComparison.Ordinal)).ToArray();

        var expectedEntries = new SortedDictionary<string, Action<string>>();
        IReadOnlySet<string> expected = GetDotNetFilesExpectedSet(runtimeType, isPublish);

        var knownSet = GetAllKnownDotnetFilesToFingerprintMap(runtimeType);
        foreach (string expectedFilename in expected)
        {
            if (Path.GetExtension(expectedFilename) == ".map")
                continue;

            bool expectFingerprint = knownSet[expectedFilename];
            expectedEntries[expectedFilename] = item =>
            {
                string prefix = Path.GetFileNameWithoutExtension(expectedFilename);
                string extension = Path.GetExtension(expectedFilename).Substring(1);

                if (ShouldCheckFingerprint(expectedFilename: expectedFilename,
                                           expectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs,
                                           expectFingerprintForThisFile: expectFingerprint))
                {
                    Assert.Matches($"{prefix}{s_dotnetVersionHashRegex}{extension}", item);
                }
                else
                {
                    Assert.Equal(expectedFilename, item);
                }

                string absolutePath = Path.Combine(binFrameworkDir, item);
                Assert.True(File.Exists(absolutePath), $"Expected to find '{absolutePath}'");
            };
        }
        // FIXME: maybe use custom code so the details can show up in the log
        Assert.Collection(bootJsonEntries.Order(), expectedEntries.Values.ToArray());
    }

    public static BootJsonData ParseBootData(string bootJsonPath)
    {
        using FileStream stream = File.OpenRead(bootJsonPath);
        stream.Position = 0;
        var serializer = new DataContractJsonSerializer(
            typeof(BootJsonData),
            new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

        var config = (BootJsonData?)serializer.ReadObject(stream);
        Assert.NotNull(config);
        return config;
    }

    public string FindBlazorBinFrameworkDir(string config, bool forPublish, string framework)
    {
        EnsureProjectDirIsSet();
        string basePath = Path.Combine(ProjectDir, "bin", config, framework);
        if (forPublish)
            basePath = FindSubDirIgnoringCase(basePath, "publish");

        return Path.Combine(basePath, "wwwroot", "_framework");
    }
}
