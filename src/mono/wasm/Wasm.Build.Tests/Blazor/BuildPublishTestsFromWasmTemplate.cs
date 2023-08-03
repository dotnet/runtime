// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using Microsoft.Playwright;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class BuildPublishTestsFromWasmTemplate : BlazorWasmTestBase
{
    public BuildPublishTestsFromWasmTemplate(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task BlazorBuildRunTest(string config)
    {
        string id = $"blazor_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });
    }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
    [InlineData("Debug", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", false)]
    [InlineData("Release", true)]
    public async Task BlazorPublishRunTest(string config, bool aot)
    {
        string id = $"blazor_{config}_{Path.GetRandomFileName()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
        if (aot)
            AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");

        BlazorPublish(new BlazorBuildOptions(
            id,
            config,
            aot ? NativeFilesType.AOT
                : (config == "Release" ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack)));
        await BlazorRunForPublishWithWebServer(new BlazorRunOptions() { Config = config });
    }

    private void BlazorAddRazorButton(string buttonText, string customCode, string methodName = "test", string razorPage = "Pages/Counter.razor")
    {
        string additionalCode = $$"""
            <p role="{{methodName}}">Output: @outputText</p>
            <button class="btn btn-primary" @onclick="{{methodName}}">{{buttonText}}</button>

            @code {
                private string outputText = string.Empty;
                public void {{methodName}}()
                {
                    {{customCode}}
                }
            }
        """;

        // find blazor's Counter.razor
        string counterRazorPath = Path.Combine(_projectDir!, razorPage);
        if (!File.Exists(counterRazorPath))
            throw new FileNotFoundException($"Could not find {counterRazorPath}");

        string oldContent = File.ReadAllText(counterRazorPath);
        File.WriteAllText(counterRazorPath, oldContent + additionalCode);
    }
}
