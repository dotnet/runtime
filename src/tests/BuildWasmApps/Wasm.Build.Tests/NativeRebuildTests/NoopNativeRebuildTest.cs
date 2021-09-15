// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class NoopNativeRebuildTest : NativeRebuildTestsBase
    {
        public NoopNativeRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public void NoOpRebuildForNativeBuilds(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_noop_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: nativeRelink, invariant: invariant, buildArgs, id);

            var pathsDict = GetFilesTable(buildArgs, paths, unchanged: true);
            var originalStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }
    }
}
