// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests.Blazor;

public class NativeTests : BlazorWasmTestBase
{
    public NativeTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTInProjectFile(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{GetRandomId()}";
        string projectFile = CreateProjectWithNativeReference(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: "<RunAOTCompilation>true</RunAOTCompilation>" + extraProperties);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true));

        // will relink
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/82725")]
    public void WithNativeReference_AOTOnCommandLine(string config)
    {
        string id = $"blz_nativeref_aot_{config}_{GetRandomId()}";
        string projectFile = CreateProjectWithNativeReference(id);
        string extraProperties = config == "Debug"
                                    ? ("<EmccLinkOptimizationFlag>-O1</EmccLinkOptimizationFlag>" +
                                        "<EmccCompileOptimizationFlag>-O1</EmccCompileOptimizationFlag>")
                                    : string.Empty;
        AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.AOT, ExpectRelinkDirWhenPublishing: true), "-p:RunAOTCompilation=true");

        // no aot!
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));
    }

    [Theory]
    [InlineData("Debug")]
    [InlineData("Release")]
    public void BlazorWasm_CanRunMonoAOTCross_WithNoTrimming(string config)
    {
        string id = $"blazorwasm_{config}_aot_{GetRandomId()}";
        CreateBlazorWasmTemplateProject(id);

        // We don't want to emcc compile, and link ~180 assemblies!
        // So, stop once `mono-aot-cross` part of the build is done
        string target = @"<Target Name=""StopAfterWasmAOT"" AfterTargets=""_WasmAotCompileApp"">
            <Error Text=""Stopping after AOT"" Condition=""'$(WasmBuildingForNestedPublish)' == 'true'"" />
        </Target>
        ";
        AddItemsPropertiesToProject(Path.Combine(_projectDir!, $"{id}.csproj"),
                                    extraItems: null,
                                    extraProperties: null,
                                    atTheEnd: target);

        string publishLogPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}.binlog");
        CommandResult res = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                    .ExecuteWithCapturedOutput("publish",
                                                               $"-bl:{publishLogPath}",
                                                               "-p:RunAOTCompilation=true",
                                                               "-p:PublishTrimmed=false",
                                                               $"-p:Configuration={config}");

        Assert.True(res.ExitCode != 0, "Expected publish to fail");
        Assert.Contains("Stopping after AOT", res.Output);
    }
}
