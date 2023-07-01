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
using System.Text.Json.Nodes;

#nullable enable

namespace Wasm.Build.Tests;

public class BlazorWasmProjectProvider(string projectDir, ITestOutputHelper _testOutput)
                : ProjectProviderBase(projectDir, _testOutput)
{
    public void AssertBlazorBootJson(string config, bool isPublish, bool isNet7AndBelow, string targetFramework, string? binFrameworkDir)
    {
        binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish, targetFramework);

        string bootJsonPath = Path.Combine(binFrameworkDir, "blazor.boot.json");
        Assert.True(File.Exists(bootJsonPath), $"Expected to find {bootJsonPath}");

        string bootJson = File.ReadAllText(bootJsonPath);
        var bootJsonNode = JsonNode.Parse(bootJson);
        var runtimeObj = bootJsonNode?["resources"]?["runtime"]?.AsObject();
        Assert.NotNull(runtimeObj);

        string msgPrefix = $"[{(isPublish ? "publish" : "build")}]";
        Assert.True(runtimeObj!.Where(kvp => kvp.Key == (isNet7AndBelow ? "dotnet.wasm" : "dotnet.native.wasm")).Any(), $"{msgPrefix} Could not find dotnet.native.wasm entry in blazor.boot.json");
        Assert.True(runtimeObj!.Where(kvp => kvp.Key.StartsWith("dotnet.", StringComparison.OrdinalIgnoreCase) &&
                                                kvp.Key.EndsWith(".js", StringComparison.OrdinalIgnoreCase)).Any(),
                                        $"{msgPrefix} Could not find dotnet.*js in {bootJson}");
    }

    public static BootJsonData ParseBootData(Stream stream)
    {
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
