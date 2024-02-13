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
        AddItemsPropertiesToProject(
            projectFile,
            extraProperties:
                $"<BlazorIcuDataFileName>{fileName}</BlazorIcuDataFileName>");

        (CommandResult res, string logPath) = BlazorBuild(buildOptions);
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug", "incorrectName.dat", false)]
    [InlineData("Release", "incorrectName.dat", false)]
    [InlineData("Debug", "icudtNonExisting.dat", true)]
    [InlineData("Release", "icudtNonExisting.dat", true)]
    public void NonExistingCustomFileAssertError(string config, string fileName, bool isFilenameCorrect)
    {
        string id = $"blz_invalidCustomIcu_{config}_{GetRandomId()}";
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
            if (isFilenameCorrect)
            {
                Assert.Contains($"Could not find $(BlazorIcuDataFileName)={fileName}, or when used as a path relative to the runtime pack", ex.Message);
            }
            else
            {
                Assert.Contains("File name in $(BlazorIcuDataFileName) has to start with 'icudt'", ex.Message);
            }
        }
        catch (Exception)
        {
            throw new Exception("Unexpected exception in test scenario.");
        }
        // we expect build error, so there is not point in running the app
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
