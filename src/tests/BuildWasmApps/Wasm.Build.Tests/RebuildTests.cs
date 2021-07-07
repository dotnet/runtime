// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class RebuildTests : BuildTestBase
    {
        public RebuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        [Theory]
        [BuildAndRun(host: RunHost.V8, aot: false, parameters: false)]
        [BuildAndRun(host: RunHost.V8, aot: false, parameters: true)]
        [BuildAndRun(host: RunHost.V8, aot: true,  parameters: false)]
        public void NoOpRebuild(BuildArgs buildArgs, bool nativeRelink, RunHost host, string id)
        {
            string projectName = $"rebuild_{buildArgs.Config}_{buildArgs.AOT}";
            bool dotnetWasmFromRuntimePack = !nativeRelink && !buildArgs.AOT;

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, $"<WasmBuildNative>{(nativeRelink ? "true" : "false")}</WasmBuildNative>");

            BuildProject(buildArgs,
                        initProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                        id: id,
                        createProject: true);

            Run();

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                Assert.True(false, $"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");

            // no-op Rebuild
            BuildProject(buildArgs,
                        () => {},
                        dotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                        id: id,
                        createProject: false,
                        useCache: false);

            Run();

            void Run() => RunAndTestWasmApp(
                                buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                                test: output => {},
                                host: host, id: id);
        }
    }
}
