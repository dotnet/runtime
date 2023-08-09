// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;
using System.Threading.Tasks;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

// these tests only check if correct ICU files got copied
public class IcuShardingTests : BlazorWasmTestBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) {}

    [Theory]
    [InlineData("Debug", "icudt.dat")]
    [InlineData("Release", "icudt.dat")]
    [InlineData("Debug", "icudt_EFIGS.dat")]
    [InlineData("Release", "icudt_EFIGS.dat")]
    [InlineData("Debug", "icudt_no_CJK.dat")]
    [InlineData("Release", "icudt_no_CJK.dat")]
    [InlineData("Debug", "icudt_CJK.dat")]
    [InlineData("Release", "icudt_CJK.dat")]
    public async Task CustomIcuFileFromRuntimePack(string config, string fileName)
    {
        string id = $"blz_customFromRuntimePack_{config}_{GetRandomId()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        var buildOptions = new BlazorBuildOptions(
                id,
                config,
                WarnAsError: true,
                GlobalizationMode: GlobalizationMode.PredefinedIcu,
                PredefinedIcudt: fileName
            );
        string icuFilePath = GetAbsolutePathToIcuFromRuntimePack(fileName, buildOptions);
        AddItemsPropertiesToProject(
            projectFile,
            extraProperties: 
                $"<BlazorIcuDataFileName>{icuFilePath}</BlazorIcuDataFileName>");

        (CommandResult res, string logPath) = BlazorBuild(buildOptions);
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    private string GetAbsolutePathToIcuFromRuntimePack(string icuFileName, BlazorBuildOptions options)
    {
        string runtimePackPath = ProjectProviderBase.GetExpectedRuntimePackDir(options.TargetFramework ?? DefaultTargetFramework);
        return Path.Combine(runtimePackPath, "runtimes", "browser-wasm", "native", icuFileName);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task NonExistingCustomFileAssertError(string config)
    {
        string id = $"blz_invalidCustomIcu_{config}_{GetRandomId()}";
        string fileName = "nonexisting.dat";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        AddItemsPropertiesToProject(
            projectFile,
            extraProperties: 
                $"<BlazorIcuDataFileName>{fileName}</BlazorIcuDataFileName>");

        try
        {
            (CommandResult res, string logPath) = BlazorBuild(
                new BlazorBuildOptions(
                    id,
                    config,
                    WarnAsError: false,
                    GlobalizationMode: GlobalizationMode.PredefinedIcu,
                    PredefinedIcudt: fileName
                ));
        }
        catch (XunitException ex)
        {
            Assert.Contains("File name in $(BlazorIcuDataFileName) has to start with 'icudt'", ex.Message);
        }
        catch (Exception)
        {
            throw new Exception("Unexpected exception in test scenario.");
        }
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task CustomFileNotFromRuntimePackAbsolutePath(string config)
    {
        string id = $"blz_invalidCustomIcu_{config}_{GetRandomId()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        AddItemsPropertiesToProject(
            projectFile,
            extraProperties: 
                $"<BlazorIcuDataFileName>{IcuTestsBase.CustomIcuPath}</BlazorIcuDataFileName>");

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: GlobalizationMode.PredefinedIcu,
                PredefinedIcudt: IcuTestsBase.CustomIcuPath
            ));
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }
}