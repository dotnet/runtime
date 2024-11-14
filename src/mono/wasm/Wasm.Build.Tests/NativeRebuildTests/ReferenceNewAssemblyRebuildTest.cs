// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    public class ReferenceNewAssemblyRebuildTest : NativeRebuildTestsBase
    {
        public ReferenceNewAssemblyRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        [ActiveIssue("File sizes don't match: dotnet.native.wasm size should be same as from obj/for-publish but is not")]
        public async void ReferenceNewAssembly(string config, bool aot, bool nativeRelink, bool invariant)
        {
            ProjectInfo info = CopyTestAsset(config, aot, "WasmBasicTestApp", "rebuild_tasks", "App");     
            BuildPaths paths = await FirstNativeBuildAndRun(info, nativeRelink, invariant);

            var pathsDict = GetFilesTable(info, paths, unchanged: false);
            pathsDict.UpdateTo(unchanged: true, "corebindings.o");
            pathsDict.UpdateTo(unchanged: true, "driver.o");
            if (!info.AOT) // relinking
                pathsDict.UpdateTo(unchanged: true, "driver-gen.c");

            var originalStat = StatFiles(pathsDict);

            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "NativeRebuildNewAssembly.cs"));

            Rebuild(info, nativeRelink, invariant);
            var newStat = StatFiles(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new (info.Configuration, ExpectedExitCode: 42, TestScenario: "DotnetRun"));
        }
    }
}
