// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

#nullable enable

namespace Wasm.Build.Tests
{
    public class RebuildTests : BuildTestBase
    {
        public RebuildTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> NonNativeDebugRebuildData()
            => ConfigWithAOTData(aot: false, config: "Debug")
                    .WithRunHosts(RunHost.Chrome)
                    .UnwrapItemsAsArrays().ToList();

        [Theory]
        [MemberData(nameof(NonNativeDebugRebuildData))]
        public void NoOpRebuild(BuildArgs buildArgs, RunHost host, string id)
        {
            string projectName = $"rebuild_{buildArgs.Config}_{buildArgs.AOT}";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs);

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), s_mainReturns42),
                                DotnetWasmFromRuntimePack: true,
                                CreateProject: true));

            Run();

            if (!_buildContext.TryGetBuildFor(buildArgs, out BuildProduct? product))
                throw new XunitException($"Test bug: could not get the build product in the cache");

            File.Move(product!.LogFile, Path.ChangeExtension(product.LogFile!, ".first.binlog"));

            _testOutput.WriteLine($"{Environment.NewLine}Rebuilding with no changes ..{Environment.NewLine}");

            // no-op Rebuild
            BuildProject(buildArgs,
                        id: id,
                        new BuildProjectOptions(
                            DotnetWasmFromRuntimePack: true,
                            CreateProject: false,
                            UseCache: false));

            Run();

            void Run() => RunAndTestWasmApp(
                                buildArgs, buildDir: _projectDir, expectedExitCode: 42,
                                test: output => {},
                                host: host, id: id);
        }
    }
}
