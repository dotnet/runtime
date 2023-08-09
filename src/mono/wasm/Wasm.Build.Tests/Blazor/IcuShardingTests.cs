// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

// these tests only check if correct ICU files got copied
public class IcuShardingTests : BlazorWasmTestBase
{
    public IcuShardingTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) {}

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void NonExistingCustomFileAssertError(string config)
    {
        string id = $"blz_invalidCustomIcu_{config}_{GetRandomId()}";
        string fileName = "nonexisting.dat";
        string projectFile = CreateProjectWithNativeReference(id);
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
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void CustomFileNotFromRuntimePackAbsolutePath(string config)
    {
        string id = $"blz_invalidCustomIcu_{config}_{GetRandomId()}";
        string projectFile = CreateProjectWithNativeReference(id);
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
                PredefinedIcudt: IcuTestsBase.CustomIcuPath,
                ExpectedFileType: NativeFilesType.Relinked
            ));
    }
}