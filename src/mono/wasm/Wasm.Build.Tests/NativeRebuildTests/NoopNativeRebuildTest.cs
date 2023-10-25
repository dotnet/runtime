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
        public void NoOpRebuildForNativeBuilds(BuildArgs buildArgs, bool nativeRelink, bool invariant, RunHost host, string id)
        {
            buildArgs = buildArgs with { ProjectName = $"rebuild_noop_{buildArgs.Config}" };
            (buildArgs, BuildPaths paths) = FirstNativeBuild(s_mainReturns42, nativeRelink: nativeRelink, invariant: invariant, buildArgs, id);

            var pathsDict = _provider.GetFilesTable(buildArgs, paths, unchanged: true);
            var originalStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            Rebuild(nativeRelink, invariant, buildArgs, id);
            var newStat = _provider.StatFiles(pathsDict.Select(kvp => kvp.Value.fullPath));

            _provider.CompareStat(originalStat, newStat, pathsDict.Values);
            RunAndTestWasmApp(buildArgs, buildDir: _projectDir, expectedExitCode: 42, host: host, id: id);
        }
    }
}
