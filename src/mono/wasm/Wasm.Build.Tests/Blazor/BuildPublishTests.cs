// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class BuildPublishTests : BuildTestBase
{
    public BuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory, TestCategory("no-workload")]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void DefaultTemplate_WithoutWorkload(string config)
    {
        string id = $"blz_no_workload_{config}_{Path.GetRandomFileName()}";
        CreateBlazorWasmTemplateProject(id);

        // Build
        BuildInternal(id, config, publish: false);
        AssertBlazorBootJson(config, isPublish: false);

        // Publish
        BuildInternal(id, config, publish: true);
        AssertBlazorBootJson(config, isPublish: true);
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void DefaultTemplate_NoAOT_WithWorkload(string config)
    {
        string id = $"blz_no_aot_{config}_{Path.GetRandomFileName()}";
        CreateBlazorWasmTemplateProject(id);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        if (config == "Release")
        {
            // relinking in publish for Release config
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
        }
        else
        {
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));
        }
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

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void WithDllImportInMainAssembly(string config)
    {
        // Based on https://github.com/dotnet/runtime/issues/59255
        string id = $"blz_dllimp_{config}";
        string projectFile = CreateProjectWithNativeReference(id);
        string nativeSource = @"
            #include <stdio.h>

            extern ""C"" {
                int cpp_add(int a, int b) {
                    return a + b;
                }
            }";

        File.WriteAllText(Path.Combine(_projectDir!, "mylib.cpp"), nativeSource);

        string myDllImportCs = @$"
            using System.Runtime.InteropServices;
            namespace {id};

            public static class MyDllImports
            {{
                [DllImport(""mylib"")]
                public static extern int cpp_add(int a, int b);
            }}";

        File.WriteAllText(Path.Combine(_projectDir!, "Pages", "MyDllImport.cs"), myDllImportCs);

        AddItemsPropertiesToProject(projectFile, extraItems: @"<NativeFileReference Include=""mylib.cpp"" />");

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
        CheckNativeFileLinked(forPublish: false);

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
        CheckNativeFileLinked(forPublish: true);

        void CheckNativeFileLinked(bool forPublish)
        {
            // very crude way to check that the native file was linked in
            // needed because we don't run the blazor app yet
            string objBuildDir = Path.Combine(_projectDir!, "obj", config, DefaultTargetFramework, "wasm", forPublish ? "for-publish" : "for-build");
            string pinvokeTableHPath = Path.Combine(objBuildDir, "pinvoke-table.h");
            Assert.True(File.Exists(pinvokeTableHPath), $"Could not find {pinvokeTableHPath}");

            string pinvokeTableHContents = File.ReadAllText(pinvokeTableHPath);
            string pattern = $"\"cpp_add\".*{id}";
            Assert.True(Regex.IsMatch(pinvokeTableHContents, pattern),
                            $"Could not find {pattern} in {pinvokeTableHPath}");
        }
    }

    [Fact]
    public void BugRegression_60479_WithRazorClassLib()
    {
        string id = $"blz_razor_lib_top_{Path.GetRandomFileName()}";
        InitBlazorWasmProjectDir(id);

        string wasmProjectDir = Path.Combine(_projectDir!, "wasm");
        string wasmProjectFile = Path.Combine(wasmProjectDir, "wasm.csproj");
        Directory.CreateDirectory(wasmProjectDir);
        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(wasmProjectDir)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutput("new blazorwasm")
                .EnsureSuccessful();


        string razorProjectDir = Path.Combine(_projectDir!, "RazorClassLibrary");
        Directory.CreateDirectory(razorProjectDir);
        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(razorProjectDir)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutput("new razorclasslib")
                .EnsureSuccessful();

        AddItemsPropertiesToProject(wasmProjectFile, extraItems:@"
            <ProjectReference Include=""..\RazorClassLibrary\RazorClassLibrary.csproj"" />
            <BlazorWebAssemblyLazyLoad Include=""RazorClassLibrary.dll"" />
        ");

        _projectDir = wasmProjectDir;
        string config = "Release";
        // No relinking, no AOT
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));

        // will relink
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        // publish/wwwroot/_framework/blazor.boot.json
        string frameworkDir = FindBlazorBinFrameworkDir(config, forPublish: true);
        string bootJson = Path.Combine(frameworkDir, "blazor.boot.json");

        Assert.True(File.Exists(bootJson), $"Could not find {bootJson}");
        var jdoc = JsonDocument.Parse(File.ReadAllText(bootJson));
        if (!jdoc.RootElement.TryGetProperty("resources", out JsonElement resValue) ||
            !resValue.TryGetProperty("lazyAssembly", out JsonElement lazyVal))
        {
            throw new XunitException($"Could not find resources.lazyAssembly object in {bootJson}");
        }

        Assert.Contains("RazorClassLibrary.dll", lazyVal.EnumerateObject().Select(jp => jp.Name));
    }
}
