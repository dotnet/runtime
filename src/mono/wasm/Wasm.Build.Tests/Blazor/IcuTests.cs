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
public class IcuTests : BlazorWasmTestBase
{
    public IcuTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) {}

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Debug", null)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    [InlineData("Release", null)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task HybridWithInvariant(string config, bool? invariant)
    {
        string id = $"blz_hybrid_{config}_{GetRandomId()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        string extraProperties = "<HybridGlobalization>true</HybridGlobalization>";
        if (invariant != null)
            extraProperties += $"<InvariantGlobalization>{invariant}</InvariantGlobalization>";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: invariant == true ? GlobalizationMode.Invariant : GlobalizationMode.Hybrid,
                ExpectedFileType: invariant == true ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack
            ));

        string warning = "$(HybridGlobalization) has no effect when $(InvariantGlobalization) is set to true.";
        if (invariant == true)
        {
            Assert.Contains(warning, res.Output);
        }
        else
        {
            Assert.DoesNotContain(warning, res.Output);
        }

        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Debug", null)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    [InlineData("Release", null)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task HybridWithFullIcuFromRuntimePack(string config, bool? fullIcu)
    {
        string id = $"blz_hybrid_{config}_{GetRandomId()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        string extraProperties = "<HybridGlobalization>true</HybridGlobalization>";
        if (fullIcu != null)
            extraProperties += $"<BlazorWebAssemblyLoadAllGlobalizationData>{fullIcu}</BlazorWebAssemblyLoadAllGlobalizationData>";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: GlobalizationMode.Hybrid
            ));

        string warning = "$(BlazorWebAssemblyLoadAllGlobalizationData) has no effect when $(HybridGlobalization) is set to true.";
        if (fullIcu == true)
        {
             Assert.Contains(warning, res.Output);
        }
        else
        {
            Assert.DoesNotContain(warning, res.Output);
        }

        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Debug", null)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    [InlineData("Release", null)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task FullIcuFromRuntimePackWithInvariant(string config, bool? invariant)
    {
        string id = $"blz_hybrid_{config}_{GetRandomId()}";
        string projectFile = CreateBlazorWasmTemplateProject(id);
        string extraProperties = "<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData>";
        if (invariant != null)
            extraProperties += $"<InvariantGlobalization>{invariant}</InvariantGlobalization>";
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: invariant == true ? GlobalizationMode.Invariant : GlobalizationMode.FullIcu,
                ExpectedFileType: invariant == true ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack
            ));

        string warning = "$(BlazorWebAssemblyLoadAllGlobalizationData) has no effect when $(InvariantGlobalization) is set to true.";
        if (invariant == true)
        {
             Assert.Contains(warning, res.Output);
        }
        else
        {
             Assert.DoesNotContain(warning, res.Output);
        }

        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }
}
