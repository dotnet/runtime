// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Wasm.Build.Tests;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.NativeRebuild.Tests
{
    [TestCategory("native-mono")]
    public class ReferenceNewAssemblyRebuildTest : NativeRebuildTestsBase
    {
        public ReferenceNewAssemblyRebuildTest(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [MemberData(nameof(NativeBuildData))]
        public async Task ReferenceNewAssembly(Configuration config, bool aot, bool nativeRelink, bool invariant)
        {
            // Reference a dedicated first-party library that the default app does not use, so it is
            // trimmed away on the first build and only enters the AOT module set once the swapped
            // entry point references it. Relying on a specific BCL assembly being absent from the
            // closure is fragile (it has silently regressed as the base app's dependencies grew).
            string extraItems = @"<ProjectReference Include=""..\Library\Library.csproj"" />";
            ProjectInfo info = CopyTestAsset(config, aot, TestAsset.WasmBasicTestApp, "rebuild_tasks", extraItems: extraItems);
            ReplaceFile(Path.Combine("..", "Library", "Library.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "NativeRebuildReferencedLibrary.cs"));
            BuildPaths paths = await FirstNativeBuildAndRun(info, config, aot, nativeRelink, invariant);

            var pathsDict = GetFilesTable(info.ProjectName, aot, paths, unchanged: false);
            pathsDict.UpdateTo(unchanged: true, "corebindings.o");
            pathsDict.UpdateTo(unchanged: true, "driver.o");
            if (!aot) // relinking
                pathsDict.UpdateTo(unchanged: true, "driver-gen.c");

            var originalStat = StatFiles(pathsDict);

            ReplaceFile(Path.Combine("Common", "Program.cs"), Path.Combine(BuildEnvironment.TestAssetsPath, "EntryPoints", "NativeRebuildNewAssembly.cs"));

            Rebuild(info, config, aot, nativeRelink, invariant, assertAppBundle: !aot);
            var newStat = StatFilesAfterRebuild(pathsDict);

            CompareStat(originalStat, newStat, pathsDict);
            await RunForPublishWithWebServer(new BrowserRunOptions(config, ExpectedExitCode: 42, TestScenario: "DotnetRun"));
        }
    }
}
