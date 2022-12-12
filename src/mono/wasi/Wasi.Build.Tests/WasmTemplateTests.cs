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
    public class WasiTemplateTests : BuildTestBase
    {
        public WasiTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void ConsoleBuildThenPublish(string config)
        {
            string id = $"{config}_{Path.GetRandomFileName()}";
            string projectFile = CreateWasmTemplateProject(id, "wasiconsole");
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
                        TargetFramework: BuildTestBase.DefaultTargetFramework
                        ));

#if true
            (int exitCode, string output) = RunProcess(s_buildEnv.DotNet, _testOutput, args: $"run --no-build -c {config}", workingDir: _projectDir);
            Assert.Equal(0, exitCode);
            Assert.Contains("Hello, Console!", output);
#endif

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
                            UseCache: false));
        }

#if false
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
                            TargetFramework: expectedTFM
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
                            UseCache: false));

            if (!aot)
            {
                // These are disabled for AOT explicitly
                AssertDotNetJsSymbols(Path.Combine(GetBinDir(config), "AppBundle"), fromRuntimePack: !expectRelinking, targetFramework: DefaultTargetFramework);
            }
            else
            {
                AssertFilesDontExist(Path.Combine(GetBinDir(config), "AppBundle"), new[] { "dotnet.js.symbols" });
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
#endif
    }
}
