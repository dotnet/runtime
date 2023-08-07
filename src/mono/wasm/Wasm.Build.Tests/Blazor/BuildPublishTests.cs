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

public class BuildPublishTests : BlazorWasmTestBase
{
    public BuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public async Task DefaultTemplate_WithoutWorkload(string config)
    {
        string id = $"blz_no_workload_{config}_{GetRandomId()}_{s_unicodeChar}";
        CreateBlazorWasmTemplateProject(id);

        BlazorBuild(new BlazorBuildOptions(id, config));
        await BlazorRunForBuildWithDotnetRun(new BlazorRunOptions() { Config = config });

        BlazorPublish(new BlazorBuildOptions(id, config));
        await BlazorRunForPublishWithWebServer(new BlazorRunOptions() { Config = config });
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void DefaultTemplate_NoAOT_WithWorkload(string config)
    {
        // disable relinking tests for Unicode: github.com/emscripten-core/emscripten/issues/17817
        // [ActiveIssue("https://github.com/dotnet/runtime/issues/83497")]
        string id = config == "Release" ?
            $"blz_no_aot_{config}_{GetRandomId()}" :
            $"blz_no_aot_{config}_{GetRandomId()}_{s_unicodeChar}";
        CreateBlazorWasmTemplateProject(id);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        if (config == "Release")
        {
            // relinking in publish for Release config
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));
        }
        else
        {
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack, ExpectRelinkDirWhenPublishing: true));
        }
    }

    [Theory]
    [InlineData("Debug", false)]
    [InlineData("Release", false)]
    [InlineData("Debug", true)]
    [InlineData("Release", true)]
    public void DefaultTemplate_CheckFingerprinting(string config, bool expectFingerprintOnDotnetJs)
    {
        string id = $"blz_checkfingerprinting_{config}_{GetRandomId()}";

        CreateBlazorWasmTemplateProject(id);

        var options = new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true, ExpectFingerprintOnDotnetJs: expectFingerprintOnDotnetJs);
        var finterprintingArg = expectFingerprintOnDotnetJs ? "/p:WasmFingerprintDotnetJs=true" : string.Empty;

        BlazorBuild(options, "/p:WasmBuildNative=true", finterprintingArg);
        BlazorPublish(options, "/p:WasmBuildNative=true", finterprintingArg);
    }

    // Disabling for now - publish folder can have more than one dotnet*hash*js, and not sure
    // how to pick which one to check, for the test
    //[Theory]
    //[InlineData("Debug")]
    //[InlineData("Release")]
    //public void DefaultTemplate_AOT_OnlyWithPublishCommandLine_Then_PublishNoAOT(string config)
    //{
    //string id = $"blz_aot_pub_{config}";
    //CreateBlazorWasmTemplateProject(id);

    //// No relinking, no AOT
    //BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack);

    //// AOT=true only for the publish command line, similar to what
    //// would happen when setting it in Publish dialog for VS
    //BlazorPublish(new BlazorBuildOptions(id, config, expectedFileType: NativeFilesType.AOT, "-p:RunAOTCompilation=true");

    //// publish again, no AOT
    //BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked);
    //}
}
