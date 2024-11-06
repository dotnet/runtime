// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;
using System.Collections.Generic;

#nullable enable

namespace Wasm.Build.Tests
{
    public class BuildPublishTests : WasmTemplateTestsBase
    {
        public BuildPublishTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(config: "Debug", aot: true)]
        public void Wasm_CannotAOT_InDebug(string config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "no_aot_in_debug", "App");

            bool isPublish = true;
            (string _, string buildOutput) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish,
                            ExpectSuccess: false
                        ));
            Assert.Contains("AOT is not supported in debug configuration", buildOutput);
        }

        [Theory]
        [BuildAndRun(config: "Release")]
        [BuildAndRun(config: "Debug")]
        public async Task BuildThenPublishNoAOT(string config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "build_publish", "App");

            bool isPublish = false;
            BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish
                        ));

            if (!_buildContext.TryGetBuildFor(info, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            RunOptions runOptions = new(info.Configuration, TestScenario: "DotnetRun");
            await RunForBuildWithDotnetRun(runOptions);

            isPublish = true;
            BuildTemplateProject(info,
                        new BuildProjectOptions(
                            info.Configuration,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish,
                            UseCache: false
                        ));
            await RunForPublishWithWebServer(runOptions);
        }

        [Theory]
        [BuildAndRun(config: "Release", aot: true)]
        public async Task BuildThenPublishWithAOT(string config, bool aot)
        {
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, "build_publish");

            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();
            
            bool isPublish = false;
            (_, string output) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish,
                            Label: "first_build"
                        ));
            
            BuildPaths paths = GetBuildPaths(info, forPublish: isPublish);
            IDictionary<string, (string fullPath, bool unchanged)> pathsDict =
                GetFilesTable(info, paths, unchanged: false);
            
            string mainDll = $"{info.ProjectName}.dll";
            var firstBuildStat = StatFiles(pathsDict);
            Assert.False(firstBuildStat["pinvoke.o"].Exists);
            Assert.False(firstBuildStat[$"{mainDll}.bc"].Exists);
            
            CheckOutputForNativeBuild(expectAOT: false, expectRelinking: isPublish, info, output);

            if (!_buildContext.TryGetBuildFor(info, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            RunOptions runOptions = new(info.Configuration, ExpectedExitCode: 42);
            await RunForBuildWithDotnetRun(runOptions);

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));
    
            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            // relink by default for Release+publish
            isPublish = true;
            (_, output) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish,
                            UseCache: false,
                            Label: "first_publish"
                        ));
            
            // publish has different paths ("for-publish", not "for-build")
            paths = GetBuildPaths(info, forPublish: isPublish);
            pathsDict = GetFilesTable(info, paths, unchanged: false);    
            IDictionary<string, FileStat> publishStat = StatFiles(pathsDict);
            Assert.True(publishStat["pinvoke.o"].Exists);
            Assert.True(publishStat[$"{mainDll}.bc"].Exists);
            CheckOutputForNativeBuild(expectAOT: true, expectRelinking: isPublish, info, output);
            
            // source maps are created for build but not for publish, make sure CompareStat won't expect them in publish:
            pathsDict["dotnet.js.map"] = (pathsDict["dotnet.js.map"].fullPath, unchanged: false);
            pathsDict["dotnet.runtime.js.map"] = (pathsDict["dotnet.runtime.js.map"].fullPath, unchanged: false);
            CompareStat(firstBuildStat, publishStat, pathsDict);
            await RunForPublishWithWebServer(runOptions);

            // second build
            isPublish = false;
            (_, output) = BuildTemplateProject(info,
                        new BuildProjectOptions(
                            config,
                            info.ProjectName,
                            BinFrameworkDir: GetBinFrameworkDir(config, isPublish),
                            ExpectedFileType: GetExpectedFileType(info, isPublish),
                            IsPublish: isPublish,
                            UseCache: false,
                            Label: "second_build"
                        ));
            var secondBuildStat = StatFiles(pathsDict);
            
            // no relinking, or AOT
            CheckOutputForNativeBuild(expectAOT: false, expectRelinking: isPublish, info, output);

            // no native files changed
            pathsDict.UpdateTo(unchanged: true);
            CompareStat(publishStat, secondBuildStat, pathsDict);
        }

        void CheckOutputForNativeBuild(bool expectAOT, bool expectRelinking, ProjectInfo buildArgs, string buildOutput)
        {
            string projectNameCore = buildArgs.ProjectName.Replace(s_unicodeChars, "");
            TestUtils.AssertMatches(@$"{projectNameCore}\S+.dll -> {projectNameCore}\S+.dll.bc", buildOutput, contains: expectAOT);
            TestUtils.AssertMatches(@$"{projectNameCore}\S+.dll.bc -> {projectNameCore}\S+.dll.o", buildOutput, contains: expectAOT);
            TestUtils.AssertMatches("pinvoke.c -> pinvoke.o", buildOutput, contains: expectRelinking || expectAOT);
        }
    }
}
