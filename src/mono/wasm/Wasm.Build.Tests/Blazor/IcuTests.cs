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
public class IcuTests : BlazorWasmTestBase
{
    public IcuTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext) {}

    string getRandomNameWithoutDots => Path.GetRandomFileName().Replace(".", "");

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    public void HybridWithInvariant(string config, bool invariant)
    {
        string id = $"blz_hybrid_{config}_{getRandomNameWithoutDots}";
        string projectFile = CreateProjectWithNativeReference(id);
        AddItemsPropertiesToProject(
            projectFile,
            extraProperties: 
                $"<HybridGlobalization>true</HybridGlobalization><InvariantGlobalization>{invariant}</InvariantGlobalization>");

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.Hybrid,
                ExpectedFileType: NativeFilesType.Relinked
            ));
        if (invariant)
            Assert.Contains("$(HybridGlobalization) has no effect when $(InvariantGlobalization) is set to true.", res.Output);
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    public void HybridWithFullIcuFromRuntimePack(string config, bool fullIcu)
    {
        string id = $"blz_hybrid_{config}_{getRandomNameWithoutDots}";
        string projectFile = CreateProjectWithNativeReference(id);
        AddItemsPropertiesToProject(projectFile, extraProperties: 
             $"<HybridGlobalization>true</HybridGlobalization><BlazorWebAssemblyLoadAllGlobalizationData>{fullIcu}</BlazorWebAssemblyLoadAllGlobalizationData>");

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: GlobalizationMode.Hybrid,
                ExpectedFileType: NativeFilesType.Relinked
            ));
        if (fullIcu)
             Assert.Contains("$(BlazorWebAssemblyLoadAllGlobalizationData) has no effect when $(HybridGlobalization) is set to true.", res.Output);
    }

    
    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    public void FullIcuFromRuntimePackWithInvariant(string config, bool invariant)
    {
        string id = $"blz_hybrid_{config}_{getRandomNameWithoutDots}";
        string projectFile = CreateProjectWithNativeReference(id);
        AddItemsPropertiesToProject(projectFile, extraProperties: 
            $"<BlazorWebAssemblyLoadAllGlobalizationData>true</BlazorWebAssemblyLoadAllGlobalizationData><InvariantGlobalization>{invariant}</InvariantGlobalization>");

        (CommandResult res, string logPath) = BlazorBuild(
            new BlazorBuildOptions(
                id,
                config,
                WarnAsError: false,
                GlobalizationMode: invariant ? GlobalizationMode.Invariant : GlobalizationMode.FullIcu,
                ExpectedFileType: NativeFilesType.Relinked
            ));
        
        if (invariant)
             Assert.Contains("$(BlazorWebAssemblyLoadAllGlobalizationData) has no effect when $(InvariantGlobalization) is set to true.", res.Output);
    }
}