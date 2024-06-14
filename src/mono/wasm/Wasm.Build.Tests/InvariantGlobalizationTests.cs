// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class InvariantGlobalizationTests : TestMainJsTestBase
    {
        public InvariantGlobalizationTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantGlobalizationTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        // TODO: check that icu bits have been linked out
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public void AOT_InvariantGlobalization(BuildArgs buildArgs, bool? invariantGlobalization, RunHost host, string id)
            => TestInvariantGlobalization(buildArgs, invariantGlobalization, host, id);

        // TODO: What else should we use to verify a relinked build?
        [Theory]
        [MemberData(nameof(InvariantGlobalizationTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public void RelinkingWithoutAOT(BuildArgs buildArgs, bool? invariantGlobalization, RunHost host, string id)
            => TestInvariantGlobalization(buildArgs, invariantGlobalization, host, id,
                                            extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                                            dotnetWasmFromRuntimePack: false);

        private void TestInvariantGlobalization(BuildArgs buildArgs, bool? invariantGlobalization,
                                                        RunHost host, string id, string extraProperties="", bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"invariant_{invariantGlobalization?.ToString() ?? "unset"}";
            if (invariantGlobalization != null)
                extraProperties = $"{extraProperties}<InvariantGlobalization>{invariantGlobalization}</InvariantGlobalization>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "Wasm.Buid.Tests.Programs", "InvariantGlobalization.cs"), Path.Combine(_projectDir!, "Program.cs")),
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack,
                                GlobalizationMode: invariantGlobalization == true ? GlobalizationMode.Invariant : GlobalizationMode.Sharded));

            if (invariantGlobalization == true)
            {
                string output = RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id);
                Assert.Contains("Could not create es-ES culture", output);
                Assert.Contains("CurrentCulture.NativeName: Invariant Language (Invariant Country)", output);
            }
            else
            {
                string output = RunAndTestWasmApp(buildArgs, expectedExitCode: 42, host: host, id: id, args: "nativename=\"espa\u00F1ol (Espa\u00F1a)\"");
                Assert.Contains("es-ES: Is Invariant LCID: False", output);

                // ignoring the last line of the output which prints the current culture
            }
        }
    }
}
