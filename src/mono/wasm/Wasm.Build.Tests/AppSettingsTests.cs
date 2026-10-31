// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests;

public class AppSettingsTests : WasmTemplateTestsBase
{
    public AppSettingsTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    [Theory, TestCategory("coreclr"), TestCategory("no-workload")]
    [InlineData(false, false, false, false, null)]
    [InlineData(true, false, false, false, null)]
    [InlineData(false, false, false, true, "default")]
    [InlineData(true, false, false, true, "default")]
    [InlineData(false, false, true, true, null)]
    [InlineData(true, false, true, true, null)]
    [InlineData(false, true, false, true, null)]
    [InlineData(true, true, false, true, null)]
    [InlineData(false, false, false, true, "legacy")]
    [InlineData(false, false, false, false, "memory")]
    [InlineData(false, false, false, false, "cache")]
    [InlineData(false, false, false, false, "null")]
    [InlineData(false, false, false, false, "response")]
    [InlineData(false, false, false, false, "url")]
    [InlineData(false, false, false, false, "empty-file")]
    [InlineData(false, false, false, false, "no-settings")]
    public async Task AppSettingsWithoutFilesystem(bool publish, bool includeVfsFiles, bool forceVfs, bool expectedVfs, string? mode)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "AppSettingsMemoryTest");
        if (!includeVfsFiles)
            AddItemsPropertiesToProject(info.ProjectFilePath, extraItems: """<WasmFilesToIncludeInFileSystem Remove="@(WasmFilesToIncludeInFileSystem)" />""");
        if (mode == "empty-file")
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(info.ProjectFilePath)!, "wwwroot", "appsettings.Empty.json"), Array.Empty<byte>());
        if (forceVfs && !includeVfsFiles)
        {
            if (publish)
                PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: "-p:_WasmFilesystemUsageDetected=false -p:PublishTrimmed=false"));
            else
                BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: "-p:_WasmFilesystemUsageDetected=false"));
        }
        string args = mode == "default" ? string.Empty : $"-p:_WasmFilesystemUsageDetected={forceVfs.ToString().ToLowerInvariant()}";
        if (publish && forceVfs && !includeVfsFiles)
            args += " -p:PublishTrimmed=false";
        if (publish)
            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: args, UseCache: false));
        else
            BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: args, UseCache: false));

        BrowserRunOptions options = new(config, TestScenario: "AppSettingsTest",
            BrowserQueryString: new NameValueCollection {
                { "settingsVfs", expectedVfs.ToString().ToLowerInvariant() },
                { "legacySettings", (mode == "legacy").ToString().ToLowerInvariant() },
                { "settingsDownload", mode is "memory" or "cache" ? mode : null },
                { "settingsLoader", mode is "null" or "response" or "url" ? mode : null },
                { "applicationEnvironment", mode == "empty-file" ? "Empty" : null },
                { "noSettings", mode == "no-settings" ? "true" : null }
            });
        RunResult result = publish
            ? await RunForPublishWithWebServer(options)
            : await RunForBuildWithDotnetRun(options);
        Assert.Contains(result.TestOutput, line => line.Contains($"'/appsettings.json' exists '{expectedVfs}'"));
        Assert.Contains(result.TestOutput, line => line.Contains("Appsettings byte API passed"));
        Assert.Contains(result.TestOutput, line => line.Contains("Appsettings file API passed"));
    }

    public static IEnumerable<object?[]> LoadAppSettingsBasedOnApplicationEnvironmentData()
    {
        // Defaults
        if (!EnvironmentVariables.UseJavascriptBundler)
            yield return new object?[] { false, null, null, "Development" };

        yield return new object?[] { true, null, null, "Production" };

        // Override defaults from MSBuild
        if (!EnvironmentVariables.UseJavascriptBundler)
            yield return new object?[] { false, "Production", null, "Production" };

        yield return new object?[] { true, "Development", null, "Development" };

        // Override defaults from JavaScript
        if (!EnvironmentVariables.UseJavascriptBundler)
            yield return new object?[] { false, null, "Production", "Production" };

        yield return new object?[] { true, null, "Development", "Development" };

        // Override MSBuild from JavaScript
        if (!EnvironmentVariables.UseJavascriptBundler)
            yield return new object?[] { false, "FromMSBuild", "Production", "Production" };

        yield return new object?[] { true, "FromMSBuild", "Development", "Development" };
        yield return new object?[] { true, null, "Staging", "Staging" };
    }

    [Theory, TestCategory("bundler-friendly")]
    [MemberData(nameof(LoadAppSettingsBasedOnApplicationEnvironmentData))]
    public async Task LoadAppSettingsBasedOnApplicationEnvironment(bool publish, string? msBuildApplicationEnvironment, string? queryApplicationEnvironment, string expectedApplicationEnvironment)
    {
        Configuration config = Configuration.Debug;
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.WasmBasicTestApp, "AppSettingsTest");
        string extraMSBuildArgs = msBuildApplicationEnvironment != null ? $"-p:WasmApplicationEnvironmentName={msBuildApplicationEnvironment}" : string.Empty;

        if (publish)
            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: extraMSBuildArgs));
        else
            BuildProject(info, config, new BuildOptions(ExtraMSBuildArgs: extraMSBuildArgs));
        
        BrowserRunOptions runOptions = new(
            config,
            TestScenario: "AppSettingsTest",
            BrowserQueryString: new NameValueCollection { { "applicationEnvironment", queryApplicationEnvironment } }
        );
        RunResult result = publish
            ? await RunForPublishWithWebServer(runOptions)
            : await RunForBuildWithDotnetRun(runOptions);

        const string browserVirtualAppBase = "/"; // keep in sync other places that define browserVirtualAppBase
        Assert.Contains(result.TestOutput, m => m.Contains($"'{browserVirtualAppBase}appsettings.json' exists 'True'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'{browserVirtualAppBase}appsettings.Development.json' exists '{expectedApplicationEnvironment == "Development"}'"));
        Assert.Contains(result.TestOutput, m => m.Contains($"'{browserVirtualAppBase}appsettings.Production.json' exists '{expectedApplicationEnvironment == "Production"}'"));
        string expectedFiles = expectedApplicationEnvironment is "Development" or "Production"
            ? $"appsettings.json,appsettings.{expectedApplicationEnvironment}.json"
            : "appsettings.json";
        Assert.Contains(result.TestOutput, m => m.Contains($"Appsettings files: {expectedFiles}"));
        Assert.Contains(result.TestOutput, m => m.Contains("Appsettings byte API passed"));
        Assert.Contains(result.TestOutput, m => m.Contains("Appsettings file API passed"));
    }
}
