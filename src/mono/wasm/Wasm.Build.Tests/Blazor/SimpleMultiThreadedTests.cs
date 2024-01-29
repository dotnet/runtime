// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Wasm.Build.Tests.Blazor;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests.MT.Blazor;

public class SimpleMultiThreadedTests : BlazorWasmTestBase
{
    public SimpleMultiThreadedTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
    }

    // dotnet-run needed for running with *build* so wwwroot has the index.html etc
    // [Theory]
    // [InlineData("Debug")]
    // [InlineData("Release")]
    // public async Task BlazorBuildRunTest(string config)
    // {
    //     string id = $"blazor_mt_{config}_{GetRandomId()}";
    //     string projectFile = CreateWasmTemplateProject(id, "blazorwasm");

    //     AddItemsPropertiesToProject(projectFile, "<WasmEnableThreads>true</WasmEnableThreads>");
    //     BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack, RuntimeType: RuntimeType.MultiThreaded));
    //     // await BlazorRunForBuildWithDotnetRun(config);

    //     await BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files --web-server-use-cors --web-server-use-cop --web-server-use-https --timeout=15:00:00",
    //                          Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(config, forPublish: false), "..")));
    // }

    [ConditionalTheory(typeof(BuildTestBase), nameof(IsWorkloadWithMultiThreadingForDefaultFramework))]
    [InlineData("Debug", false)]
    // [InlineData("Debug", true)]
    [InlineData("Release", false)]
    // [InlineData("Release", true)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task BlazorPublishRunTest(string config, bool aot)
    {
        string id = $"blazor_mt_{config}_{GetRandomId()}";
        string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
        AddItemsPropertiesToProject(projectFile, "<WasmEnableThreads>true</WasmEnableThreads>");
        // if (aot)
            // AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");

        BlazorPublish(new BlazorBuildOptions(
            id,
            config,
            aot ? NativeFilesType.AOT
                : (config == "Release" ? NativeFilesType.Relinked : NativeFilesType.FromRuntimePack),
            RuntimeType: RuntimeVariant.MultiThreaded));

        StringBuilder errorOutput = new();
        await BlazorRunForPublishWithWebServer(
                runOptions: new BlazorRunOptions(
                    Config: config,
                    ExtraArgs: "--web-server-use-cors --web-server-use-cop",
                    OnConsoleMessage: (message) =>
                    {
                        if (message.Type == "error")
                            errorOutput.AppendLine(message.Text);
                    },
                    OnErrorMessage: (message) =>
                    {
                        errorOutput.AppendLine(message);
                    }));

        if (errorOutput.Length > 0)
            throw new XunitException($"Errors found in browser console output:\n{errorOutput}");
    }
}
