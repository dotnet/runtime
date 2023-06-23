// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable enable

using System;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.Playwright;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Wasm.Build.Tests;

public abstract class BlazorWasmTestBase : WasmTemplateTestBase
{
    protected BlazorWasmTestBase(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
                : base(output, buildContext)
    {
    }

    public void InitBlazorWasmProjectDir(string id, string targetFramework = DefaultTargetFrameworkForBlazor)
    {
        InitPaths(id);
        if (Directory.Exists(_projectDir))
            Directory.Delete(_projectDir, recursive: true);
        Directory.CreateDirectory(_projectDir);

        File.WriteAllText(Path.Combine(_projectDir, "nuget.config"),
                            GetNuGetConfigWithLocalPackagesPath(
                                        GetNuGetConfigPathFor(targetFramework),
                                        s_buildEnv.BuiltNuGetsPath));

        File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.props"), Path.Combine(_projectDir, "Directory.Build.props"));
        File.Copy(Path.Combine(BuildEnvironment.TestDataPath, "Blazor.Directory.Build.targets"), Path.Combine(_projectDir, "Directory.Build.targets"));
    }

    public string CreateBlazorWasmTemplateProject(string id)
    {
        InitBlazorWasmProjectDir(id);
        new DotNetCommand(s_buildEnv, _testOutput, useDefaultArgs: false)
                .WithWorkingDirectory(_projectDir!)
                .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                .ExecuteWithCapturedOutput("new blazorwasm")
                .EnsureSuccessful();

        string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
        if (!UseWebcil)
            AddItemsPropertiesToProject(projectFile, "<WasmEnableWebcil>false</WasmEnableWebcil>");
        return projectFile;
    }

    protected (CommandResult, string) BlazorBuild(BlazorBuildOptions options, params string[] extraArgs)
    {
        if (options.WarnAsError)
            extraArgs = extraArgs.Append("/warnaserror").ToArray();

        var res = BlazorBuildInternal(options.Id, options.Config, publish: false, setWasmDevel: false, extraArgs);
        _testOutput.WriteLine($"BlazorBuild, options.tfm: {options.TargetFramework}");
        AssertDotNetNativeFiles(options.ExpectedFileType, options.Config, forPublish: false, targetFramework: options.TargetFramework);
        AssertBlazorBundle(options.Config,
                           isPublish: false,
                           dotnetWasmFromRuntimePack: options.ExpectedFileType == NativeFilesType.FromRuntimePack,
                           targetFramework: options.TargetFramework,
                           expectFingerprintOnDotnetJs: options.ExpectFingerprintOnDotnetJs);

        return res;
    }

    protected (CommandResult, string) BlazorPublish(BlazorBuildOptions options, params string[] extraArgs)
    {
        var res = BlazorBuildInternal(options.Id, options.Config, publish: true, setWasmDevel: false, extraArgs);
        AssertDotNetNativeFiles(options.ExpectedFileType, options.Config, forPublish: true, targetFramework: options.TargetFramework);
        AssertBlazorBundle(options.Config,
                           isPublish: true,
                           dotnetWasmFromRuntimePack: options.ExpectedFileType == NativeFilesType.FromRuntimePack,
                           targetFramework: options.TargetFramework,
                           expectFingerprintOnDotnetJs: options.ExpectFingerprintOnDotnetJs);

        if (options.ExpectedFileType == NativeFilesType.AOT)
        {
            // check for this too, so we know the format is correct for the negative
            // test for jsinterop.webassembly.dll
            Assert.Contains("Microsoft.JSInterop.dll -> Microsoft.JSInterop.dll.bc", res.Item1.Output);

            // make sure this assembly gets skipped
            Assert.DoesNotContain("Microsoft.JSInterop.WebAssembly.dll -> Microsoft.JSInterop.WebAssembly.dll.bc", res.Item1.Output);

        }

        string objBuildDir = Path.Combine(_projectDir!, "obj", options.Config, options.TargetFramework, "wasm", "for-build");
        // Check that we linked only for publish
        if (options.ExpectRelinkDirWhenPublishing)
            Assert.True(Directory.Exists(objBuildDir), $"Could not find expected {objBuildDir}, which gets created when relinking during Build. This is likely a test authoring error");
        else
            Assert.False(Directory.Exists(objBuildDir), $"Found unexpected {objBuildDir}, which gets created when relinking during Build");

        return res;
    }

    protected (CommandResult, string) BlazorBuildInternal(string id, string config, bool publish = false, bool setWasmDevel = true, params string[] extraArgs)
    {
        string label = publish ? "publish" : "build";
        _testOutput.WriteLine($"{Environment.NewLine}** {label} **{Environment.NewLine}");

        string logPath = Path.Combine(s_buildEnv.LogRootPath, id, $"{id}-{label}.binlog");
        string[] combinedArgs = new[]
        {
            label, // same as the command name
            $"-bl:{logPath}",
            $"-p:Configuration={config}",
            !UseWebcil ? "-p:WasmEnableWebcil=false" : string.Empty,
            "-p:BlazorEnableCompression=false",
            "-nr:false",
            setWasmDevel ? "-p:_WasmDevel=true" : string.Empty
        }.Concat(extraArgs).ToArray();

        CommandResult res = new DotNetCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(_projectDir!)
                                    .WithEnvironmentVariable("NUGET_PACKAGES", _nugetPackagesDir)
                                    .ExecuteWithCapturedOutput(combinedArgs)
                                    .EnsureSuccessful();

        return (res, logPath);
    }

    protected void AssertDotNetNativeFiles(NativeFilesType type, string config, bool forPublish, string targetFramework)
    {
        string label = forPublish ? "publish" : "build";
        string objBuildDir = Path.Combine(_projectDir!, "obj", config, targetFramework, "wasm", forPublish ? "for-publish" : "for-build");
        string binFrameworkDir = FindBlazorBinFrameworkDir(config, forPublish, framework: targetFramework);

        string srcDir = type switch
        {
            NativeFilesType.FromRuntimePack => s_buildEnv.GetRuntimeNativeDir(targetFramework),
            NativeFilesType.Relinked => objBuildDir,
            NativeFilesType.AOT => objBuildDir,
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        AssertSameFile(Path.Combine(srcDir, "dotnet.native.wasm"), Path.Combine(binFrameworkDir, "dotnet.native.wasm"), label);

        // find dotnet*js
        string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir)
                                .Where(p => Path.GetFileName(p).StartsWith("dotnet.native", StringComparison.OrdinalIgnoreCase) &&
                                                Path.GetFileName(p).EndsWith(".js", StringComparison.OrdinalIgnoreCase))
                                .SingleOrDefault();

        Assert.True(!string.IsNullOrEmpty(dotnetJsPath), $"[{label}] Expected to find dotnet.native*js in {binFrameworkDir}");
        AssertSameFile(Path.Combine(srcDir, "dotnet.native.js"), dotnetJsPath!, label);

        if (type != NativeFilesType.FromRuntimePack)
        {
            // check that the files are *not* from runtime pack
            AssertNotSameFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.wasm"), Path.Combine(binFrameworkDir, "dotnet.native.wasm"), label);
            AssertNotSameFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js"), dotnetJsPath!, label);
        }
    }

    protected void AssertBlazorBundle(string config, bool isPublish, bool dotnetWasmFromRuntimePack, string targetFramework = DefaultTargetFrameworkForBlazor, string? binFrameworkDir = null, bool expectFingerprintOnDotnetJs = false)
    {
        binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish, targetFramework);

        AssertBlazorBootJson(config, isPublish, targetFramework != DefaultTargetFrameworkForBlazor, targetFramework, binFrameworkDir: binFrameworkDir);
        AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.wasm"),
                   Path.Combine(binFrameworkDir, "dotnet.native.wasm"),
                   "Expected dotnet.native.wasm to be same as the runtime pack",
                   same: dotnetWasmFromRuntimePack);

        string? dotnetJsPath = Directory.EnumerateFiles(binFrameworkDir, "dotnet.native.*.js").FirstOrDefault();
        Assert.True(dotnetJsPath != null, $"Could not find blazor's dotnet*js in {binFrameworkDir}");

        AssertFile(Path.Combine(s_buildEnv.GetRuntimeNativeDir(targetFramework), "dotnet.native.js"),
                    dotnetJsPath!,
                    "Expected dotnet.native.js to be same as the runtime pack",
                    same: dotnetWasmFromRuntimePack);

        string bootConfigPath = Path.Combine(binFrameworkDir, "blazor.boot.json");
        Assert.True(File.Exists(bootConfigPath), $"Expected to find '{bootConfigPath}'");

        using (var bootConfigContent = File.OpenRead(bootConfigPath))
        {
            var bootConfig = ParseBootData(bootConfigContent);
            var dotnetJsEntries = bootConfig.resources.runtime.Keys.Where(k => k.StartsWith("dotnet.") && k.EndsWith(".js")).ToArray();

            void AssertFileExists(string fileName)
            {
                string absolutePath = Path.Combine(binFrameworkDir, fileName);
                Assert.True(File.Exists(absolutePath), $"Expected to find '{absolutePath}'");
            }

            string versionHashRegex = @"\.(?<version>.+)\.(?<hash>[a-zA-Z0-9]+)\.";

            Assert.Collection(
                dotnetJsEntries.OrderBy(f => f),
                item =>
                {
                    if (expectFingerprintOnDotnetJs)
                        Assert.Matches($"dotnet{versionHashRegex}js", item);
                    else
                        Assert.Equal("dotnet.js", item);

                    AssertFileExists(item);
                },
                item => { Assert.Matches($"dotnet\\.native{versionHashRegex}js", item); AssertFileExists(item); },
                item => { Assert.Matches($"dotnet\\.runtime{versionHashRegex}js", item); AssertFileExists(item); }
            );
        }
    }

    protected void AssertBlazorBootJson(string config, bool isPublish, bool isNet7AndBelow, string targetFramework = DefaultTargetFrameworkForBlazor, string? binFrameworkDir = null)
    {
        binFrameworkDir ??= FindBlazorBinFrameworkDir(config, isPublish, targetFramework);

        string bootJsonPath = Path.Combine(binFrameworkDir, "blazor.boot.json");
        Assert.True(File.Exists(bootJsonPath), $"Expected to find {bootJsonPath}");

        string bootJson = File.ReadAllText(bootJsonPath);
        var bootJsonNode = JsonNode.Parse(bootJson);
        var runtimeObj = bootJsonNode?["resources"]?["runtime"]?.AsObject();
        Assert.NotNull(runtimeObj);

        string msgPrefix = $"[{(isPublish ? "publish" : "build")}]";
        Assert.True(runtimeObj!.Where(kvp => kvp.Key == (isNet7AndBelow ? "dotnet.wasm" : "dotnet.native.wasm")).Any(), $"{msgPrefix} Could not find dotnet.native.wasm entry in blazor.boot.json");
        Assert.True(runtimeObj!.Where(kvp => kvp.Key.StartsWith("dotnet.", StringComparison.OrdinalIgnoreCase) &&
                                                kvp.Key.EndsWith(".js", StringComparison.OrdinalIgnoreCase)).Any(),
                                        $"{msgPrefix} Could not find dotnet.*js in {bootJson}");
    }

    protected string FindBlazorBinFrameworkDir(string config, bool forPublish, string framework = DefaultTargetFrameworkForBlazor)
    {
        string basePath = Path.Combine(_projectDir!, "bin", config, framework);
        if (forPublish)
            basePath = FindSubDirIgnoringCase(basePath, "publish");

        return Path.Combine(basePath, "wwwroot", "_framework");
    }

    protected string CreateProjectWithNativeReference(string id)
    {
        CreateBlazorWasmTemplateProject(id);

        string extraItems = @$"
            {GetSkiaSharpReferenceItems()}
            <WasmFilesToIncludeInFileSystem Include=""{Path.Combine(BuildEnvironment.TestAssetsPath, "mono.png")}"" />
        ";
        string projectFile = Path.Combine(_projectDir!, $"{id}.csproj");
        AddItemsPropertiesToProject(projectFile, extraItems: extraItems);

        return projectFile;
    }

    // Keeping these methods with explicit Build/Publish in the name
    // so in the test code it is evident which is being run!
    public Task BlazorRunForBuildWithDotnetRun(string config, Func<IPage, Task>? test = null, string extraArgs = "--no-build", Action<IConsoleMessage>? onConsoleMessage = null)
        => BlazorRunTest($"run -c {config} {extraArgs}", _projectDir!, test, onConsoleMessage);

    public Task BlazorRunForPublishWithWebServer(string config, Func<IPage, Task>? test = null, string extraArgs = "", Action<IConsoleMessage>? onConsoleMessage = null)
        => BlazorRunTest($"{s_xharnessRunnerCommand} wasm webserver --app=. --web-server-use-default-files {extraArgs}",
                         Path.GetFullPath(Path.Combine(FindBlazorBinFrameworkDir(config, forPublish: true), "..")),
                         test, onConsoleMessage);

    public async Task BlazorRunTest(string runArgs,
                                    string workingDirectory,
                                    Func<IPage, Task>? test = null,
                                    Action<IConsoleMessage>? onConsoleMessage = null,
                                    bool detectRuntimeFailures = true)
    {
        using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                    .WithWorkingDirectory(workingDirectory);

        await using var runner = new BrowserRunner(_testOutput);
        var page = await runner.RunAsync(runCommand, runArgs, onConsoleMessage: OnConsoleMessage);

        await page.Locator("text=Counter").ClickAsync();
        var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 0", txt);

        await page.Locator("text=\"Click me\"").ClickAsync();
        txt = await page.Locator("p[role='status']").InnerHTMLAsync();
        Assert.Equal("Current count: 1", txt);

        if (test is not null)
            await test(page);

        void OnConsoleMessage(IConsoleMessage msg)
        {
            if (EnvironmentVariables.ShowBuildOutput)
                Console.WriteLine($"[{msg.Type}] {msg.Text}");
            _testOutput.WriteLine($"[{msg.Type}] {msg.Text}");

            onConsoleMessage?.Invoke(msg);

            if (detectRuntimeFailures)
            {
                if (msg.Text.Contains("[MONO] * Assertion") || msg.Text.Contains("Error: [MONO] "))
                    throw new XunitException($"Detected a runtime failure at line: {msg.Text}");
            }
        }
    }

}
