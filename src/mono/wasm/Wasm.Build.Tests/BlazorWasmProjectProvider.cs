// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using System.Runtime.Serialization.Json;
using Microsoft.NET.Sdk.WebAssembly;

#nullable enable

namespace Wasm.Build.Tests;

public class BlazorWasmProjectProvider(string projectDir, ITestOutputHelper testOutput)
                : WasmSdkBasedProjectProvider(projectDir, testOutput)
{
    public void AssertBlazorBootJson(
        string binFrameworkDir,
        bool expectFingerprintOnDotnetJs = false,
        bool isPublish = false,
        RuntimeVariant runtimeType = RuntimeVariant.SingleThreaded)
    {
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
        string basePath = Path.Combine(ProjectDir, "bin", config, framework);
        if (forPublish)
            basePath = FindSubDirIgnoringCase(basePath, "publish");

        return Path.Combine(basePath, "wwwroot", "_framework");
    }
}
