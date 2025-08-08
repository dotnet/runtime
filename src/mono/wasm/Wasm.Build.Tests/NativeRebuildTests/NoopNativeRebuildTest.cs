// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Wasm.Build.Tests;
using System.Threading.Tasks;
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
        public async Task NoOpRebuildForNativeBuilds(Configuration config, bool aot, bool nativeRelink, bool invariant)
        {
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild_noop");
            BuildPaths paths = await FirstNativeBuildAndRun(info, config, aot, nativeRelink, invariant);

            var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: true);
            var originalStat = StatFiles(pathsDict);

            Rebuild(info, config, aot, nativeRelink, invariant);
            var newStat = StatFiles(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, TestScenario: "DotnetRun"));
        }

        [Fact]
        public void NativeRelinkFailsWithInvariant()
        {
            Configuration config = Configuration.Release;
            string extraArgs = "-p:_WasmDevel=true -p:WasmBuildNative=false -p:InvariantGlobalization=true";
            ProjectInfo info = CopyTestAsset(config, aot: true, TestAsset.WasmBasicTestApp, "relink_fails");
            var options = new PublishOptions(ExpectSuccess: false, AOT: true, ExtraMSBuildArgs: extraArgs);
            PublishProject(info, config, options);
            Assert.Contains("WasmBuildNative is required because InvariantGlobalization=true, but WasmBuildNative is already set to 'false'", _testOutput.ToString());
        }
    }
}
