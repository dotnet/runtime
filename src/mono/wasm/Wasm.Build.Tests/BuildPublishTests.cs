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
        [BuildAndRun(config: Configuration.Debug, aot: true)]
        public void Wasm_CannotAOT_InDebug(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "no_aot_in_debug");
            (string _, string buildOutput) = PublishProject(info, config, new PublishOptions(AOT: aot, ExpectSuccess: false));
            Assert.Contains("AOT is not supported in debug configuration", buildOutput);
        }

        [Theory]
        [BuildAndRun(config: Configuration.Release)]
        [BuildAndRun(config: Configuration.Debug)]
        public async Task BuildThenPublishNoAOT(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "build_publish");
            BuildProject(info, config);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun");
            await RunForBuildWithDotnetRun(runOptions);

            PublishProject(info, config, new PublishOptions(UseCache: false));
            await RunForPublishWithWebServer(runOptions);
        }

        [Theory]
        [BuildAndRun(config: Configuration.Release, aot: true)]
        public async Task BuildThenPublishWithAOT(Configuration config, bool aot)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "build_publish");
            
            bool isPublish = false;
            (_, string output) = BuildProject(info, config, new BuildOptions(Label: "first_build", AOT: aot), isNativeBuild: aot);
            
            BuildPaths paths = GetBuildPaths(config, forPublish: isPublish);
            IDictionary<string, (string fullPath, bool unchanged)> pathsDict =
                GetFilesTable(info.ProjectName, aot, paths, unchanged: false);
            
            string mainDll = $"{info.ProjectName}.dll";
            var firstBuildStat = StatFiles(pathsDict);
            Assert.True(firstBuildStat["pinvoke.o"].Exists);
            Assert.False(firstBuildStat[$"{mainDll}.bc"].Exists);

            CheckOutputForNativeBuild(expectAOT: false, expectRelinking: isPublish || aot, info.ProjectName, output);

            if (!_buildContext.TryGetBuildFor(info, out BuildResult? result))
                throw new XunitException($"Test bug: could not get the build result in the cache");

            BrowserRunOptions runOptions = new(config, TestScenario: "DotnetRun");
            await RunForBuildWithDotnetRun(runOptions);

            File.Move(result!.LogFile, Path.ChangeExtension(result.LogFile!, ".first.binlog"));
    
            _testOutput.WriteLine($"{Environment.NewLine}Publishing with no changes ..{Environment.NewLine}");

            // relink by default for Release+publish
            isPublish = true;
            (_, output) = PublishProject(info, config, new PublishOptions(Label: "first_publish", UseCache: false, AOT: aot));
            
            // publish has different paths ("for-publish", not "for-build")
            paths = GetBuildPaths(config, forPublish: isPublish);
            pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: false);  
            IDictionary<string, FileStat> publishStat = StatFiles(pathsDict);
            Assert.True(publishStat["pinvoke.o"].Exists);
            Assert.True(publishStat[$"{mainDll}.bc"].Exists);
            CheckOutputForNativeBuild(expectAOT: true, expectRelinking: isPublish || aot, info.ProjectName, output);
            
            // source maps are created for build but not for publish, make sure CompareStat won't expect them in publish:
            pathsDict["dotnet.js.map"] = (pathsDict["dotnet.js.map"].fullPath, unchanged: false);
            pathsDict["dotnet.runtime.js.map"] = (pathsDict["dotnet.runtime.js.map"].fullPath, unchanged: false);
            CompareStat(firstBuildStat, publishStat, pathsDict);
            await RunForPublishWithWebServer(runOptions);

            // second build
            isPublish = false;
            (_, output) = BuildProject(info, config, new BuildOptions(Label: "second_build", UseCache: false, AOT: aot), isNativeBuild: aot);
            var secondBuildStat = StatFiles(pathsDict);
            
            // no relinking, or AOT
            CheckOutputForNativeBuild(expectAOT: false, expectRelinking: isPublish || aot, info.ProjectName, output);

            // no native files changed
            pathsDict.UpdateTo(unchanged: true);
            CompareStat(publishStat, secondBuildStat, pathsDict);
        }

        void CheckOutputForNativeBuild(bool expectAOT, bool expectRelinking, string projectName, string buildOutput)
        {
            TestUtils.AssertMatches(@$"{projectName}.dll -> {projectName}.dll.bc", buildOutput, contains: expectAOT);
            TestUtils.AssertMatches(@$"{projectName}.dll.bc -> {projectName}.dll.o", buildOutput, contains: expectAOT);
            TestUtils.AssertMatches("pinvoke.c -> pinvoke.o", buildOutput, contains: expectRelinking || expectAOT);
        }
    }
}
