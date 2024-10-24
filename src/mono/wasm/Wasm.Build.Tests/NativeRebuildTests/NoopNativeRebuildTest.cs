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
            string prefix = $"rebuild_noop_{config}";
            ProjectInfo info = CreateWasmTemplateProject(Template.WasmBrowser, config, aot, prefix);
            UpdateBrowserProgramFile();
            UpdateBrowserMainJs();

            BuildPaths paths = await FirstNativeBuildAndRun(info, nativeRelink, invariant);

            var pathsDict = GetFilesTable(info, paths, unchanged: true);
            var originalStat = StatFiles(pathsDict);

            Rebuild(info, nativeRelink, invariant);
            var newStat = StatFiles(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new (info.Configuration, ExpectedExitCode: 42));
        }
    }
}
