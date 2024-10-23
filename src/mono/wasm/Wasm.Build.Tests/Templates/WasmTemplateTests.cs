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
    public class WasmTemplateTests : WasmTemplateTestsBase
    {
        public WasmTemplateTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        private BuildProjectOptions _basePublishProjectOptions = new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: false,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: true
                        );
        private BuildProjectOptions _baseBuildProjectOptions = new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: true,
                            CreateProject: false,
                            HasV8Script: false,
                            MainJS: "main.js",
                            Publish: false
                        );

        [Theory, TestCategory("no-fingerprinting")]
        [InlineData("Debug")]
        [InlineData("Release")]
        public void BrowserBuildThenPublish(string config)
        {
            string id = $"browser_{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);

            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

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
            BuildTemplateProject(buildArgs, id: id, _baseBuildProjectOptions);

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            bool expectRelinking = config == "Release";
            BuildTemplateProject(buildArgs,
                        id: id,
                        _basePublishProjectOptions with
                        {
                            UseCache = false,
                            DotnetWasmFromRuntimePack = !expectRelinking,
                        }
                    );
        }

        public static TheoryData<bool, string> TestDataForAppBundleDir()
        {
            var data = new TheoryData<bool, string>();
            AddTestData(runOutsideProjectDirectory: false);
            AddTestData(runOutsideProjectDirectory: true);

            void AddTestData(bool runOutsideProjectDirectory)
            {
                // FIXME: Disabled for `main` right now, till 7.0 gets the fix
                data.Add(runOutsideProjectDirectory, string.Empty);
                data.Add(runOutsideProjectDirectory,
                                $"<OutputPath>{Path.Combine(BuildEnvironment.TmpPath, Path.GetRandomFileName())}</OutputPath>");
                data.Add(runOutsideProjectDirectory,
                                $"<WasmAppDir>{Path.Combine(BuildEnvironment.TmpPath, Path.GetRandomFileName())}</WasmAppDir>");
            }

            return data;
        }

        [Theory, TestCategory("no-fingerprinting")]
        [MemberData(nameof(TestDataForAppBundleDir))]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/108107")]
        public async Task RunWithDifferentAppBundleLocations(bool runOutsideProjectDirectory, string extraProperties)
            => await BrowserRunTwiceWithAndThenWithoutBuildAsync("Release", extraProperties, runOutsideProjectDirectory);

        private async Task BrowserRunTwiceWithAndThenWithoutBuildAsync(string config, string extraProperties = "", bool runOutsideProjectDirectory = false)
        {
            string id = $"browser_{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");

            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

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
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser", extraNewArgs, addFrameworkArg: extraNewArgs.Length == 0);
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string extraProperties = runtimeAssetsRelativePath == DefaultRuntimeAssetsRelativePath ?
                "" :
                $"<WasmRuntimeAssetsLocation>{runtimeAssetsRelativePath}</WasmRuntimeAssetsLocation>";
            AddItemsPropertiesToProject(projectFile, extraProperties);

            if (targetFramework != "net8.0")
                UpdateBrowserProgramFile();
            UpdateBrowserMainJs(targetFramework, runtimeAssetsRelativePath);

            using ToolCommand cmd = new DotNetCommand(s_buildEnv, _testOutput)
                                        .WithWorkingDirectory(_projectDir!);
            cmd.Execute($"build -c {config} -bl:{Path.Combine(s_buildEnv.LogRootPath, $"{id}.binlog")} {(runtimeAssetsRelativePath != DefaultRuntimeAssetsRelativePath ? "-p:WasmRuntimeAssetsLocation=" + runtimeAssetsRelativePath : "")}")
                .EnsureSuccessful();
            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);
            BuildTemplateProject(buildArgs, id: id, _baseBuildProjectOptions);

            string runOutput = await RunBuiltBrowserApp(config, projectFile);
            Assert.Contains("Hello, Browser!", runOutput);
        }

        [Theory]
        [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ false)]
        [InlineData("Debug", /*appendRID*/ true, /*useArtifacts*/ true)]
        [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ true)]
        [InlineData("Debug", /*appendRID*/ false, /*useArtifacts*/ false)]
        public async Task BuildAndRunForDifferentOutputPaths(string config, bool appendRID, bool useArtifacts)
        {
            string id = $"{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string projectDirectory = Path.GetDirectoryName(projectFile)!;

            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            string extraPropertiesForDBP = string.Empty;
            string frameworkDir = FindBinFrameworkDir(config, forPublish: false);
            
            var buildOptions = _baseBuildProjectOptions with 
            {
                BinFrameworkDir = frameworkDir
            };
            if (useArtifacts)
            {
                extraPropertiesForDBP += "<UseArtifactsOutput>true</UseArtifactsOutput><ArtifactsPath>.</ArtifactsPath>";
                buildOptions = buildOptions with
                {
                    // browser app does not allow appending RID
                    BinFrameworkDir = Path.Combine(
                                            projectDirectory,
                                            "bin",
                                            id,
                                            config.ToLower(),
                                            "wwwroot",
                                            "_framework")
                };
            }
            if (appendRID)
            {
                extraPropertiesForDBP += "<AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>";
            }
            // UseArtifactsOutput cannot be set in a project file, due to MSBuild ordering constraints.
            string propsPath = Path.Combine(projectDirectory, "Directory.Build.props");
            AddItemsPropertiesToProject(propsPath, extraPropertiesForDBP);

            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);
            BuildTemplateProject(buildArgs, id: id, buildOptions);

            await RunBuiltBrowserApp(config, projectFile, extraArgs: "x y z");
        }

        [Theory]
        [InlineData("", true)] // Default case
        [InlineData("false", false)] // the other case
        public async Task Test_WasmStripILAfterAOT(string stripILAfterAOT, bool expectILStripping)
        {
            string config = "Release";
            string id = $"strip_{config}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            string projectDirectory = Path.GetDirectoryName(projectFile)!;
            bool aot = true;

            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            string extraProperties = "<RunAOTCompilation>true</RunAOTCompilation>";
            if (!string.IsNullOrEmpty(stripILAfterAOT))
                extraProperties += $"<WasmStripILAfterAOT>{stripILAfterAOT}</WasmStripILAfterAOT>";
            AddItemsPropertiesToProject(projectFile, extraProperties);

            var buildArgs = new BuildArgs(projectName, config, aot, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);
            BuildTemplateProject(buildArgs,
                        id: id,
                        _basePublishProjectOptions with {
                            UseCache = false,
                            AssertAppBundle = false
                        });

            await RunBuiltBrowserApp(config, projectFile);
            string frameworkDir = FindBinFrameworkDir(config, forPublish: true);
            string objBuildDir = Path.Combine(projectDirectory, "obj", config, BuildTestBase.DefaultTargetFramework, "wasm", "for-publish");
            TestWasmStripILAfterAOTOutput(objBuildDir, frameworkDir, expectILStripping, _testOutput);
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
            string shouldCopy = copyOutputSymbolsToPublishDirectory.ToString().ToLower();
            string id = $"publishpdb_{shouldCopy}_{GetRandomId()}";
            string projectFile = CreateWasmTemplateProject(id, "wasmbrowser");
            string projectName = Path.GetFileNameWithoutExtension(projectFile);
            var buildArgs = new BuildArgs(projectName, config, false, id, null);
            buildArgs = ExpandBuildArgs(buildArgs);
            AddItemsPropertiesToProject(projectFile,
                extraProperties: $"<CopyOutputSymbolsToPublishDirectory>{shouldCopy}</CopyOutputSymbolsToPublishDirectory>");

            BuildTemplateProject(buildArgs, buildArgs.Id, _basePublishProjectOptions);
            string publishPath = FindBinFrameworkDir(config, forPublish: true);
            AssertFile(".pdb");
            AssertFile(".pdb.gz");
            AssertFile(".pdb.br");

            void AssertFile(string suffix)
            {
                var fileName = Directory.EnumerateFiles(publishPath, $"*{suffix}").FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).StartsWith(id));
                Assert.True(copyOutputSymbolsToPublishDirectory == (fileName != null && File.Exists(fileName)), $"The {fileName} file {(copyOutputSymbolsToPublishDirectory ? "should" : "shouldn't")} exist in publish folder");
            }
        }
    }
}
