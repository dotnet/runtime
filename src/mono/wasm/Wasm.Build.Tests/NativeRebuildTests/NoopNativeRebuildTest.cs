// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    public class NoopNativeRebuildTest : NativeRebuildTestsBase
    {
        public NoopNativeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public async void NoOpRebuildForNativeBuilds(string config, bool aot, bool nativeRelink, bool invariant)
        {
            ProjectInfo info = CopyTestAsset(config, aot, BasicTestApp, "rebuild_noop");
            BuildPaths paths = await FirstNativeBuildAndRun(info, nativeRelink, invariant);

            var pathsDict = GetFilesTable(info, paths, unchanged: true);
            var originalStat = StatFiles(pathsDict);

            Rebuild(info, nativeRelink, invariant);
            var newStat = StatFiles(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new(info.Configuration, TestScenario: "DotnetRun"));
        }

        [Fact]
        
        public void NativeRelinkFailsWithInvariant()
        {
            bool nativeRelink = false;
            var extraArgs = new string[] {
                "-p:_WasmDevel=true",
                $"-p:WasmBuildNative={nativeRelink}",
                $"-p:InvariantGlobalization=true",
            };
            ProjectInfo info = CopyTestAsset("Release", aot: true, BasicTestApp, "relink_fails");
            bool isPublish = true;
            (string _, string buildOutput) = BuildProject(info,
                new BuildOptions(
                    info.Configuration,
                    info.ProjectName,
                    BinFrameworkDir: GetBinFrameworkDir(info.Configuration, isPublish),
                    IsPublish: isPublish,
                    GlobalizationMode: GlobalizationMode.Invariant,
                    ExpectSuccess: false
                ),
                extraArgs
            );
            Assert.Contains("WasmBuildNative is required because InvariantGlobalization=true, but WasmBuildNative is already set to 'false'", _testOutput.ToString());
        }
    }
}
