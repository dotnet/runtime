// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmTemplateTests : BuildTestBase
    {
        public WasmTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        private void updateProgramCS() {
            string programText = """
            Console.WriteLine("Hello, Console!");

            for (int i = 0; i < args.Length; i ++)
                Console.WriteLine ($"args[{i}] = {args[i]}");
            """;
            var path = Path.Combine(_projectDir!, "Program.cs");
            string text = File.ReadAllText(path);
            text = text.Replace(@"Console.WriteLine(""Hello, Console!"");", programText);
            text = text.Replace("return 0;", "return 42;");
            File.WriteAllText(path, text);
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BrowserBuildThenPublish(string config)
        {
            string id = $"browser_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: true,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: false,
                            TargetFramework: "net7.0"
                        ));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: true);

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");
            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            bool expectRelinking = config == "Release";
            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: !expectRelinking,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: true,
                            TargetFramework: "net7.0",
                            UseCache: false));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking);
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void ConsoleBuildThenPublish(string config)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                        DotnetWasmFromRuntimePack: true,
                        CreateProject: false,
                        HasV8Script: false,
                        MainJS: "main.mjs",
                        Publish: false,
                        TargetFramework: "net7.0"
                        ));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: true);

            (int exitCode, string output) = RunProcess(s_buildEnv.DotNet, _testOutput, args: $"run --no-build -c {config}", workingDir: _projectDir);
            Assert.Equal(0, exitCode);
            Assert.Contains("Hello, Console!", output);

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            bool expectRelinking = config == "Release";
            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: !expectRelinking,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.mjs",
                            Publish: true,
                            TargetFramework: "net7.0",
                            UseCache: false));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug", false)]
        [InlineData("Debug", true)]
        [InlineData("Release", false)]
        [InlineData("Release", true)]
        public void ConsoleBuildAndRun(string config, bool relinking)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            updateProgramCS();
            if (relinking)
                AddItemsPropertiesToProject(projectFile, "<WasmBuildNative>true</WasmBuildNative>");

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: !relinking,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.mjs",
                            Publish: false,
                            TargetFramework: "net7.0"
                            ));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !relinking);

            (int exitCode, string output) = RunProcess(s_buildEnv.DotNet, _testOutput, args: $"run --no-build -c {config} x y z", workingDir: _projectDir);
            Assert.Equal(42, exitCode);
            Assert.Contains("args[0] = x", output);
            Assert.Contains("args[1] = y", output);
            Assert.Contains("args[2] = z", output);
        }

        public static TheoryData<string, bool, bool> TestDataForConsolePublishAndRun()
        {
            var data = new TheoryData<string, bool, bool>();
            data.Add("Debug", false, false);
            data.Add("Debug", false, false);
            data.Add("Debug", false, true);
            data.Add("Release", false, false); // Release relinks by default

            // [ActiveIssue("https://github.com/dotnet/runtime/issues/71887", TestPlatforms.Windows)]
            if (!OperatingSystem.IsWindows())
            {
                data.Add("Debug", true, false);
                data.Add("Release", true, false);
            }

            return data;
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [MemberData(nameof(TestDataForConsolePublishAndRun))]
        public void ConsolePublishAndRun(string config, bool aot, bool relinking)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            updateProgramCS();

            if (aot)
                AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");
            else if (relinking)
                AddItemsPropertiesToProject(projectFile, "<WasmBuildNative>true</WasmBuildNative>");

            var buildArgs = new BuildArgs(projectName, config, aot, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);

            bool expectRelinking = config == "Release" || aot || relinking;
            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: !expectRelinking,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.mjs",
                            Publish: true,
                            TargetFramework: "net7.0",
                            UseCache: false));

            if (!aot)
            {
                // These are disabled for AOT explicitly
                AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking);
            }
            else
            {
                AssertFilesDontExist(Path.Combine(GetBinDir(config), "AppBundle"), new[] { "dotnet.js.symbols" });
            }

            // FIXME: pass envvars via the environment, once that is supported
            string runArgs = $"run --no-build -c {config}";
            if (aot)
                runArgs += $" --setenv=MONO_LOG_MASK=aot --setenv=MONO_LOG_LEVEL=debug";
            runArgs += " x y z";
            var res = new RunCommand(s_buildEnv, _testOutput, label: id)
                                .WithWorkingDirectory(_projectDir!)
                                .ExecuteWithCapturedOutput(runArgs)
                                .EnsureExitCode(42);

            if (aot)
                Assert.Contains($"AOT: image '{Path.GetFileNameWithoutExtension(projectFile)}' found", res.Output);
            Assert.Contains("args[0] = x", res.Output);
            Assert.Contains("args[1] = y", res.Output);
            Assert.Contains("args[2] = z", res.Output);
        }

        [ConditionalFact(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        public async Task BlazorRunTest()
        {
            string config = "Debug";
            string id = $"blazor_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "blazorwasm");
            // string projectName = Path.GetFileNameWithoutExtension(projectFile);

            // var buildArgs = new BuildArgs(projectName, config, false, id, null);
            // buildArgs = ExpandBuildArgs(buildArgs);

            new DotNetCommand(s_buildEnv, _testOutput)
                    .WithWorkingDirectory(_projectDir!)
                    .Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")}")
                    .EnsureSuccessful();

            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!);

            await using var runner = new BrowserRunner();
            var page = await runner.RunAsync(runCommand, $"run -c {config} --no-build");

            await page.Locator("text=Counter").ClickAsync();
            var txt = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.Equal("Current count: 0", txt);

            await page.Locator("text=\"Click me\"").ClickAsync();
            txt = await page.Locator("p[role='status']").InnerHTMLAsync();
            Assert.Equal("Current count: 1", txt);
        }

        [ConditionalFact(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        public async Task BrowserTest()
        {
            string config = "Debug";
            string id = $"browser_{config}_{Path.GetRandomFileName()}";
            CreateWasmTemplateProject(id, "wasmbrowser");

            // var buildArgs = new BuildArgs(projectName, config, false, id, null);
            // buildArgs = ExpandBuildArgs(buildArgs);

            new DotNetCommand(s_buildEnv, _testOutput)
                    .WithWorkingDirectory(_projectDir!)
                    .Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")}")
                    .EnsureSuccessful();

            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!);

            await using var runner = new BrowserRunner();
            var page = await runner.RunAsync(runCommand, $"run -c {config} --no-build -r browser-wasm --forward-console");
            await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
            Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
        }
    }
}
