// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        private void UpdateProgramCS()
        {
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

        private void UpdateBrowserMainJs(string targetFramework)
        {
            string mainJsPath = Path.Combine(_projectDir!, "main.js");
            string mainJsContent = File.ReadAllText(mainJsPath);

            // .withExitOnUnhandledError() is available only only >net7.0
            mainJsContent = mainJsContent.Replace(".create()",
                    targetFramework == "net8.0"
                        ? ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().create()"
                        : ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().create()");
            File.WriteAllText(mainJsPath, mainJsContent);
        }

        private void UpdateConsoleMainJs()
        {
            string mainJsPath = Path.Combine(_projectDir!, "main.mjs");
            string mainJsContent = File.ReadAllText(mainJsPath);

            mainJsContent = mainJsContent
                .Replace(".create()", ".withConsoleForwarding().create()");

            File.WriteAllText(mainJsPath, mainJsContent);
        }

        private void UpdateMainJsEnvironmentVariables(params (string key, string value)[] variables)
        {
            string mainJsPath = Path.Combine(_projectDir!, "main.mjs");
            string mainJsContent = File.ReadAllText(mainJsPath);

            StringBuilder js = new();
            foreach (var variable in variables)
            {
                js.Append($".withEnvironmentVariable(\"{variable.key}\", \"{variable.value}\")");
            }

            mainJsContent = mainJsContent
                .Replace(".create()", js.ToString() + ".create()");

            File.WriteAllText(mainJsPath, mainJsContent);
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BrowserBuildThenPublish(string config)
        {
            string id = $"browser_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            UpdateBrowserMainJs(DefaultTargetFramework);

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
                            TargetFramework: BuildTestBase.DefaultTargetFramework
                        ));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: true, targetFramework: DefaultTargetFramework);

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
                            MainJS: "main.js",
                            Publish: true,
                            TargetFramework: BuildTestBase.DefaultTargetFramework,
                            UseCache: false));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking, targetFramework: DefaultTargetFramework);
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void ConsoleBuildThenPublish(string config)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            UpdateConsoleMainJs();

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
                        TargetFramework: BuildTestBase.DefaultTargetFramework,
                        IsBrowserProject: false
                        ));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: true, targetFramework: DefaultTargetFramework);

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
                            TargetFramework: BuildTestBase.DefaultTargetFramework,
                            UseCache: false,
                            IsBrowserProject: false));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking, targetFramework: DefaultTargetFramework);
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("Debug", false)]
        [InlineData("Debug", true)]
        [InlineData("Release", false)]
        [InlineData("Release", true)]
        public void ConsoleBuildAndRunDefault(string config, bool relinking)
            => ConsoleBuildAndRun(config, relinking, string.Empty, DefaultTargetFramework);

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        // [ActiveIssue("https://github.com/dotnet/runtime/issues/79313")]
        // [InlineData("Debug", "-f net7.0", "net7.0")]
        [InlineData("Debug", "-f net8.0", "net8.0")]
        public void ConsoleBuildAndRunForSpecificTFM(string config, string extraNewArgs, string expectedTFM)
            => ConsoleBuildAndRun(config, false, extraNewArgs, expectedTFM);

        private void ConsoleBuildAndRun(string config, bool relinking, string extraNewArgs, string expectedTFM)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole", extraNewArgs);
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            UpdateProgramCS();
            UpdateConsoleMainJs();
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
                            TargetFramework: expectedTFM,
                            IsBrowserProject: false
                            ));

            AssertDotNetJsSymbols(Path.Combine(GetBinDir(config, expectedTFM), "AppBundle"), fromRuntimePack: !relinking, targetFramework: expectedTFM);

            (int exitCode, string output) = RunProcess(s_buildEnv.DotNet, _testOutput, args: $"run --no-build -c {config} x y z", workingDir: _projectDir);
            Assert.Equal(42, exitCode);

            Assert.Contains("args[0] = x", output);
            Assert.Contains("args[1] = y", output);
            Assert.Contains("args[2] = z", output);
        }

        public static TheoryData<bool, bool, string> TestDataForAppBundleDir()
        {
            var data = new TheoryData<bool, bool, string>();
            AddTestData(forConsole: true, runOutsideProjectDirectory: false);
            AddTestData(forConsole: true, runOutsideProjectDirectory: true);

            AddTestData(forConsole: false, runOutsideProjectDirectory: false);
            AddTestData(forConsole: false, runOutsideProjectDirectory: true);

            void AddTestData(bool forConsole, bool runOutsideProjectDirectory)
            {
                // FIXME: Disabled for `main` right now, till 7.0 gets the fix
                data.Add(runOutsideProjectDirectory, forConsole, string.Empty);

                data.Add(runOutsideProjectDirectory, forConsole,
                                $"<OutputPath>{Path.Combine(BuildEnvironment.TmpPath, Path.GetRandomFileName())}</OutputPath>");
                data.Add(runOutsideProjectDirectory, forConsole,
                                $"<WasmAppDir>{Path.Combine(BuildEnvironment.TmpPath, Path.GetRandomFileName())}</WasmAppDir>");
            }

            return data;
        }

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [MemberData(nameof(TestDataForAppBundleDir))]
        public async Task RunWithDifferentAppBundleLocations(bool forConsole, bool runOutsideProjectDirectory, string extraProperties)
            => await (forConsole
                    ? ConsoleRunWithAndThenWithoutBuildAsync("Release", extraProperties, runOutsideProjectDirectory)
                    : BrowserRunTwiceWithAndThenWithoutBuildAsync("Release", extraProperties, runOutsideProjectDirectory));

        private async Task BrowserRunTwiceWithAndThenWithoutBuildAsync(string config, string extraProperties = "", bool runOutsideProjectDirectory = false)
        {
            string id = $"browser_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");

            UpdateBrowserMainJs(DefaultTargetFramework);

            if (!string.IsNullOrEmpty(extraProperties))
                AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

            string workingDir = runOutsideProjectDirectory ? BuildEnvironment.TmpPath : _projectDir!;

            {
                using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(workingDir);

                await using var runner = new BrowserRunner(_testOutput);
                var page = await runner.RunAsync(runCommand, $"run -c {config} --project {projectFile} --forward-console");
                await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
                Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
            }

            {
                using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(workingDir);

                await using var runner = new BrowserRunner(_testOutput);
                var page = await runner.RunAsync(runCommand, $"run -c {config} --no-build --project {projectFile} --forward-console");
                await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
                Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
            }
        }

        private Task ConsoleRunWithAndThenWithoutBuildAsync(string config, string extraProperties = "", bool runOutsideProjectDirectory = false)
        {
            string id = $"console_{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole");

            UpdateProgramCS();
            UpdateConsoleMainJs();

            if (!string.IsNullOrEmpty(extraProperties))
                AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

            string workingDir = runOutsideProjectDirectory ? BuildEnvironment.TmpPath : _projectDir!;

            {
                string runArgs = $"run -c {config} --project {projectFile}";
                runArgs += " x y z";
                using var cmd = new RunCommand(s_buildEnv, _testOutput, label: id)
                                    .WithWorkingDirectory(workingDir)
                                    .WithEnvironmentVariables(s_buildEnv.EnvVars);
                var res = cmd.ExecuteWithCapturedOutput(runArgs).EnsureExitCode(42);

                Assert.Contains("args[0] = x", res.Output);
                Assert.Contains("args[1] = y", res.Output);
                Assert.Contains("args[2] = z", res.Output);
            }

            _testOutput.WriteLine($"{Environment.NewLine}[{id}] Running again with --no-build{Environment.NewLine}");

            {
                // Run with --no-build
                string runArgs = $"run -c {config} --project {projectFile} --no-build";
                runArgs += " x y z";
                using var cmd = new RunCommand(s_buildEnv, _testOutput, label: id)
                                .WithWorkingDirectory(workingDir);
                var res = cmd.ExecuteWithCapturedOutput(runArgs).EnsureExitCode(42);

                Assert.Contains("args[0] = x", res.Output);
                Assert.Contains("args[1] = y", res.Output);
                Assert.Contains("args[2] = z", res.Output);
            }

            return Task.CompletedTask;
        }

        public static TheoryData<string, bool, bool> TestDataForConsolePublishAndRun()
        {
            var data = new TheoryData<string, bool, bool>();
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

            UpdateProgramCS();
            UpdateConsoleMainJs();

            if (aot)
            {
                // FIXME: pass envvars via the environment, once that is supported
                UpdateMainJsEnvironmentVariables(("MONO_LOG_MASK", "aot"), ("MONO_LOG_LEVEL", "debug"));
                AddItemsPropertiesToProject(projectFile, "<RunAOTCompilation>true</RunAOTCompilation>");
            }
            else if (relinking)
            {
                AddItemsPropertiesToProject(projectFile, "<WasmBuildNative>true</WasmBuildNative>");
            }

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
                            TargetFramework: BuildTestBase.DefaultTargetFramework,
                            UseCache: false,
                            IsBrowserProject: false));

            if (!aot)
            {
                // These are disabled for AOT explicitly
                AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking, targetFramework: DefaultTargetFramework);
            }
            else
            {
                AssertFilesDontExist(Path.Combine(GetBinDir(config), "AppBundle"), new[] { "dotnet.native.js.symbols" });
            }

            string runArgs = $"run --no-build -c {config}";
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

        [ConditionalTheory(typeof(BuildTestBase), nameof(IsUsingWorkloads))]
        [InlineData("", BuildTestBase.DefaultTargetFramework)]
        // [ActiveIssue("https://github.com/dotnet/runtime/issues/79313")]
        // [InlineData("-f net7.0", "net7.0")]
        [InlineData("-f net8.0", "net8.0")]
        public async Task BrowserBuildAndRun(string extraNewArgs, string targetFramework)
        {
            string config = "Debug";
            string id = $"browser_{config}_{Path.GetRandomFileName()}";
            CreateWasmTemplateProject(id, "wasmbrowser", extraNewArgs);

            UpdateBrowserMainJs(targetFramework);

            new DotNetCommand(s_buildEnv, _testOutput)
                    .WithWorkingDirectory(_projectDir!)
                    .Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")}")
                    .EnsureSuccessful();

            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!);

            await using var runner = new BrowserRunner(_testOutput);
            var page = await runner.RunAsync(runCommand, $"run -c {config} --no-build -r browser-wasm --forward-console");
            await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
            Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
        }
    }
}
