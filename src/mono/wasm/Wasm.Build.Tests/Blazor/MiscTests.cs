// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.NET.Sdk.WebAssembly;
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
    [TestCategory("native")]
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
    [TestCategory("native")]
    public void DefaultTemplate_AOT_InProjectFile(Configuration config)
    {
        string extraProperties = config == Configuration.Debug
                                    ? ("<RunAOTCompilation>true</RunAOTCompilation>" +
                                        "<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : "<RunAOTCompilation>true</RunAOTCompilation>";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "blz_aot_prj_file", extraProperties: extraProperties);

        // build relinks
        BlazorBuild(info, config, isNativeBuild: true);

        // will aot
        BlazorPublish(info, config, new PublishOptions(UseCache: false, AOT: true));

        // build again
        BlazorBuild(info, config, new BuildOptions(UseCache: false), isNativeBuild: true);
    }

    [Fact]
    [TestCategory("native")]
    public void BugRegression_60479_WithRazorClassLib()
    {
        Configuration config = Configuration.Release;
        string razorClassLibraryName = "RazorClassLibrary";
        string extraItems = @$"
            <ProjectReference Include=""..\\RazorClassLibrary\\RazorClassLibrary.csproj"" />
            <BlazorWebAssemblyLazyLoad Include=""{razorClassLibraryName}{ProjectProviderBase.WasmAssemblyExtension}"" />";
        ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.BlazorBasicTestApp, "blz_razor_lib_top", extraItems: extraItems);

        // No relinking, no AOT
        BlazorBuild(info, config, new BuildOptions(UseCache: false));

        // will relink
        BlazorPublish(info, config, new PublishOptions(UseCache: false));

        // publish/wwwroot/_framework/blazor.boot.json
        string bootConfigPath = _provider.GetBootConfigPath(GetBlazorBinFrameworkDir(config, forPublish: true));
        BootJsonData bootJson = _provider.GetBootJson(bootConfigPath);

        Assert.Contains(((AssetsData)bootJson.resources).lazyAssembly, f => f.name.StartsWith(razorClassLibraryName));
    }

    [Theory]
    [InlineData(Configuration.Debug)]
    [InlineData(Configuration.Release)]
    public void MultiClientHostedBuild(Configuration config)
    {
        // Test that two Blazor WASM client projects can be hosted by a single server project
        // without duplicate static web asset Identity collisions. This validates the Framework
        // SourceType materialization path that gives each client unique per-project Identity
        // for shared runtime pack files.
        ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.BlazorBasicTestApp, "multi_hosted");

        // _projectDir is now .../App. Go up to the root and create a second client + server.
        string rootDir = Path.GetDirectoryName(_projectDir)!;
        string client1Dir = _projectDir;
        string client2Dir = Path.Combine(rootDir, "App2");
        string serverDir = Path.Combine(rootDir, "Server");

        // Duplicate App as App2 with a different StaticWebAssetBasePath
        Utils.DirectoryCopy(client1Dir, client2Dir);
        string client2Csproj = Path.Combine(client2Dir, "BlazorBasicTestApp.csproj");
        File.Move(client2Csproj, Path.Combine(client2Dir, "BlazorBasicTestApp2.csproj"));
        client2Csproj = Path.Combine(client2Dir, "BlazorBasicTestApp2.csproj");

        // Set different base paths so the two clients don't collide on routes
        AddItemsPropertiesToProject(Path.Combine(client1Dir, "BlazorBasicTestApp.csproj"),
            extraProperties: "<StaticWebAssetBasePath>client1</StaticWebAssetBasePath>");
        AddItemsPropertiesToProject(client2Csproj,
            extraProperties: "<StaticWebAssetBasePath>client2</StaticWebAssetBasePath><RootNamespace>BlazorBasicTestApp</RootNamespace>");

        // Create a minimal server project that references both clients
        Directory.CreateDirectory(serverDir);
        string serverCsproj = Path.Combine(serverDir, "Server.csproj");
        File.WriteAllText(serverCsproj, $"""
            <Project Sdk="Microsoft.NET.Sdk.Web">
              <PropertyGroup>
                <TargetFramework>{DefaultTargetFrameworkForBlazor}</TargetFramework>
                <Nullable>enable</Nullable>
                <ImplicitUsings>enable</ImplicitUsings>
              </PropertyGroup>
              <ItemGroup>
                <ProjectReference Include="..\App\BlazorBasicTestApp.csproj" />
                <ProjectReference Include="..\App2\BlazorBasicTestApp2.csproj" />
              </ItemGroup>
            </Project>
            """);
        File.WriteAllText(Path.Combine(serverDir, "Program.cs"), """
            var builder = WebApplication.CreateBuilder(args);
            var app = builder.Build();
            app.UseStaticFiles();
            app.Run();
            """);

        // Build the server project — this will transitively build both clients.
        // Without Framework materialization, this would fail with duplicate Identity
        // for shared runtime pack files (dotnet.native.js, ICU data, etc.)
        string logPath = Path.Combine(s_buildEnv.LogRootPath, info.ProjectName, $"{info.ProjectName}-multi-hosted.binlog");
        using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(serverDir);
        CommandResult result = cmd
            .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
            .ExecuteWithCapturedOutput("build", $"-p:Configuration={config}", $"-bl:{logPath}")
            .EnsureSuccessful();

        // Verify both clients produced framework files in their own bin directories
        string client1Framework = Path.Combine(client1Dir, "bin", config.ToString(), DefaultTargetFrameworkForBlazor, "wwwroot", "_framework");
        string client2Framework = Path.Combine(client2Dir, "bin", config.ToString(), DefaultTargetFrameworkForBlazor, "wwwroot", "_framework");

        Assert.True(Directory.Exists(client1Framework), $"Client1 framework dir missing: {client1Framework}");
        Assert.True(Directory.Exists(client2Framework), $"Client2 framework dir missing: {client2Framework}");

        // Both should have dotnet.js (verifies framework files were materialized per-client)
        var client1Files = Directory.GetFiles(client1Framework);
        var client2Files = Directory.GetFiles(client2Framework);
        Assert.Contains(client1Files, f => Path.GetFileName(f).StartsWith("dotnet.") && f.EndsWith(".js"));
        Assert.Contains(client2Files, f => Path.GetFileName(f).StartsWith("dotnet.") && f.EndsWith(".js"));
    }
}
