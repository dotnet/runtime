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

public class MiscTests3 : BlazorWasmTestBase
{
    public MiscTests3(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
        : base(output, buildContext)
    {
        _enablePerTestCleanup = true;
    }

    [Theory]
    [InlineData("Debug", /*build*/true, /*publish*/false)]
    [InlineData("Debug", /*build*/false, /*publish*/true)]
    [InlineData("Debug", /*build*/true, /*publish*/true)]
    [InlineData("Release", /*build*/true, /*publish*/false)]
    [InlineData("Release", /*build*/false, /*publish*/true)]
    [InlineData("Release", /*build*/true, /*publish*/true)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/87877", TestPlatforms.Windows)]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/97054")]
    public async Task WithDllImportInMainAssembly(string config, bool build, bool publish)
    {
        // Based on https://github.com/dotnet/runtime/issues/59255
        string id = $"blz_dllimp_{config}_{s_unicodeChar}";
        if (build && publish)
            id += "build_then_publish";
        else if (build)
            id += "build";
        else
            id += "publish";

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
        BlazorAddRazorButton("cpp_add", """
            var result = MyDllImports.cpp_add(10, 12);
            outputText = $"{result}";
        """);

        if (build)
            BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.Relinked));

        if (publish)
            BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: build));

        BlazorRunOptions runOptions = new() { Config = config, Test = TestDllImport };
        if (publish)
            await BlazorRunForPublishWithWebServer(runOptions);
        else
            await BlazorRunForBuildWithDotnetRun(runOptions);

        async Task TestDllImport(IPage page)
        {
            await page.Locator("text=\"cpp_add\"").ClickAsync();
            var txt = await page.Locator("p[role='test']").InnerHTMLAsync();
            Assert.Equal("Output: 22", txt);
        }
    }

    [Fact]
    public void BugRegression_60479_WithRazorClassLib()
    {
        string id = $"blz_razor_lib_top_{GetRandomId()}";
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

        string razorClassLibraryFileName = $"RazorClassLibrary{ProjectProviderBase.WasmAssemblyExtension}";
        AddItemsPropertiesToProject(wasmProjectFile, extraItems: @$"
            <ProjectReference Include=""..\\RazorClassLibrary\\RazorClassLibrary.csproj"" />
            <BlazorWebAssemblyLazyLoad Include=""{razorClassLibraryFileName}"" />
        ");

        _projectDir = wasmProjectDir;
        string config = "Release";
        // No relinking, no AOT
        BlazorBuild(new BlazorBuildOptions(id, config, NativeFilesType.FromRuntimePack));

        // will relink
        BlazorPublish(new BlazorBuildOptions(id, config, NativeFilesType.Relinked, ExpectRelinkDirWhenPublishing: true));

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

        Assert.Contains(razorClassLibraryFileName, lazyVal.EnumerateObject().Select(jp => jp.Name));
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
