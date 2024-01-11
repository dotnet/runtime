// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

#nullable enable

namespace Wasm.Build.Tests
{
    public class InvariantTimezoneTests : TestMainJsTestBase
    {
        public InvariantTimezoneTests(ITestOutputHelper output, SharedBuildPerTestClassFixture buildContext)
            : base(output, buildContext)
        {
        }

        public static IEnumerable<object?[]> InvariantTimezoneTestData(bool aot, RunHost host)
            => ConfigWithAOTData(aot)
                .Multiply(
                    new object?[] { null },
                    new object?[] { false },
                    new object?[] { true })
                .WithRunHosts(host)
                .UnwrapItemsAsArrays();

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ true, RunHost.All })]
        public Task AOT_InvariantTimezone(BuildArgs buildArgs, bool? invariantTimezone, RunHost host, string id)
            => TestInvariantTimezoneAsync(buildArgs, invariantTimezone, host, id);

        [Theory]
        [MemberData(nameof(InvariantTimezoneTestData), parameters: new object[] { /*aot*/ false, RunHost.All })]
        public Task RelinkingWithoutAOT(BuildArgs buildArgs, bool? invariantTimezone, RunHost host, string id)
            => TestInvariantTimezoneAsync(buildArgs, invariantTimezone, host, id,
                                            extraProperties: "<WasmBuildNative>true</WasmBuildNative>",
                                            dotnetWasmFromRuntimePack: false);

        private async Task TestInvariantTimezoneAsync(BuildArgs buildArgs, bool? invariantTimezone,
                                                        RunHost host, string id, string extraProperties="", bool? dotnetWasmFromRuntimePack=null)
        {
            string projectName = $"invariant_{invariantTimezone?.ToString() ?? "unset"}";
            if (invariantTimezone != null)
                extraProperties = $"{extraProperties}<InvariantTimezone>{invariantTimezone}</InvariantTimezone>";

            buildArgs = buildArgs with { ProjectName = projectName };
            buildArgs = ExpandBuildArgs(buildArgs, extraProperties);

            if (dotnetWasmFromRuntimePack == null)
                dotnetWasmFromRuntimePack = !(buildArgs.AOT || buildArgs.Config == "Release");

            BuildProject(buildArgs,
                            id: id,
                            new BuildProjectOptions(
                                InitProject: () => File.Copy(Path.Combine(BuildEnvironment.TestAssetsPath, "Wasm.Buid.Tests.Programs", "InvariantTimezone.cs"), Path.Combine(_projectDir!, "Program.cs")),
                                DotnetWasmFromRuntimePack: dotnetWasmFromRuntimePack));

            string output = await RunAndTestWasmAppAsync(buildArgs, expectedExitCode: 42, host: host, id: id);
            Assert.Contains("UTC BaseUtcOffset is 0", output);
            if (invariantTimezone == true)
            {
                Assert.Contains("Could not find Asia/Tokyo", output);
            }
            else
            {
                Assert.Contains("Asia/Tokyo BaseUtcOffset is 09:00:00", output);
            }
        }
    }
}
