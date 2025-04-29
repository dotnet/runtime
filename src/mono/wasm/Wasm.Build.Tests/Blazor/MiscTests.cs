// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class MiscTests : BlazorWasmTestBase
{
    public MiscTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData(Configuration.Debug, true)]
    [InlineData(Configuration.Debug, false)]
    [InlineData(Configuration.Release, true)]
    [InlineData(Configuration.Release, false)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/103566")]
    public void NativeBuild_WithDeployOnBuild_UsedByVS(Configuration config, bool nativeRelink)
    {
        string extraProperties = config == Configuration.Debug
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        if (!nativeRelink)
            extraProperties += "<RunAOTCompilation>true</RunAOTCompilation>";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "blz_deploy_on_build", extraProperties: extraProperties);

        // build with -p:DeployOnBuild=true, and that will trigger a publish
        (string _, string buildOutput) = BlazorBuild(info,
            config,
            new BuildOptions(ExtraMSBuildArgs: "-p:DeployBuild=true -p:CompressionEnabled=false"),
            isNativeBuild: true);

        // double check relinking!
        string substring = "pinvoke.c -> pinvoke.o";
        Assert.Contains(substring, buildOutput);

        // there should be only one instance of this string!
        int occurrences = buildOutput.Split(new[] { substring }, StringSplitOptions.None).Length - 1;
        Assert.Equal(2, occurrences);
    }

    [Theory]
    [InlineData(Configuration.Release)]
    public void DefaultTemplate_AOT_InProjectFile(Configuration config)
    {
        string extraProperties = config == Configuration.Debug
                                    ? ("<RunAOTCompilation>true</RunAOTCompilation>" +
                                        "<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : "<RunAOTCompilation>true</RunAOTCompilation>";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "blz_aot_prj_file", extraProperties: extraProperties);

        // No relinking, no AOT
        BlazorBuild(info, config);

        // will aot
        BlazorPublish(info, config, new PublishOptions(UseCache: false, AOT: true));

        // build again
        BlazorBuild(info, config, new BuildOptions(UseCache: false));
    }

    [Fact]
    public void BugRegression_60479_WithRazorClassLib()
    {
        Configuration config = Configuration.Release;
        string razorClassLibraryName = "RazorClassLibrary";
        string extraItems = @$"
            <ProjectReference Include=""..\\RazorClassLibrary\\RazorClassLibrary.csproj"" />
            <BlazorWebAssemblyLazyLoad Include=""{razorClassLibraryName}{ProjectProviderBase.WasmAssemblyExtension}"" />";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "blz_razor_lib_top", extraItems: extraItems);

        // No relinking, no AOT
        BlazorBuild(info, config);

        // will relink
        BlazorPublish(info, config, new PublishOptions(UseCache: false, BootConfigFileName: "blazor.boot.json"));

        // publish/wwwroot/_framework/blazor.boot.json
        string frameworkDir = GetBlazorBinFrameworkDir(config, forPublish: true);
        string bootJson = Path.Combine(frameworkDir, "blazor.boot.json");

        Assert.True(File.Exists(bootJson), $"Could not find {bootJson}");
        var jdoc = JsonDocument.Parse(File.ReadAllText(bootJson));
        if (!jdoc.RootElement.TryGetProperty("resources", out JsonElement resValue) ||
            !resValue.TryGetProperty("lazyAssembly", out JsonElement lazyVal))
        {
            throw new XunitException($"Could not find resources.lazyAssembly object in {bootJson}");
        }

        Assert.True(lazyVal.EnumerateObject().Select(jp => jp.Name).FirstOrDefault(f => f.StartsWith(razorClassLibraryName)) != null);
    }
}
