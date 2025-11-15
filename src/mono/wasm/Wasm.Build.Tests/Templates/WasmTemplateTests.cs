// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class WasmTemplateTests : BlazorWasmTestBase
    {
        public WasmTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        private string StringReplaceWithAssert(string oldContent, string oldValue, string newValue)
        {
            string newContent = oldContent.Replace(oldValue, newValue);
            if (oldValue != newValue && oldContent == newContent)
                throw new XunitException($"Replacing '{oldValue}' with '{newValue}' did not change the content '{oldContent}'");

            return newContent;
        }

        private void UpdateBrowserProgramCs()
        {
            var path = Path.Combine(_projectDir!, "Program.cs");
            string text = File.ReadAllText(path);
            text = StringReplaceWithAssert(text, "while(true)", $"int i = 0;{Environment.NewLine}while(i++ < 10)");
            text = StringReplaceWithAssert(text, "partial class StopwatchSample", $"return 42;{Environment.NewLine}partial class StopwatchSample");
            File.WriteAllText(path, text);
        }

        private void UpdateConsoleProgramCs()
        {
            string programText = """
            Console.WriteLine("Hello, Console!");

            for (int i = 0; i < args.Length; i ++)
                Console.WriteLine ($"args[{i}] = {args[i]}");
            """;
            var path = Path.Combine(_projectDir!, "Program.cs");
            string text = File.ReadAllText(path);
            text = StringReplaceWithAssert(text, @"Console.WriteLine(""Hello, Console!"");", programText);
            text = StringReplaceWithAssert(text, "return 0;", "return 42;");
            File.WriteAllText(path, text);
        }

        private void UpdateBrowserMainJs(string targetFramework, string runtimeAssetsRelativePath = DefaultRuntimeAssetsRelativePath)
        {
            base.UpdateBrowserMainJs(
                (mainJsContent) =>
                {
                    // .withExitOnUnhandledError() is available only only >net7.0
                    mainJsContent = StringReplaceWithAssert(
                        mainJsContent,
                        ".create()",
                        (targetFramework == "net8.0" || targetFramework == "net9.0")
                            ? ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().withExitOnUnhandledError().create()"
                            : ".withConsoleForwarding().withElementOnExit().withExitCodeLogging().create()"
                    );

                    // dotnet.run() is already used in <= net8.0
                    if (targetFramework != "net8.0")
                        mainJsContent = StringReplaceWithAssert(mainJsContent, "runMain()", "dotnet.run()");

                    mainJsContent = StringReplaceWithAssert(mainJsContent, "from './_framework/dotnet.js'", $"from '{runtimeAssetsRelativePath}dotnet.js'");

                    return mainJsContent;
                },
                targetFramework,
                runtimeAssetsRelativePath
            );
        }

        private void UpdateConsoleMainJs()
        {
            string mainJsPath = Path.Combine(_projectDir!, "main.mjs");
            string mainJsContent = File.ReadAllText(mainJsPath);

            mainJsContent = StringReplaceWithAssert(mainJsContent, ".create()", ".withConsoleForwarding().create()");

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

            mainJsContent = StringReplaceWithAssert(mainJsContent, ".create()", js.ToString() + ".create()");

            File.WriteAllText(mainJsPath, mainJsContent);
        }

        [Theory, TestCategory("no-fingerprinting")]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BrowserBuildThenPublish(string config)
        {
            string id = $"browser_{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            UpdateBrowserProgramCs();
            UpdateBrowserMainJs(DefaultTargetFramework);

            var buildArgs = new BuildArgs(projectName, config, false, id, null);

            AddItemsPropertiesToProject(projectFile,
                atTheEnd:
                    """
                    <Target Name="CheckLinkedFiles" AfterTargets="ILLink">
                        <ItemGroup>
                            <_LinkedOutFile Include="$(IntermediateOutputPath)\linked\*.dll" />
                        </ItemGroup>
                        <Error Text="No file was linked-out. Trimming probably doesn't work (PublishTrimmed=$(PublishTrimmed))" Condition="@(_LinkedOutFile->Count()) == 0" />
                    </Target>
                    """
            );

            buildArgs = ExpandBuildArgs(buildArgs);

            BuildTemplateProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: true,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: false,
                            TargetFramework: BuildTestBase.DefaultTargetFramework
                        ));

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            bool expectRelinking = config == "Release";
            BuildTemplateProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: !expectRelinking,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: true,
                            TargetFramework: BuildTestBase.DefaultTargetFramework,
                            UseCache: false));
        }

        public static TheoryData<bool, bool, string> TestDataForAppBundleDir()
        {
            var data = new TheoryData<bool, bool, string>();
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

        [Theory, TestCategory("no-fingerprinting")]
        [MemberData(nameof(TestDataForAppBundleDir))]
        public async Task RunWithDifferentAppBundleLocations(bool forConsole, bool runOutsideProjectDirectory, string extraProperties)
            => await (forConsole
                    ? ConsoleRunWithAndThenWithoutBuildAsync("Release", extraProperties, runOutsideProjectDirectory)
                    : BrowserRunTwiceWithAndThenWithoutBuildAsync("Release", extraProperties, runOutsideProjectDirectory));

        private async Task BrowserRunTwiceWithAndThenWithoutBuildAsync(string config, string extraProperties = "", bool runOutsideProjectDirectory = false)
        {
            string id = $"browser_{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");

            UpdateBrowserProgramCs();
            UpdateBrowserMainJs(DefaultTargetFramework);

            if (!string.IsNullOrEmpty(extraProperties))
                AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

            string workingDir = runOutsideProjectDirectory ? BuildEnvironment.TmpPath : _projectDir!;

            {
                using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(workingDir);

                await using var runner = new BrowserRunner(_testOutput);
                var page = await runner.RunAsync(runCommand, $"run --no-silent -c {config} --project \"{projectFile}\" --forward-console");
                await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
                Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
            }

            {
                using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(workingDir);

                await using var runner = new BrowserRunner(_testOutput);
                var page = await runner.RunAsync(runCommand, $"run --no-silent -c {config} --no-build --project \"{projectFile}\" --forward-console");
                await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
                Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
            }
        }

        private Task ConsoleRunWithAndThenWithoutBuildAsync(string config, string extraProperties = "", bool runOutsideProjectDirectory = false)
        {
            string id = $"console_{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmconsole");

            UpdateConsoleProgramCs();
            UpdateConsoleMainJs();

            if (!string.IsNullOrEmpty(extraProperties))
                AddItemsPropertiesToProject(projectFile, extraProperties: extraProperties);

            string workingDir = runOutsideProjectDirectory ? BuildEnvironment.TmpPath : _projectDir!;

            {
                string runArgs = $"run --no-silent -c {config} --project \"{projectFile}\"";
                runArgs += " x y z";
                using var cmd = new RunCommand(s_buildEnv, _testOutput, label: id)
                                    .WithWorkingDirectory(workingDir)
                                    .WithEnvironmentVariables(s_buildEnv.EnvVars);
                CommandResult res = cmd.ExecuteWithCapturedOutput(runArgs).EnsureExitCode(42);

                Assert.Contains("args[0] = x", res.Output);
                Assert.Contains("args[1] = y", res.Output);
                Assert.Contains("args[2] = z", res.Output);
            }

            _testOutput.WriteLine($"{Environment.NewLine}[{id}] Running again with --no-build{Environment.NewLine}");

            {
                // Run with --no-build
                string runArgs = $"run --no-silent -c {config} --project \"{projectFile}\" --no-build";
                runArgs += " x y z";
                using var cmd = new RunCommand(s_buildEnv, _testOutput, label: id)
                                .WithWorkingDirectory(workingDir);
                CommandResult res = cmd.ExecuteWithCapturedOutput(runArgs).EnsureExitCode(42);

                Assert.Contains("args[0] = x", res.Output);
                Assert.Contains("args[1] = y", res.Output);
                Assert.Contains("args[2] = z", res.Output);
            }

            return Task.CompletedTask;
        }

        public static IEnumerable<object?[]> BrowserBuildAndRunTestData()
        {
            yield return new object?[] { "", BuildTestBase.DefaultTargetFramework, DefaultRuntimeAssetsRelativePath };
            yield return new object?[] { "-f net9.0", "net9.0", DefaultRuntimeAssetsRelativePath };

            if (EnvironmentVariables.WorkloadsTestPreviousVersions)
                yield return new object?[] { "-f net8.0", "net8.0", DefaultRuntimeAssetsRelativePath };

            // ActiveIssue("https://github.com/dotnet/runtime/issues/90979")
            // yield return new object?[] { "", BuildTestBase.DefaultTargetFramework, "./" };
            // yield return new object?[] { "-f net8.0", "net8.0", "./" };
        }

        [Theory]
        [MemberData(nameof(BrowserBuildAndRunTestData))]
        public async Task BrowserBuildAndRun(string extraNewArgs, string targetFramework, string runtimeAssetsRelativePath)
        {
            string config = "Debug";
            string id = $"browser_{config}_{GetRandomId()}";
            CreateWasmTemplateProject(id, "wasmbrowser", extraNewArgs, addFrameworkArg: extraNewArgs.Length == 0);

            if (targetFramework != "net8.0")
                UpdateBrowserProgramCs();

            UpdateBrowserMainJs(targetFramework, runtimeAssetsRelativePath);

            using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!);
            cmd.Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")} {(runtimeAssetsRelativePath != DefaultRuntimeAssetsRelativePath ? "-p:WasmRuntimeAssetsLocation=" + runtimeAssetsRelativePath : "")}")
                .EnsureSuccessful();

            using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!);

            await using var runner = new BrowserRunner(_testOutput);
            var page = await runner.RunAsync(runCommand, $"run --no-silent -c {config} --no-build -r browser-wasm --forward-console");
            await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
            Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
        }

        internal static void TestWasmStripILAfterAOTOutput(string objBuildDir, string frameworkDir, bool expectILStripping, ITestOutputHelper testOutput)
        {
            string origAssemblyDir = Path.Combine(objBuildDir, "aot-in");
            string strippedAssemblyDir = Path.Combine(objBuildDir, "stripped");
            Assert.True(Directory.Exists(origAssemblyDir), $"Could not find the original AOT input assemblies dir: {origAssemblyDir}");
            if (expectILStripping)
                Assert.True(Directory.Exists(strippedAssemblyDir), $"Could not find the stripped assemblies dir: {strippedAssemblyDir}");
            else
                Assert.False(Directory.Exists(strippedAssemblyDir), $"Expected {strippedAssemblyDir} to not exist");

            string assemblyToExamine = "System.Private.CoreLib.dll";
            string assemblyToExamineWithoutExtension = Path.GetFileNameWithoutExtension(assemblyToExamine);
            string originalAssembly = Path.Combine(objBuildDir, origAssemblyDir, assemblyToExamine);
            string strippedAssembly = Path.Combine(objBuildDir, strippedAssemblyDir, assemblyToExamine);
            string? bundledAssembly = Directory.EnumerateFiles(frameworkDir, $"*{ProjectProviderBase.WasmAssemblyExtension}").FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).StartsWith(assemblyToExamineWithoutExtension));
            Assert.True(File.Exists(originalAssembly), $"Expected {nameof(originalAssembly)} {originalAssembly} to exist");
            Assert.True(bundledAssembly != null && File.Exists(bundledAssembly), $"Expected {nameof(bundledAssembly)} {bundledAssembly} to exist");
            if (expectILStripping)
                Assert.True(File.Exists(strippedAssembly), $"Expected {nameof(strippedAssembly)} {strippedAssembly} to exist");
            else
                Assert.False(File.Exists(strippedAssembly), $"Expected {strippedAssembly} to not exist");

            string compressedOriginalAssembly = Utils.GZipCompress(originalAssembly);
            string compressedBundledAssembly = Utils.GZipCompress(bundledAssembly);
            FileInfo compressedOriginalAssembly_fi = new FileInfo(compressedOriginalAssembly);
            FileInfo compressedBundledAssembly_fi = new FileInfo(compressedBundledAssembly);

            testOutput.WriteLine ($"compressedOriginalAssembly_fi: {compressedOriginalAssembly_fi.Length}, {compressedOriginalAssembly}");
            testOutput.WriteLine ($"compressedBundledAssembly_fi: {compressedBundledAssembly_fi.Length}, {compressedBundledAssembly}");

            if (expectILStripping)
            {
                if (!UseWebcil)
                {
                    string compressedStrippedAssembly = Utils.GZipCompress(strippedAssembly);
                    FileInfo compressedStrippedAssembly_fi = new FileInfo(compressedStrippedAssembly);
                    testOutput.WriteLine ($"compressedStrippedAssembly_fi: {compressedStrippedAssembly_fi.Length}, {compressedStrippedAssembly}");
                    Assert.True(compressedOriginalAssembly_fi.Length > compressedStrippedAssembly_fi.Length, $"Expected original assembly({compressedOriginalAssembly}) size ({compressedOriginalAssembly_fi.Length}) " +
                                $"to be bigger than the stripped assembly ({compressedStrippedAssembly}) size ({compressedStrippedAssembly_fi.Length})");
                    Assert.True(compressedBundledAssembly_fi.Length == compressedStrippedAssembly_fi.Length, $"Expected bundled assembly({compressedBundledAssembly}) size ({compressedBundledAssembly_fi.Length}) " +
                                $"to be the same as the stripped assembly ({compressedStrippedAssembly}) size ({compressedStrippedAssembly_fi.Length})");
                }
            }
            else
            {
                if (!UseWebcil)
                {
                    // FIXME: The bundled file would be .wasm in case of webcil, so can't compare size
                    Assert.True(compressedOriginalAssembly_fi.Length == compressedBundledAssembly_fi.Length);
                }
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void PublishPdb(bool copyOutputSymbolsToPublishDirectory)
        {
            string config = "Release";
            string id = $"publishpdb_{copyOutputSymbolsToPublishDirectory.ToString().ToLower()}_{GetRandomId()}";
            CreateWasmTemplateProject(id, "wasmbrowser");

            (CommandResult result, _) = BlazorPublish(new BlazorBuildOptions(id, config), $"-p:CopyOutputSymbolsToPublishDirectory={copyOutputSymbolsToPublishDirectory.ToString().ToLower()}");
            result.EnsureSuccessful();

            string publishFrameworkPath = Path.GetFullPath(FindBlazorBinFrameworkDir(config, forPublish: true));
            AssertFile(".pdb");
            AssertFile(".pdb.gz");
            AssertFile(".pdb.br");

            void AssertFile(string suffix)
            {
                var fileName = Directory.EnumerateFiles(publishFrameworkPath, $"*{suffix}").FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).StartsWith(id));
                Assert.True(copyOutputSymbolsToPublishDirectory == (fileName != null && File.Exists(fileName)), $"The {fileName} file {(copyOutputSymbolsToPublishDirectory ? "should" : "shouldn't")} exist in publish folder");
            }
        }
    }
}
