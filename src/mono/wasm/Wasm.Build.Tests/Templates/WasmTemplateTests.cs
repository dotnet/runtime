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

        [Theory, TestCategory("no-fingerprinting")]
        [InlineData(Configuration.Debug)]
        [InlineData(Configuration.Release)]
        public void BrowserBuildThenPublish(Configuration config)
        {
            string atEnd = """
                    <Target Name="CheckLinkedFiles" AfterTargets="ILLink">
                        <ItemGroup>
                            <_LinkedOutFile Include="$(IntermediateOutputPath)\linked\*.dll" />
                        </ItemGroup>
                        <Error Text="No file was linked-out. Trimming probably doesn't work (PublishTrimmed=$(PublishTrimmed))" Condition="@(_LinkedOutFile->Count()) == 0" />
                    </Target>
                    """;
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot: false, "browser", insertAtEnd: atEnd);
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            BuildProject(info, config);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            PublishProject(info, config, new PublishOptions(UseCache: false));
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
            => await BrowserRunTwiceWithAndThenWithoutBuildAsync(Configuration.Release, extraProperties, runOutsideProjectDirectory);

        private async Task BrowserRunTwiceWithAndThenWithoutBuildAsync(Configuration config, string extraProperties = "", bool runOutsideProjectDirectory = false)
        {
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot: false, "browser", extraProperties: extraProperties);
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            string workingDir = runOutsideProjectDirectory ? BuildEnvironment.TmpPath : _projectDir;

            {
                using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(workingDir);

                await using var runner = new BrowserRunner(_testOutput);
                var page = await runner.RunAsync(runCommand, $"run --no-silent -c {config} --project \"{info.ProjectName}.csproj\" --forward-console");
                await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
                Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
            }

            {
                using var runCommand = new RunCommand(s_buildEnv, _testOutput)
                                            .WithWorkingDirectory(workingDir);

                await using var runner = new BrowserRunner(_testOutput);
                var page = await runner.RunAsync(runCommand, $"run --no-silent -c {config} --no-build --project \"{info.ProjectName}.csproj\" --forward-console");
                await runner.WaitForExitMessageAsync(TimeSpan.FromMinutes(2));
                Assert.Contains("Hello, Browser!", string.Join(Environment.NewLine, runner.OutputLines));
            }
        }

        public static IEnumerable<object?[]> BrowserBuildAndRunTestData()
        {
            yield return new object?[] { "", BuildTestBase.DefaultTargetFramework, DefaultRuntimeAssetsRelativePath };
            yield return new object?[] { $"-f {DefaultTargetFramework}", DefaultTargetFramework, DefaultRuntimeAssetsRelativePath };

            if (EnvironmentVariables.WorkloadsTestPreviousVersions)
            {
                yield return new object?[] { $"-f {PreviousTargetFramework}", PreviousTargetFramework, DefaultRuntimeAssetsRelativePath };
                yield return new object?[] { $"-f {Previous2TargetFramework}", Previous2TargetFramework, DefaultRuntimeAssetsRelativePath };
            }

            // ActiveIssue("https://github.com/dotnet/runtime/issues/90979")
            // yield return new object?[] { "", BuildTestBase.DefaultTargetFramework, "./" };
            // yield return new object?[] { "-f net8.0", "net8.0", "./" };
        }

        [Theory]
        [MemberData(nameof(BrowserBuildAndRunTestData))]
        public async Task BrowserBuildAndRun(string extraNewArgs, string targetFramework, string runtimeAssetsRelativePath)
        {
            Configuration config = Configuration.Debug;
            string extraProperties = runtimeAssetsRelativePath == DefaultRuntimeAssetsRelativePath ?
                "" :
                $"<WasmRuntimeAssetsLocation>{runtimeAssetsRelativePath}</WasmRuntimeAssetsLocation>";
            ProjectInfo info = CreateWasmTemplateProject(
                Template.WasmBrowser,
                config,
                aot: false,
                "browser",
                extraProperties: extraProperties,
                extraArgs: extraNewArgs,
                addFrameworkArg: extraNewArgs.Length == 0
            );

            if (new Version(targetFramework.Replace("net", "")).Major > 8)
                UpdateBrowserProgramFile();
            UpdateBrowserMainJs(targetFramework, runtimeAssetsRelativePath);

            PublishProject(info, config, new PublishOptions(UseCache: false));

            var runOutput = await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42));
            Assert.Contains("Hello, Browser!", runOutput.TestOutput);
        }

        [Theory]
        [InlineData(Configuration.Debug, /*appendRID*/ true, /*useArtifacts*/ false)]
        [InlineData(Configuration.Debug, /*appendRID*/ true, /*useArtifacts*/ true)]
        [InlineData(Configuration.Debug, /*appendRID*/ false, /*useArtifacts*/ true)]
        [InlineData(Configuration.Debug, /*appendRID*/ false, /*useArtifacts*/ false)]
        public async Task BuildAndRunForDifferentOutputPaths(Configuration config, bool appendRID, bool useArtifacts)
        {
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot: false);
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            bool isPublish = false;
            string projectDirectory = Path.GetDirectoryName(info.ProjectFilePath) ?? "";
            // browser app does not allow appending RID
            string frameworkDir = useArtifacts ?
                Path.Combine(projectDirectory, "bin", info.ProjectName, config.ToString().ToLower(), "wwwroot", "_framework") :
                GetBinFrameworkDir(config, isPublish);

            string extraPropertiesForDBP = string.Empty;
            if (useArtifacts)
            {
                extraPropertiesForDBP += "<UseArtifactsOutput>true</UseArtifactsOutput><ArtifactsPath>.</ArtifactsPath>";
            }
            if (appendRID)
            {
                extraPropertiesForDBP += "<AppendRuntimeIdentifierToOutputPath>true</AppendRuntimeIdentifierToOutputPath>";
            }
            // UseArtifactsOutput cannot be set in a project file, due to MSBuild ordering constraints.
            string propsPath = Path.Combine(projectDirectory, "Directory.Build.props");
            AddItemsPropertiesToProject(propsPath, extraPropertiesForDBP);

            BuildProject(info, config, new BuildOptions(NonDefaultFrameworkDir: frameworkDir));
            await RunForBuildWithDotnetRun(new BrowserRunOptions(config, ExpectedExitCode: 42, ExtraArgs: "x y z"));
        }

        [Theory]
        [InlineData("", true)] // Default case
        [InlineData("false", false)] // the other case
        public async Task Test_WasmStripILAfterAOT(string stripILAfterAOT, bool expectILStripping)
        {
            Configuration config = Configuration.Release;
            bool aot = true;
            string extraProperties = "<RunAOTCompilation>true</RunAOTCompilation>";
            if (!string.IsNullOrEmpty(stripILAfterAOT))
                extraProperties += $"<WasmStripILAfterAOT>{stripILAfterAOT}</WasmStripILAfterAOT>";
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, "strip", extraProperties: extraProperties);

            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            PublishProject(info, config, new PublishOptions(UseCache: false, AssertAppBundle: false));
            await RunForBuildWithDotnetRun(new BrowserRunOptions(config, ExpectedExitCode: 42));

            string projectDirectory = Path.GetDirectoryName(info.ProjectFilePath)!;
            string objBuildDir = Path.Combine(projectDirectory, "obj", config.ToString(), BuildTestBase.DefaultTargetFramework, "wasm", "for-publish");

            string frameworkDir = GetBinFrameworkDir(config, forPublish: true);
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
            Configuration config = Configuration.Release;
            string shouldCopy = copyOutputSymbolsToPublishDirectory.ToString().ToLower();
            string extraProperties = $"<CopyOutputSymbolsToPublishDirectory>{shouldCopy}</CopyOutputSymbolsToPublishDirectory>";
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot: false, "publishpdb", extraProperties: extraProperties);

            PublishProject(info, config, new PublishOptions(ExtraMSBuildArgs: "-p:CompressionEnabled=true"));
            string publishPath = GetBinFrameworkDir(config, forPublish: true);
            AssertFile(".pdb");
            AssertFile(".pdb.gz");
            AssertFile(".pdb.br");

            void AssertFile(string suffix)
            {
                var fileName = Directory.EnumerateFiles(publishPath, $"*{suffix}").FirstOrDefault(f => Path.GetFileNameWithoutExtension(f).StartsWith(info.ProjectName));
                Assert.True(copyOutputSymbolsToPublishDirectory == (fileName != null && File.Exists(fileName)), $"The {fileName} file {(copyOutputSymbolsToPublishDirectory ? "should" : "shouldn't")} exist in publish folder");
            }
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async void LibraryModeBuild(bool useWasmSdk)
        {
            var config = Configuration.Release;
            ProjectInfo info = CopyTestAsset(config, aot: false, TestAsset.LibraryModeTestApp, "libraryMode");
            if (!useWasmSdk)
            {
                UpdateFile($"{info.ProjectName}.csproj", new Dictionary<string, string>() {
                    { "Microsoft.NET.Sdk.WebAssembly", "Microsoft.NET.Sdk" }
                });
            }
            BuildProject(info, config, new BuildOptions(AssertAppBundle: useWasmSdk));
            if (useWasmSdk)
            {
                var result = await RunForBuildWithDotnetRun(new BrowserRunOptions(config, ExpectedExitCode: 100));
                Assert.Contains("WASM Library MyExport is called", result.TestOutput);
            }
            
        }
    }
}
