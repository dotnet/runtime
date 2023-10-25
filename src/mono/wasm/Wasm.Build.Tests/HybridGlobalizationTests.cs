// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class HybridGlobalizationTests : TestMainJsTestBase
    {
        public HybridGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> HybridGlobalizationTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(HybridGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(HybridGlobalizationTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void AOT_HybridGlobalizationTests(BuildArgs buildArgs, RunHost host, string id)
            => TestHybridGlobalizationTests(buildArgs, host, id);

        [Theory]
        [MemberData(nameof(HybridGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void RelinkingWithoutAOT(BuildArgs buildArgs, RunHost host, string id)
            => TestHybridGlobalizationTests(buildArgs, host, id,
                                            extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                                            dotnetWasmFromRuntimePack: false);

        private void TestHybridGlobalizationTests(BuildArgs buildArgs, RunHost host, string id, string extraProperties="", bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"hybrid";
            extraProperties = $"{extraProperties}<HybridGlobalization>true</HybridGlobalization>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            string programText = File.ReadAllText(Path.Combine(BuildEnvironment.TestAssetsPath, "Wasm.Buid.Tests.Programs", "HybridGlobalization.cs"));

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.WriteAllText(Path.Combine(_projectDir!, "Program.cs"), programText),
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                                GlobalizationMode: GlobalizationMode.Hybrid));

            string output = RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("HybridGlobalization works, thrown exception as expected", output);
        }
    }
}
